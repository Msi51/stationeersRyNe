using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects;

public class LanderCapsule : DynamicThing, IExitable, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, IVolume, IOnEachDay
{
	[SerializeField]
	private Transform _exitPoint;

	[SerializeField]
	private Transform _cameraPoint;

	[SerializeField]
	private GameObject _doorOpenTrigger;

	[SerializeField]
	public EntryEffects _entryEffects;

	[SerializeField]
	public Transform gasEffect;

	public Light InteriorLight;

	private static EnumCollection<LanderMode, byte> _landerModes = new EnumCollection<LanderMode, byte>(toProper: false);

	private CancellationTokenWrapper _descentCancellation = new CancellationTokenWrapper();

	public const float RENDER_DISTANCE = 200f;

	private const float RENDER_MAX_DISTANCE_SQUARED = 40000f;

	private const float SHADOW_MAX_DISTANCE_SQUARED = 200f;

	private const float DOOR_EJECT_FORCE = 9f;

	public float DynamicTriggerHeight = 10f;

	public float KinematicStartHeight = 100f;

	public const float DURATION = 10f;

	private const float ENGINE_START_TIME = 3f;

	private float _controlledDescentLerp;

	[SerializeField]
	private float _volume = 100f;

	private bool _isLightRunning;

	private bool _hasCollidedWithGround;

	private Vector3 _positionBeforeBeginDescent;

	private bool _decentInitialised;

	private float _shakeMagnitude;

	private const float DESCENT_SHAKE = 0.03f;

	private const float ENGINES_SHAKE = 0.2f;

	public override float LavaDamage => 0f;

	private Slot DoorSlot => Slots[0];

	private Slot SeatSlot => Slots[1];

	public override string[] ModeStrings => _landerModes.Names;

	public LanderMode LanderMode => (LanderMode)Mode;

	public bool IsDescending => LanderMode == LanderMode.Descending;

