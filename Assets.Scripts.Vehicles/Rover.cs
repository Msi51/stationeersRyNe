using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Trading;
using UI;
using UnityEngine;

namespace Assets.Scripts.Vehicles;

public class Rover : Vehicle, IBatteryPowered, IPowered, IDensePoolable, IReferencable, IEvaluable, IContainerMount, IFastenedConnector, ITransmitable, ILogicable, IThermal, IExitable, IRepairable
{
	public static List<Rover> AllRovers = new List<Rover>();

	public static float MaxStorageMountDistance = 30f;

	private static readonly float RepairSpeedScale = 1f;

	public Transform DriverExit;

	public Transform PassengerExit;

	public ContainerSlot[] ConnectionSlots;

	private List<Slot> _batterySlots = new List<Slot>(3);

	public static int SpeedHash = Animator.StringToHash("Speed");

	public static int PitchHash = Animator.StringToHash("Pitch");

	public static int RollHash = Animator.StringToHash("Roll");

	public static int EngineAudioHash = Animator.StringToHash("Engine");

	public static int CabinRattlesHash = Animator.StringToHash("CabinRattles");

	private float _speedIndicator;

	private float _pitchIndicator;

	private float _rollIndicator;

	private float _deltaRollPitch;

	private const float MaxDeltaRollPitch = 40f;

	private const float MaxRollPitchLerp = 20f;

	private const float MinRollPitchLerp = 0.5f;

	private bool _engineSound;

	public const float GearRatio = 90f;

	public const float MaxRpm = 8000f;

	public const float LerpSpeed = 10f;

	public float EnginePitchMin = 0.5f;

	public float EnginePitchMax = 0.75f;

	public AnimationCurve MaxTurningAngleSpeedAdjustedCurve;

	public AnimationCurve TurningTimeSpeedAdjustedCurve;

	private float _enginePitch;

	public float MaxSpeed = 3f;

	public Vector3 CrowBarForce = new Vector3(0f, 2000f, 0f);

	private static int RoverEnterHash = Animator.StringToHash("RoverEnter");

	private static readonly float _radiusCheck = 0.05f;

	public Slot ContainerSlot => null;

	public Slot ContainerLeft => Slots[12];

	public Slot ContainerRight => Slots[13];

	public Slot TankLeft => Slots[14];

	public Slot TankRight => Slots[15];

	public bool IsLeftStorageEmpty
	{
		get
		{
			if (ContainerLeft.Occupant == null)
			{
				return TankLeft.Occupant == null;
			}
			return false;
		}
	}

	public bool IsRightStorageEmpty
	{
		get
		{
			if (ContainerRight.Occupant == null)
			{
				return TankRight.Occupant == null;
			}
			return false;
		}
	}

	public Slot BatterySlot => null;

	public BatteryCell Battery
	{
		get
		{
			for (int i = 0; i < _batterySlots.Count; i++)
			{
				BatteryCell batteryCell = _batterySlots[i].Occupant as BatteryCell;
				if ((bool)batteryCell && !batteryCell.IsEmpty)
				{
					return batteryCell;
				}
			}
			return null;
		}
	}

	private bool HasBatteries => _batterySlots.Exists((Slot slot) => slot.Occupant);

	public override bool HasAuthority
	{
		get
		{
			if (base.DriverSlot.IsEmpty() && GameManager.RunSimulation)
			{
				return true;
			}
			if (!GameManager.IsBatchMode && base.DriverSlot.Get<Human>() == InventoryManager.ParentHuman)
			{
				return true;
			}
			return false;
		}
	}

	public bool FreeLook => true;

