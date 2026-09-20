using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Sound;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts;

public class ElevatorCarrage : DynamicThing
{
	private byte _elevatormode;

	public ElevatorShaft CurrentShaft;

	public ElevatorShaftNetwork ShaftNetwork;

	private int _levelTarget;

	private static readonly int ElevatorSoundHash = Animator.StringToHash("activate");

	private GameAudioEvent _elevatorSound;

	public List<DigitGameObject> DigitReferences = new List<DigitGameObject>();

	private Dictionary<int, DigitGameObject> _digitLookup = new Dictionary<int, DigitGameObject>();

	private DigitGameObject _currentDigitRef;

	private Grid3 _lastGrid;

	private float _speed = ElevatorShaft.MinSpeed;

	private int _levelTargetSaved;

	private float _savedSpeed;

	[ByteArraySync]
	private ElevatorMode _ElevatorMode
	{
		get
		{
			return (ElevatorMode)_elevatormode;
		}
		set
		{
			_elevatormode = (byte)value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	[ByteArraySync]
	public int LevelTarget
	{
		get
		{
			return _levelTarget;
		}
		set
		{
			_levelTarget = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			RefreshDigitReference();
		}
	}

	public ElevatorMode ElevatorMode => _ElevatorMode;

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (Activate != 0)
		{
			float t = Mathf.Max(_speed - 1.5f, 0f) / (MaxMovementSpeed - 1.5f);
			if (_elevatorSound?.AudioSource != null)
			{
				_elevatorSound.AudioSource.SetPitchMultiplier(ElevatorSoundHash, Mathf.Lerp(1f, 1.75f, t));
				_elevatorSound.AudioSource.SetVolumeMultiplier(ElevatorSoundHash, Mathf.Lerp(1f, 2.5f, t));
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte((byte)_ElevatorMode);
			writer.WriteInt32(LevelTarget);
			writer.WriteSingle(ShaftNetwork?.Speed ?? 1.5f);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			_ElevatorMode = (ElevatorMode)reader.ReadByte();
			LevelTarget = reader.ReadInt32();
			float speed = reader.ReadSingle();
			if (ShaftNetwork != null)
			{
				ShaftNetwork.Speed = speed;
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)_ElevatorMode);
		writer.WriteInt32(LevelTarget);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_ElevatorMode = (ElevatorMode)reader.ReadByte();
		LevelTarget = reader.ReadInt32();
	}

	public void SetLevelTarget(int target)
	{
		LevelTarget = (int)Mathf.Clamp(target, 0f, DigitReferences.Count);
	}

	public void SetElevatorMode(ElevatorMode elevatorMode)
	{
		_ElevatorMode = elevatorMode;
		switch (elevatorMode)
		{
		case ElevatorMode.Stationary:
			OnServer.Interact(base.InteractActivate, 0);
			break;
		case ElevatorMode.Upward:
		case ElevatorMode.Downward:
			OnServer.Interact(base.InteractActivate, 1);
			break;
		}
	}

	private async UniTaskVoid WaitThenRegister()
	{
		await UniTask.WaitUntil(() => GameManager.GameState == GameState.Running);
		await UniTask.WaitForEndOfFrame();
		ElevatorShaft elevatorShaft = SmallCell.Get<ElevatorShaft>(base.ThingTransformPosition);
		if ((bool)elevatorShaft && !CurrentShaft)
		{
			CurrentShaft = elevatorShaft;
			CurrentShaft.ShaftNetwork.Register(this);
		}
		else if (GameManager.RunSimulation && !CurrentShaft)
		{
			OnServer.Destroy(this);
		}
		SetLevelTarget(_levelTargetSaved);
	}

	private void RefreshDigitReference()
	{
		int key = Mathf.Clamp(LevelTarget, 0, 99);
		if (_currentDigitRef != null)
		{
			_currentDigitRef.IsVisible(isVisible: false);
		}
		_digitLookup.TryGetValue(key, out _currentDigitRef);
		if (_currentDigitRef != null)
		{
			_currentDigitRef.IsVisible(isVisible: true);
		}
	}

	public override void Awake()
	{
		base.Awake();
		WaitThenRegister().Forget();
		_elevatorSound = GetAudioEvent(ElevatorSoundHash);
		foreach (DigitGameObject digitReference in DigitReferences)
		{
			_digitLookup.Add(digitReference.Digit, digitReference);
		}
	}

	public void TeleportTo(ElevatorShaft elevatorShaft)
	{
		CurrentShaft = elevatorShaft;
		RigidBody.MovePosition(elevatorShaft.ThingTransformPosition + elevatorShaft.ThingTransform.forward);
	}

	protected override void PhysicsOnRender(bool isRendered)
	{
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!Powered)
			{
				if (!CurrentShaft.OnOff)
				{
					return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.ShaftNotOn);
				}
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceNoPower);
			}
			switch (interactable.Action)
			{
			case InteractableType.Button1:
				delayedActionInstance.ActionMessage = ActionStrings.Up;
				if (doAction && GameManager.RunSimulation && ShaftNetwork != null && LevelTarget < ShaftNetwork.Shafts.Count)
				{
					for (int i = LevelTarget + 1; i < ShaftNetwork.Shafts.Count; i++)
					{
						if ((bool)(ShaftNetwork.Shafts[i] as ElevatorLevel))
						{
							SetLevelTarget(i);
							break;
						}
					}
					RefreshMovement();
				}
				return delayedActionInstance;
			case InteractableType.Button2:
				delayedActionInstance.ActionMessage = ActionStrings.Down;
				if (doAction && GameManager.RunSimulation && ShaftNetwork != null && LevelTarget > 0)
				{
					int num = LevelTarget - 1;
					while (num > 0 && (num >= ShaftNetwork.Shafts.Count || !(ShaftNetwork.Shafts[num] is ElevatorLevel)))
					{
						num--;
					}
					SetLevelTarget(num);
					RefreshMovement();
				}
				return delayedActionInstance;
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public void RefreshMovement()
	{
		if ((bool)CurrentShaft)
		{
			if (LevelTarget > CurrentShaft.ShaftLevel)
			{
				SetElevatorMode(ElevatorMode.Upward);
				CurrentShaft.ShaftNetwork.RefreshLevelState();
			}
			else if (LevelTarget < CurrentShaft.ShaftLevel)
			{
				SetElevatorMode(ElevatorMode.Downward);
			}
			else
			{
				SetElevatorMode(ElevatorMode.Stationary);
			}
			CurrentShaft.ShaftNetwork.RefreshLevelState();
			CurrentShaft.CheckCarrageState(this);
		}
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		WorldCenterOfMass = base.ActiveRigidbody.worldCenterOfMass;
		Rotation = base.ActiveRigidbody.rotation;
		base.Position = base.ActiveRigidbody.position;
		if (GameManager.RunSimulation && ShaftNetwork != null && PoweredValue != ShaftNetwork.PoweredValue)
		{
			OnServer.Interact(this, InteractableType.Powered, ShaftNetwork.PoweredValue);
		}
		if (ShaftNetwork != null && ShaftNetwork.Powered)
		{
			switch (_ElevatorMode)
			{
			case ElevatorMode.Upward:
				SetPosition(Vector3.up, invert: true);
				break;
			case ElevatorMode.Downward:
				SetPosition(Vector3.down, invert: false);
				break;
			case ElevatorMode.Stationary:
				break;
			}
		}
	}

	private void SetPosition(Vector3 direction, bool invert)
	{
		float num = Mathf.Abs(base.gameObject.transform.position.y - ShaftNetwork.Shafts[LevelTarget].gameObject.transform.position.y - 0.5f);
		float t = Mathf.Clamp01(num / CurrentShaft.Speed);
		float num2 = Mathf.Lerp(ElevatorShaft.MinSpeed, CurrentShaft.Speed, t);
		if (num2 > num)
		{
			num2 = Mathf.Max(num, ElevatorShaft.MinSpeed);
		}
		float num3 = ((num2 > _speed) ? 1f : 2.5f);
		_speed = Mathf.Lerp(_speed, num2, Time.fixedDeltaTime * num3);
		Vector3 vector = base.ThingTransformPosition + direction * _speed * Time.fixedDeltaTime;
		ElevatorShaft elevatorShaft = (invert ? SmallCell.Get<ElevatorShaft>(vector - direction * 0.25f) : SmallCell.Get<ElevatorShaft>(vector + direction * 0.25f));
		if ((bool)elevatorShaft)
		{
			RigidBody.MovePosition(vector);
			CurrentShaft = elevatorShaft;
			if (CurrentShaft.StopCarrage(this))
			{
				SetElevatorMode(ElevatorMode.Stationary);
				_speed = ElevatorShaft.MinSpeed;
				CurrentShaft.ShaftNetwork.RefreshLevelState();
				CurrentShaft.CheckCarrageState(this);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		ElevatorCarrageSaveData elevatorCarrageSaveData = savedData as ElevatorCarrageSaveData;
		if (GameManager.GameState != GameState.None && elevatorCarrageSaveData != null)
		{
			elevatorCarrageSaveData.LevelTarget = LevelTarget;
			elevatorCarrageSaveData.Speed = ShaftNetwork?.Speed ?? 1.5f;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ElevatorCarrageSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is ElevatorCarrageSaveData elevatorCarrageSaveData)
		{
			_levelTargetSaved = elevatorCarrageSaveData.LevelTarget;
			_savedSpeed = elevatorCarrageSaveData.Speed;
		}
		WaitThenRegister().Forget();
		SetElevatorSpeedOnLoad().Forget();
	}

	private async UniTaskVoid SetElevatorSpeedOnLoad()
	{
		await UniTask.WaitUntil(() => CurrentShaft != null && ShaftNetwork != null);
		ShaftNetwork.Speed = Mathf.Clamp(_savedSpeed, ElevatorShaft.MinSpeed, ShaftNetwork.Carrage.MaxMovementSpeed);
	}
}