	public float ControlledDescentLerp
	{
		get
		{
			return _controlledDescentLerp;
		}
		set
		{
			_controlledDescentLerp = value;
			_entryEffects.SetIntensity(ControlledDescentLerp);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public VolumeLitres Volume => new VolumeLitres(_volume);

	public bool FreeLook => true;

	public VolumeLitres GetVolume => Volume;

	protected override float GetRenderMaxDistanceSquared()
	{
		if (!IsDescending)
		{
			return base.GetRenderMaxDistanceSquared();
		}
		return 40000f;
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		if (!IsDescending)
		{
			return base.GetShadowMaxDistanceSquared();
		}
		return 200f;
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, Volume, 0L));
		}
	}

	public void OnNewDay()
	{
		if (_isLightRunning)
		{
			OnServer.Interact(base.InteractPowered, 0);
			_isLightRunning = false;
		}
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if (LanderMode == LanderMode.Venting && !GameManager.IsBatchMode)
		{
			PipeLeak.EmitAtLocation(gasEffect.position, gasEffect.rotation.eulerAngles);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LanderCapsuleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (IsOpen)
		{
			DisableDoorTrigger();
		}
		if (LanderMode == LanderMode.Descending || !IsOpen)
		{
			WaitLoadingThenTerminateDescent().Forget();
			if (_positionBeforeBeginDescent != Vector3.zero)
			{
				Transform.position = _positionBeforeBeginDescent;
			}
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (IsOpen)
		{
			DisableDoorTrigger();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteFloatHalf(ControlledDescentLerp);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ControlledDescentLerp = reader.ReadFloatHalf();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteFloatHalf(ControlledDescentLerp);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ControlledDescentLerp = reader.ReadFloatHalf();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsDescending && _decentInitialised && !(other == null) && !other.isTrigger)
		{
			TerminateDescent();
		}
	}

	public override void OnCollisionEnter(Collision collision)
	{
		base.OnCollisionEnter(collision);
		if (collision.gameObject.layer == (int)Layers.Terrain && !_hasCollidedWithGround && !IsOpen)
		{
			_hasCollidedWithGround = true;
			PlaySound(Defines.Sounds.CapsuleImpact);
		}
	}

	private async UniTaskVoid BeginDescent()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.Yield();
			if (GameManager.GameState == GameState.None)
			{
				return;
			}
		}
		_decentInitialised = true;
		RigidBody.isKinematic = true;
		_positionBeforeBeginDescent = Transform.position;
		DynamicTriggerHeight = Transform.position.y;
		KinematicStartHeight += DynamicTriggerHeight;
		Transform.position += Vector3.up * KinematicStartHeight;
		OnServer.Interact(base.InteractLock, 1);
		_descentCancellation.CancelAndInitialize();
		ControlledDescent(_descentCancellation.Token).Forget();
	}

	private async UniTaskVoid WaitLoadingThenTerminateDescent()
	{
		await UniTask.WaitUntil(() => GameManager.GameState != GameState.Loading);
		TerminateDescent();
	}

	private void TerminateDescent()
	{
		_descentCancellation.Cancel();
		RigidBody.isKinematic = false;
		_entryEffects.DisableEffects();
		CameraController.SetCameraShake(0f);
		if (GameManager.RunSimulation)
		{
			if (LanderMode != LanderMode.AtRest)
			{
				OnServer.Interact(base.InteractMode, 0);
			}
			if (Activate != 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			WaitThenOpen().Forget();
		}
	}

	private async UniTaskVoid WaitThenOpen()
	{
		await UniTask.Delay(3000);
		if (!IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
		await UniTask.Delay(500);
		OnServer.Interact(base.InteractLock, 0);
		if (GameManager.RunSimulation && SeatSlot.Get<Entity>() == InventoryManager.Parent)
		{
			SaveHelper.Save(XmlSaveLoad.Instance.CurrentStationName, default(CancellationToken)).Forget();
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (_shakeMagnitude > 0f)
		{
			if (SeatSlot.Get<Entity>() == InventoryManager.Parent)
			{
				CameraController.SetCameraShake(_shakeMagnitude);
			}
			_shakeMagnitude -= _shakeMagnitude / 2f * Time.deltaTime;
			if (_shakeMagnitude <= 0f)
			{
				CameraController.SetCameraShake(0f);
			}
		}
	}

	private async UniTaskVoid ControlledDescent(CancellationToken cancellationToken)
	{
		float timeElapsed = 0f;
		Vector3 startingPosition = new Vector3(Transform.position.x, KinematicStartHeight, Transform.position.z);
		Vector3 targetPosition = new Vector3(Transform.position.x, DynamicTriggerHeight, Transform.position.z);
		while (timeElapsed < 10f && !cancellationToken.IsCancellationRequested)
		{
			if ((object)Transform == null)
			{
				return;
			}
			float num = timeElapsed / 10f;
			if (timeElapsed > 3f && Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			float t = EaseOutQuad(num);
			Transform.position = Vector3.Lerp(startingPosition, targetPosition, t);
			ControlledDescentLerp = num;
			timeElapsed += Time.deltaTime;
			await UniTask.Yield(PlayerLoopTiming.Update);
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			Transform.position = targetPosition;
			RigidBody.isKinematic = false;
			_entryEffects.DisableEffects();
			TerminateDescent();
		}
	}

	private static float EaseOutQuad(float x)
	{
		return 1f - (1f - x) * (1f - x);
	}

	private void DisableDoorTrigger()
	{
		_doorOpenTrigger.SetActive(value: false);
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		base.OnInteractableStateChanged(interactable, newState, oldState);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Mode && newState != oldState && newState == 1)
		{
			BeginDescent().Forget();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered && interactable.State == 0)
		{
			InteriorLight.enabled = false;
		}
		else if (interactable.Action == InteractableType.Open && IsOpen)
		{
			if (DoorSlot.Contains<DynamicThing>(out var occupant))
			{
				DisableDoorTrigger();
				OnServer.Interact(occupant, InteractableType.Open, 1);
				OnServer.MoveToWorld(occupant, occupant.Position, occupant.Rotation, occupant.Transform.forward * 9f, Vector3.zero);
			}
			OnServer.Interact(base.InteractMode, 2);
		}
		else if (interactable.Action == InteractableType.Mode)
		{
			switch (LanderMode)
			{
			case LanderMode.AtRest:
				_entryEffects.DisableEffects();
				_shakeMagnitude = 0f;
				break;
			case LanderMode.Descending:
				_shakeMagnitude = 0.03f;
				break;
			case LanderMode.Venting:
				if (GameManager.RunSimulation)
				{
					WaitThenDisableLeakEffect().Forget();
				}
				break;
			}
		}
		else if (interactable.Action == InteractableType.Activate)
		{
			switch (interactable.State)
			{
			case 0:
				_entryEffects.DisableEffects();
				break;
			case 1:
				_entryEffects.EnableEffects();
				_shakeMagnitude = 0.2f;
				break;
			}
		}
	}

	private async UniTaskVoid WaitThenDisableLeakEffect()
	{
		await UniTask.Delay(3000);
		OnServer.Interact(base.InteractMode, 0);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Slot2)
		{
			return HandleSeatSlot(interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private DelayedActionInstance HandleSeatSlot(Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = ActionStrings.GetIn
		};
		DynamicThing occupant = interaction.SourceSlot.Occupant;
		if ((bool)SeatSlot.Occupant || (bool)occupant)
		{
			return HandleSwitch(interaction, SeatSlot.SlotIndex, delayedActionInstance, doAction);
		}
		if (GameManager.RunSimulation && doAction)
		{
			OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, SeatSlot);
		}
		return delayedActionInstance.Succeed();
	}

	public override bool IsSoundLocal()
	{
		if (SeatSlot.Get<Entity>() == InventoryManager.Parent)
		{
			return true;
		}
		return false;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_descentCancellation.Cancel();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOpen)
		{
			Atmosphere outputAtmos = AtmosphericsController.World.CloneGlobalAtmosphere(new WorldGrid(base.Position), 0L);
			AtmosphereHelper.Mix(base.InternalAtmosphere, outputAtmos, AtmosphereHelper.MatterState.All);
		}
	}

	public Vector3 GetExitPosition(Entity human)
	{
		return _exitPoint.position;
	}

	public void Exit(Human human)
	{
		human.MoveToWorld(GetExitPosition(human), human.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
	}

	public Transform GetCameraPoint(Entity entity)
	{
		return _cameraPoint;
	}
}