	public float RepairRatio => DamageState.TotalRatio;

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		Thing.IsNetworkUpdateRequired(256u, networkUpdateType);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		Thing.IsNetworkUpdateRequired(256u, networkUpdateType);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt16((short)base.LastValidPlayablePosition.x);
		writer.WriteInt16((short)base.LastValidPlayablePosition.y);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		short num = reader.ReadInt16();
		short num2 = reader.ReadInt16();
		base.LastValidPlayablePosition = new Vector2(num, num2);
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}

	public override bool IsSoundLocal()
	{
		if (base.DriverSlot.Occupant == InventoryManager.Parent)
		{
			return true;
		}
		if (base.PassengerSlot.Occupant == InventoryManager.Parent)
		{
			return true;
		}
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		ElectricityManager.Register(this);
		AllRovers.Add(this);
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			if (slot.Type == Slot.Class.Battery)
			{
				_batterySlots.Add(slot);
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
			ElectricityManager.Deregister(this);
			AllRovers.Remove(this);
		}
	}

	public void OnPowerTick()
	{
		if (!GameManager.RunSimulation || IsCursor || GameManager.GameState != GameState.Running)
		{
			return;
		}
		float num = ((CurrentMotorPower > 0f) ? (CurrentMotorPower * 10f) : 0f);
		if (Button1 > 0)
		{
			num += 20f;
		}
		if (Button2 > 0)
		{
			num += 5f;
		}
		int num2 = 0;
		if (OnOff)
		{
			float num3 = 0f;
			float num4 = 0f;
			int count = _batterySlots.Count;
			while (count-- > 0)
			{
				BatteryCell batteryCell = _batterySlots[count].Occupant as BatteryCell;
				if (!(batteryCell == null))
				{
					num3 += batteryCell.PowerMaximum;
					num4 += batteryCell.PowerStored;
					if (!batteryCell.IsEmpty)
					{
						batteryCell.PowerStored -= num;
						num = 0f;
					}
				}
			}
			if (num <= 0f && num3 > 0f && num4 > 0f)
			{
				float num5 = num4 / num3;
				num2 = ((num5 <= 0.2f) ? 1 : ((num5 <= 0.4f) ? 2 : ((num5 <= 0.6f) ? 3 : ((!(num5 <= 0.8f)) ? 5 : 4))));
			}
		}
		if (PoweredValue != num2)
		{
			OnServer.Interact(base.InteractPowered, num2);
		}
	}

	public void Recharge(float ammount)
	{
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if (InventoryManager.ParentHuman != null && InventoryManager.ParentHuman.ParentSlot != null && InventoryManager.ParentHuman.ParentSlot.Parent == this)
		{
			Transform transform = InventoryManager.ParentHuman.HelmetSlot?.Location;
			if (transform != null)
			{
				InventoryManager.Parent.CameraRig.position = transform.position;
			}
		}
		if (HasAuthority && base.VelocityMagnitude > MaxSpeed && !RigidBody.isKinematic)
		{
			RigidBody.velocity = Vector3.ClampMagnitude(RigidBody.velocity, MaxSpeed);
		}
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			HandlePlayerControl();
			if (!IsOccluded)
			{
				float pitchIndicator = _pitchIndicator;
				float rollIndicator = _rollIndicator;
				_speedIndicator = Mathf.Lerp(_speedIndicator, base.VelocityMagnitude, Time.deltaTime * 3f);
				_pitchIndicator = Mathf.Lerp(_pitchIndicator, GetPitch(), Time.deltaTime * 3f);
				_rollIndicator = Mathf.Lerp(_rollIndicator, 0f - GetRoll(), Time.deltaTime * 3f);
				BaseAnimator.SetFloat(SpeedHash, _speedIndicator);
				BaseAnimator.SetFloat(PitchHash, _pitchIndicator);
				BaseAnimator.SetFloat(RollHash, _rollIndicator);
				float num = ((Time.deltaTime > 0f) ? Time.deltaTime : 1f);
				float num2 = Mathf.Abs(_pitchIndicator - pitchIndicator) / num * 1.4f;
				float num3 = Mathf.Abs(_rollIndicator - rollIndicator) / num * 0.8f;
				float num4 = ((_deltaRollPitch < num3 + num2) ? 0.5f : 20f);
				_deltaRollPitch = Mathf.Lerp(_deltaRollPitch, num3 + num2, num4 * num);
			}
		}
	}

	private void HandlePlayerControl()
	{
		if (!GameManager.IsBatchMode && (bool)InventoryManager.Parent && GameManager.GameState != GameState.None)
		{
			if (InventoryManager.Parent == base.DriverSlot.Occupant)
			{
				HandleRoverMovement();
				return;
			}
			base.TargetMotorPower = 0f;
			base.TargetBrakePower = BrakePower;
		}
	}

	private void HandleRoverMovement()
	{
		if (base.ActiveRigidbody == null || !InventoryManager.AllowMouseControl)
		{
			return;
		}
		float num = 0f;
		float b = BrakePower;
		float num2 = 0f;
		foreach (Wheel wheel in Wheels)
		{
			num2 += wheel.WheelRpm;
		}
		num2 /= (float)Wheels.Count;
		if (KeyManager.GetButton(KeyMap.Forward))
		{
			num += MotorPower;
			b = 0f;
		}
		if (KeyManager.GetButtonUp(KeyMap.Forward))
		{
			b = BrakePower;
		}
		if (KeyManager.GetButton(KeyMap.Backward))
		{
			num -= MotorPower;
			b = 0f;
		}
		if (KeyManager.GetButtonUp(KeyMap.Backward))
		{
			b = BrakePower;
		}
		float num3 = 0f;
		if (KeyManager.GetButton(KeyMap.Left))
		{
			num3 -= SteeringPower;
		}
		if (KeyManager.GetButtonUp(KeyMap.Left))
		{
			num3 = 0f;
		}
		if (KeyManager.GetButton(KeyMap.Right))
		{
			num3 += SteeringPower;
		}
		if (KeyManager.GetButtonUp(KeyMap.Right))
		{
			num3 = 0f;
		}
		if (base.PlayableAreaState == PlayableAreaRule.Invalid)
		{
			Vector3 force = Transform.forward * num;
			float outOfBoundsMultiplier = GetOutOfBoundsMultiplier(force);
			num *= outOfBoundsMultiplier;
		}
		float time = Mathf.Clamp01(num2 / 100f);
		float num4 = MaxTurnAngle * MaxTurningAngleSpeedAdjustedCurve.Evaluate(time);
		float num5 = SteeringSpeed * TurningTimeSpeedAdjustedCurve.Evaluate(time);
		num3 = Mathf.Clamp(num3, 0f - num4, num4);
		base.TargetSteeringAngle = Mathf.Lerp(base.TargetSteeringAngle, num3, Time.deltaTime * num5);
		if (!Powered)
		{
			num = 0f;
		}
		float num6 = ((base.TargetMotorPower > num && base.TargetMotorPower > 0f) ? MotorSpeed : (MotorSpeed * 10f));
		base.TargetMotorPower = Mathf.Lerp(base.TargetMotorPower, num, Time.deltaTime * num6);
		base.TargetBrakePower = Mathf.Lerp(base.TargetBrakePower, b, Time.deltaTime * BrakeSpeed);
	}

	private float GetOutOfBoundsMultiplier(Vector3 force)
	{
		if (base.PlayableAreaState == PlayableAreaRule.Invalid)
		{
			return Mathf.Clamp01(Vector2.Dot(new Vector2(force.x, force.z), base.LastValidPlayablePosition - new Vector2(base.Position.x, base.Position.z)));
		}
		return 1f;
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!WorldManager.IsGamePaused)
		{
			if (IsOccluded)
			{
				SetEngineSound(soundEnabled: false, deltaTime);
			}
			else
			{
				SetEngineSound(OnOff && Powered && WheelsTurning(), deltaTime);
			}
		}
	}

	private void SetEngineSound(bool soundEnabled, float deltaTime)
	{
		if (soundEnabled == _engineSound)
		{
			if (soundEnabled)
			{
				SetEngineAudio(deltaTime);
			}
			return;
		}
		_engineSound = soundEnabled;
		if (soundEnabled)
		{
			PlaySound(CabinRattlesHash);
			PlaySound(EngineAudioHash);
			SetEngineAudio(deltaTime);
		}
		else
		{
			StopSound(EngineAudioHash);
			StopSound(CabinRattlesHash);
		}
	}

	public void SetEngineAudio(float deltaTime)
	{
		float num = Wheels[0].WheelRpm * 90f;
		_enginePitch = Mathf.Lerp(_enginePitch, Mathf.Clamp01(num / 8000f), deltaTime * 10f);
		float pitch = Mathf.Lerp(EnginePitchMin, EnginePitchMax, _enginePitch);
		float volumeMultiplier = Mathf.Clamp01(num / 1000f);
		GetAudioEvent(EngineAudioHash)?.SetVolumeAndPitch(volumeMultiplier, pitch);
		float volumeMultiplier2 = Mathf.Clamp01(_deltaRollPitch / 40f);
		GetAudioEvent(CabinRattlesHash)?.SetVolumeMultiplier(volumeMultiplier2);
	}

	public bool WheelsTurning()
	{
		return Wheels[0].WheelRpm > 0.0001f;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is Entity)
		{
			newChild.RigidBody.interpolation = RigidbodyInterpolation.None;
			string keyName = Localization.GetKeyName(KeyMap._Drop.Key);
			if (InventoryManager.ParentHuman == newChild)
			{
				AlertMessage.Show(GameStrings.PressToExit.AsString(keyName), 4f);
			}
		}
		if (newChild.HasAuthority)
		{
			if (InventoryManager.ParentHuman != null)
			{
				InventoryManager.ParentHuman.AllowChildInteraction = true;
			}
		}
		else
		{
			if (!(newChild is Container))
			{
				return;
			}
			foreach (Collider selfCollider in newChild._selfColliders)
			{
				selfCollider.isTrigger = true;
				selfCollider.enabled = true;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing newChild)
	{
		base.OnChildExitInventory(newChild);
		if (newChild is Entity)
		{
			newChild.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
		}
		if (newChild.HasAuthority)
		{
			InventoryManager.ParentHuman.AllowChildInteraction = false;
		}
		else
		{
			if (!(newChild is DynamicGasCanister) && !(newChild is Container))
			{
				return;
			}
			foreach (Collider selfCollider in newChild._selfColliders)
			{
				selfCollider.isTrigger = false;
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == base.DriverSlot.Action || interactable.Action == base.PassengerSlot.Action)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.GetIn
			};
			if ((bool)interactable.Slot.Occupant)
			{
				return delayedActionInstance.Fail(GameStrings.SlotAlreadyOccupied, interactable.Slot.ToTooltip(), interactable.Slot.Occupant.ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, interactable.Slot);
			}
			interaction.SourceThing.PlaySound(RoverEnterHash);
			return delayedActionInstance.Succeed();
		}
		switch (interactable.Action)
		{
		case InteractableType.Button1:
		{
			DelayedActionInstance delayedActionInstance3 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = "Headlights " + ((interactable.State == 1) ? "Off" : "On")
			};
			if (!doAction)
			{
				return delayedActionInstance3.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			}
			return delayedActionInstance3.Succeed();
		}
		case InteractableType.Button2:
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = "Cabin Lights " + ((interactable.State == 1) ? "Off" : "On")
			};
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			}
			return delayedActionInstance2.Succeed();
		}
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DelayedActionInstance result = base.AttackWith(attack, doAction);
		if (attack.SourceItem is Crowbar crowbar && crowbar.RootParentHuman != null)
		{
			Human rootParentHuman = crowbar.RootParentHuman;
			if (rootParentHuman == null || !rootParentHuman.IsGrounded(rootParentHuman.AllButPlayerImmune))
			{
				return result;
			}
			result = new DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = ActionStrings.FlipRover
			};
			if (!doAction)
			{
				return result;
			}
			if (GameManager.RunSimulation)
			{
				RigidBody.AddForceAtPosition(CrowBarForce, attack.Position);
				return result;
			}
		}
		if (attack.SourceItem is IRoverRepairer roverRepairer)
		{
			float num = roverRepairer.RepairQuantity(this);
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = num * roverRepairer.GetRepairSpeed() * RepairSpeedScale,
				ActionMessage = GameStrings.ActionRepairRover.DisplayString
			};
			if (DamageState.TotalRatio <= float.Epsilon)
			{
				return delayedActionInstance.Fail(GameStrings.StructureIsNotDamaged, ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			roverRepairer.Repair(base.netId, num * attack.CompletedRatio);
			return delayedActionInstance;
		}
		return result;
	}

	public static IContainerMount IsNearby(DynamicThing container)
	{
		Rover result = null;
		float num = MaxStorageMountDistance;
		for (int i = 0; i < AllRovers.Count; i++)
		{
			Rover rover = AllRovers[i];
			if ((bool)rover)
			{
				float num2 = RocketMath.DistanceSquared(container.CenterPosition, rover.CenterPosition);
				if (RocketMath.DistanceSquared(container.CenterPosition, rover.CenterPosition) < num)
				{
					result = rover;
					num = num2;
				}
			}
		}
		return result;
	}

	public bool Attach(Thing target = null)
	{
		bool flag = target is DynamicGasCanister;
		for (int i = 0; i < ConnectionSlots.Length; i++)
		{
			if (flag)
			{
				if (ConnectionSlots[i].TryAttachTank(target))
				{
					return true;
				}
			}
			else if (ConnectionSlots[i].TryAttachContainer(target))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnDamageDestroyed()
	{
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && GameManager.RunSimulation)
			{
				OnServer.MoveToWorld(slot.Occupant);
			}
		}
		base.OnDamageDestroyed();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(InterfaceStrings.CrowbarToFlip);
		return extendedText;
	}

	public Vector3 GetExitPosition(Entity human)
	{
		if (base.DriverSlot.Get<Entity>() == human)
		{
			return DriverExit.position;
		}
		if (base.PassengerSlot.Get<Entity>() == human)
		{
			return PassengerExit.position;
		}
		return DriverExit.position;
	}

	private Transform GetExitTransform(Human human)
	{
		if (base.DriverSlot.Get<Human>() == human)
		{
			return DriverExit;
		}
		if (base.PassengerSlot.Get<Human>() == human)
		{
			return PassengerExit;
		}
		return DriverExit;
	}

	public virtual void Exit(Human parent)
	{
		Transform exitTransform = GetExitTransform(parent);
		Vector3 worldPosition = exitTransform.position;
		if (Physics.CheckSphere(worldPosition, _radiusCheck))
		{
			Vector3 entityForward = parent.EntityForward;
			float z = exitTransform.localPosition.z;
			Vector3 vector = exitTransform.position - entityForward * z;
			worldPosition = vector - entityForward * z;
			if (Physics.CheckSphere(worldPosition, _radiusCheck))
			{
				Vector3 right = ThingTransform.right;
				worldPosition = vector + right * z;
				if (Physics.CheckSphere(worldPosition, _radiusCheck))
				{
					worldPosition = vector - right * z;
				}
			}
		}
		parent.MoveToWorld(worldPosition, parent.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
	}

	public Transform GetCameraPoint(Entity entity)
	{
		if (base.DriverSlot.Get<Entity>() == entity)
		{
			return CameraPointDriver;
		}
		if (base.PassengerSlot.Get<Entity>() == entity)
		{
			return CameraPointPassenger;
		}
		return CameraPointDriver;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RoverSaveData roverSaveData)
		{
			roverSaveData.LastValidPlayablePosition = base.LastValidPlayablePosition;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoverSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is RoverSaveData roverSaveData)
		{
			base.LastValidPlayablePosition = roverSaveData.LastValidPlayablePosition;
		}
	}
}
