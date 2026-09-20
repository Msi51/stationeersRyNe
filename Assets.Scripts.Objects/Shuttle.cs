using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects;
using Trading;
using UI;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Shuttle : DraggableThing, IExitable, IRepairable, IPlayerVehicle, IReferencable, IEvaluable, IVehicleCamera
{
	public Transform CameraPointThirdPerson;

	public Transform CameraPointDriver;

	public Transform CameraPointPassenger;

	private Transform _cameraRig;

	public int DriverSlotIndex;

	public int PassengerSlotIndex = 1;

	private Vector3 _previousCameraPosition;

	public List<DevicePart> Switches = new List<DevicePart>();

	public Transform DriverExit;

	public Transform PassengerExit;

	public float MaxSpeed = 12f;

	private static int RoverEnterHash = Animator.StringToHash("RoverEnter");

	private static readonly float _radiusCheck = 0.05f;

	private static readonly int GearHash = Animator.StringToHash("Gear");

	public float EnginePower = 1000f;

	public float ThrustSmoothTime = 1f;

	private Vector3 _currentThrottle;

	private Vector3 _throttleVelocity;

	private Quaternion _rotationVelocity;

	private float _targetYaw;

	private float _targetPitch;

	private float _targetRoll;

	private float _currentYaw;

	private float _currentRoll;

	private float _currentPitch;

	private float _yawVelocity;

	private float _pitchVelocity;

	private float _rollVelocity;

	[Header("Shuttle")]
	public float RotationSpeed = 90f;

	public float SmoothTime = 0.1f;

	public float MaxPitch = 20f;

	public float MaxRoll = 15f;

	public AnimationCurve RollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public List<Thruster> Thrusters = new List<Thruster>();

	public Slot DriverSlot => Slots[DriverSlotIndex];

	public Slot PassengerSlot => Slots[PassengerSlotIndex];

	public override bool HasAuthority
	{
		get
		{
			if (DriverSlot.IsEmpty() && GameManager.RunSimulation)
			{
				return true;
			}
			if (!GameManager.IsBatchMode && DriverSlot.Get<Human>() == InventoryManager.ParentHuman)
			{
				return true;
			}
			return false;
		}
	}

	public bool FreeLook => false;

	public float RepairRatio => DamageState.TotalRatio;

	public override float MaxMovementSpeed => MaxSpeed;

	public Transform GetVehicleCameraTransform()
	{
		return CameraPointThirdPerson;
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		RigidBody.constraints = RigidbodyConstraints.FreezeRotation;
		foreach (Thruster thruster in Thrusters)
		{
			thruster.SetActive(active: false);
		}
	}

	public override void Awake()
	{
		base.Awake();
		RigidBody.maxDepenetrationVelocity = 10f;
		foreach (DevicePart @switch in Switches)
		{
			@switch.RefreshState(skipAnim: true);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		foreach (Slot slot in newChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: false, hideOnPlayer: true, isRecursive: true);
			}
		}
		if (newChild is Entity)
		{
			newChild.RigidBody.interpolation = RigidbodyInterpolation.None;
			string keyName = Localization.GetKeyName(KeyMap._Drop.Key);
			if (InventoryManager.ParentHuman == newChild)
			{
				AlertMessage.Show(GameStrings.PressToExit.AsString(keyName), 4f);
			}
		}
		if (newChild.HasAuthority && InventoryManager.ParentHuman != null)
		{
			InventoryManager.ParentHuman.AllowChildInteraction = true;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		foreach (Slot slot in previousChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: true, hideOnPlayer: false, isRecursive: true);
			}
		}
		if (previousChild is Entity)
		{
			previousChild.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
		}
		if (previousChild.HasAuthority)
		{
			InventoryManager.ParentHuman.AllowChildInteraction = false;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		foreach (DevicePart @switch in Switches)
		{
			@switch.RefreshState(skipAnimation);
		}
	}

	public Vector3 GetExitPosition(Entity human)
	{
		if (DriverSlot.Get<Entity>() == human)
		{
			return DriverExit.position;
		}
		if (PassengerSlot.Get<Entity>() == human)
		{
			return PassengerExit.position;
		}
		return DriverExit.position;
	}

	private Transform GetExitTransform(Human human)
	{
		if (DriverSlot.Get<Human>() == human)
		{
			return DriverExit;
		}
		if (PassengerSlot.Get<Human>() == human)
		{
			return PassengerExit;
		}
		return DriverExit;
	}

	public Transform GetCameraPoint(Entity entity)
	{
		if (DriverSlot.Get<Entity>() == entity)
		{
			return CameraPointDriver;
		}
		if (PassengerSlot.Get<Entity>() == entity)
		{
			return CameraPointPassenger;
		}
		return CameraPointDriver;
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
			base.ActiveRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			HandlePlayerControl();
		}
		else
		{
			base.ActiveRigidbody.interpolation = RigidbodyInterpolation.None;
		}
		base.ActiveRigidbody.drag = ((OnOff && DriverSlot.Contains<Human>()) ? 0.5f : 0f);
		if (HasAuthority && base.VelocityMagnitude > MaxSpeed && !RigidBody.isKinematic)
		{
			RigidBody.velocity = Vector3.ClampMagnitude(RigidBody.velocity, MaxSpeed);
		}
		if (GameManager.RunSimulation && OnOff)
		{
			float magnitude = new Vector2(RigidBody.velocity.x, RigidBody.velocity.z).magnitude;
			if (magnitude < 0.5f && Activate == 0 && Physics.Raycast(new Ray(Transform.position, Vector3.down), 2f))
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			else if (magnitude > 4f && Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
		if (WorldManager.HasGravityAtHeight(Transform.position.y) && !OnOff)
		{
			base.ActiveRigidbody.AddForce(Vector3.up * (WorldManager.WorldGravity * base.ActiveRigidbody.mass * 20f), ForceMode.Force);
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded)
		{
			UpdateThrusterExhausts();
		}
	}

	public override void BuildOwnerUpdate(RocketBinaryWriter writer)
	{
		base.BuildOwnerUpdate(writer);
		new ShuttleUpdate(_currentThrottle, _yawVelocity, _pitchVelocity).Write(writer);
	}

	public override void ProcessOwnerUpdate(RocketBinaryReader reader)
	{
		base.ProcessOwnerUpdate(reader);
		ShuttleUpdate shuttleUpdate = default(ShuttleUpdate);
		shuttleUpdate.Read(reader);
		Apply(shuttleUpdate);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		new ShuttleUpdate(_currentThrottle, _yawVelocity, _pitchVelocity).Write(writer);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		ShuttleUpdate shuttleUpdate = default(ShuttleUpdate);
		shuttleUpdate.Read(reader);
		Apply(shuttleUpdate);
	}

	private void Apply(ShuttleUpdate shuttleUpdate)
	{
		_currentThrottle = shuttleUpdate.CurrentThrottle;
		_yawVelocity = shuttleUpdate.YawVelocity;
		_pitchVelocity = shuttleUpdate.PitchVelocity;
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		base.OnInteractableStateChanged(interactable, newState, oldState);
		if (interactable.Action == InteractableType.OnOff)
		{
			base.InteractPowered.State = newState;
			RigidBody.constraints = ((newState == 1) ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None);
			foreach (Thruster thruster in Thrusters)
			{
				thruster.GameObject.SetActive(newState == 1);
			}
		}
		if (interactable.Action == InteractableType.Activate)
		{
			BaseAnimator.SetBool(GearHash, interactable.State == 1);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == DriverSlot.Action || interactable.Action == PassengerSlot.Action)
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
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = GameStrings.LandingGear
			};
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance2.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
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

	private void HandlePlayerControl()
	{
		if (!GameManager.IsBatchMode && GameManager.GameState != GameState.None && (object)InventoryManager.Parent != null)
		{
			if (DriverSlot.Contains(InventoryManager.Parent))
			{
				HandleMovement();
			}
			if (DriverSlot.Contains(InventoryManager.Parent) || PassengerSlot.Contains(InventoryManager.Parent))
			{
				HandleCamera();
			}
		}
	}

	private void HandleCamera()
	{
		Quaternion rotation = Quaternion.Euler(CameraPointThirdPerson.rotation.eulerAngles.x, CameraPointThirdPerson.rotation.eulerAngles.y, 0f);
		Vector3 vector = CameraPointThirdPerson.position;
		CameraPointThirdPerson.SetPositionAndRotation(vector, rotation);
	}

	public override void OnGravityEnabled()
	{
		RigidBody.useGravity = (object)DriverSlot.Occupant == null;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return GameStrings.LandingGear.DisplayString;
		}
		return base.GetContextualName(interactable);
	}

	private void HandleMovement()
	{
		if (!base.ActiveRigidbody)
		{
			return;
		}
		bool num = OnOff && InventoryManager.AllowMouseControl;
		Vector3 zero = Vector3.zero;
		if (num)
		{
			if (KeyManager.GetButton(KeyMap.Forward))
			{
				zero += Vector3.forward * 2f;
			}
			if (KeyManager.GetButton(KeyMap.Backward))
			{
				zero += Vector3.back * 0.66f;
			}
			if (KeyManager.GetButton(KeyMap.Left))
			{
				zero += Vector3.left * 0.8f;
			}
			if (KeyManager.GetButton(KeyMap.Right))
			{
				zero += Vector3.right * 0.8f;
			}
			zero += Vector3.up * (KeyManager.GetAscend() * 0.66f);
			zero += Vector3.down * (KeyManager.GetDescend() * 0.33f);
			float num2 = Singleton<InputManager>.Instance.GetAxis("LookX");
			if (KeyManager.HasAxis(ControllerMap.HorizontalLook))
			{
				num2 = Mathf.Clamp(num2 + ControllerMap.HorizontalLook.Output, -1f, 1f);
			}
			float num3 = Singleton<InputManager>.Instance.GetAxis("LookY");
			if (KeyManager.HasAxis(ControllerMap.VerticalLook))
			{
				num3 = Mathf.Clamp(num3 + ControllerMap.VerticalLook.Output, -1f, 1f);
			}
			_targetYaw += num2 * RotationSpeed * Time.fixedDeltaTime;
			_targetPitch += (0f - num3) * RotationSpeed * Time.fixedDeltaTime;
		}
		_currentThrottle.x = Mathf.SmoothDamp(_currentThrottle.x, zero.x, ref _throttleVelocity.x, ThrustSmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		_currentThrottle.y = Mathf.SmoothDamp(_currentThrottle.y, zero.y, ref _throttleVelocity.y, ThrustSmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		_currentThrottle.z = Mathf.SmoothDamp(_currentThrottle.z, zero.z, ref _throttleVelocity.z, ThrustSmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		Vector3 force = base.transform.TransformDirection(_currentThrottle) * EnginePower;
		base.ActiveRigidbody.AddForce(force, ForceMode.Force);
		_targetPitch = Mathf.Clamp(_targetPitch, 0f - MaxPitch, MaxPitch);
		_currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, SmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		_currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, SmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		float f = Mathf.Clamp(_yawVelocity / RotationSpeed, -1f, 1f);
		float num4 = RollCurve.Evaluate(Mathf.Abs(f)) * Mathf.Sign(f);
		_targetRoll = (0f - num4) * MaxRoll;
		_currentRoll = Mathf.SmoothDampAngle(_currentRoll, _targetRoll, ref _rollVelocity, SmoothTime * 0.5f, float.PositiveInfinity, Time.fixedDeltaTime);
		Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, _currentRoll);
		base.ActiveRigidbody.MoveRotation(rot);
	}

	private void UpdateThrusterExhausts()
	{
		Vector3 normalized = Transform.TransformDirection(_currentThrottle).normalized;
		float magnitude = _currentThrottle.magnitude;
		float f = Mathf.Clamp(_yawVelocity / RotationSpeed, -1f, 1f);
		float num = Mathf.Abs(f) * 0.2f;
		Vector3 vector = Transform.right * Mathf.Sign(f);
		float f2 = Mathf.Clamp(_pitchVelocity / RotationSpeed, -1f, 1f);
		float num2 = Mathf.Abs(f2) * 0.2f;
		Vector3 vector2 = -Transform.forward * Mathf.Sign(f2);
		Vector3 throttleDir = normalized * magnitude + vector * num + vector2 * num2;
		float throttleMagnitude = magnitude + num + num2;
		if (throttleDir.sqrMagnitude > 0.0001f)
		{
			throttleDir.Normalize();
		}
		foreach (Thruster thruster in Thrusters)
		{
			thruster.UpdateScale(throttleDir, throttleMagnitude);
		}
	}
}
