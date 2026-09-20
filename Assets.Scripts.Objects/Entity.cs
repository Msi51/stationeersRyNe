using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Objects.Electrical;
using Trading;
using UI.ImGuiUi;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects;

public class Entity : DynamicThing, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, IThermal, IDraggable, IReferencable, IEvaluable, IOnEachDay
{
	public static List<Entity> AllEntities = new List<Entity>();

	[Header("Entity")]
	[SerializeField]
	private Transform characterRotationY;

	[ReadOnly]
	public SkinnedMeshRendererInstance[] SkinnedMeshes;

	public Transform RigRoot;

	[ReadOnly]
	[SerializeField]
	public List<RagdollPart> RagdollParts = new List<RagdollPart>();

	[SerializeField]
	private List<RagdollTransformDefaults> _ragdollPartsToReset;

	private float _currency = 250f;

	[ReadOnly]
	public Human AsHuman;

	public const int DECAY_TIME_SECS = 60;

	public const int MINIMUM_DECAY_TIME_WHEN_DROPPED = 60;

	public const float ENTITY_MOLE_PER_BREATH = 0.0048f;

	public const float ENTITY_TOXIN_ABSORPTION = 0.0012f;

	public float WarningOxygen = 1f;

	public float CriticalOxygen = 0.75f;

	public float WarningNutrition = 2f;

	public float CriticalNutrition = 1f;

	public float WarningHydration = 2f;

	public float CriticalHydration = 1f;

	public float FullNutrition = 4.5f;

	public float WarningHealth = 0.25f;

	public float CriticalHealth = 0.75f;

	public float WarningMood = 0.5f;

	public float CriticalMood;

	public float WarningHygiene = 0.25f;

	public float CriticalHygiene;

	public float WarningStun = 0.05f;

	public float CriticalStun = 0.75f;

	public float CautionSanitation = 0.75f;

	public float CriticalSanitation = 0.9f;

	public static readonly PressurekPa ToxicPartialPressureForDamage = new PressurekPa(1.0);

	public static readonly PressurekPa ToxicPartialPressureForWarning = new PressurekPa(0.5);

	[ReadOnly]
	public MovementController MovementController;

	[ReadOnly]
	public CameraController CameraController;

	[ReadOnly]
	public Transform CameraRig;

	private SkinnedMeshRenderer _bodySkin;

	public AnimationUpdate LastAnimationUpdate;

	private bool _reparentRagDoll;

	private Vector3 _ragDollParentPosition;

	private float _baseMass;

	public bool AllowChildInteraction;

	private float _oxygenQuality;

	private const float _jointForwardOffset = 1.5f;

	private readonly Vector3 _jointOffSet = Vector3.forward / 1.5f + new Vector3(0f, 0.25f, 0f);

	private EntityState _state;

	protected EntityState _oldState;

	public Event OnStateChangeEvent;

	[ReadOnly]
	public Slot BrainSlot;

	[ReadOnly]
	public Slot LungsSlot;

	[ReadOnly]
	public Brain OrganBrain;

	[ReadOnly]
	public Lungs OrganLungs;

	[ReadOnly]
	public List<Organ> Organs = new List<Organ>();

	public Animator Animator;

	public Collider Collider;

	private Quaternion _rotationBeforeRagdoll;

	private bool _recentParent;

	public const float ENERGY_RELEASED_PER_TICK = 0.1f;

	public static readonly MoleEnergy EnergyReleasedPerTick = new MoleEnergy(0.10000000149011612);

	public static readonly TemperatureKelvin BodyTemperature = Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(37.0);

	public const float UNCONSCIOUS_METABOLIC_DAMAGE_SCALE = 0.33f;

	public const float UNCONSCIOUS_DEHYDRATION_SCALE = 0.5f;

	public const float UNCONSCIOUS_BREATHING_SCALE = 0.5f;

	public const float DEHYDRATION_DAMAGE = 0.1f;

	public const float NUTRITION_DAMAGE = 0.1f;

	private static float _stunUpdateSpeed = 1f;

	private ushort _daysLived;

	private float _nutrition;

	private float _respawnStressTime;

	public const float BASE_HYDRATION_STORAGE = 5f;

	public CapsuleCollider MasterCollider;

	public List<Collider> MovementColliders = new List<Collider>(2);

	[ByteArraySync]
	private float _hydration;

	protected const float MAX_OXYGEN_STORAGE = 0.024f;

	private float _oxygenation;

	public const float MAX_MOOD = 1f;

	public const float NORMAL_HYGIENE = 1f;

	public const float MAX_HYGIENE_FROM_SHOWER = 1.5f;

	public const float MAX_HYGIENE = 1.5f;

	private float _mood;

	private float _hygiene;

	public const float MAX_FOOD_QUALITY = 1f;

	private float _foodQuality = 0.75f;

	private Transform _previousCameraRig;

	private bool _isUnresponsive;

	private const float POLLUTED_RATIO_FOR_SOILED = 0.1f;

	public Transform CharacterRotationY => characterRotationY;

	public virtual Vector3 EntityForward
	{
		get
		{
			if (HasAuthority)
			{
				MovementController movementController = MovementController;
				if ((object)movementController == null || movementController.ControlMode != MovementController.Mode.Seated)
				{
					return characterRotationY.forward;
				}
			}
			return ThingTransform.forward;
		}
	}

	public virtual Quaternion EntityRotation
	{
		get
		{
			if (HasAuthority)
			{
				MovementController movementController = MovementController;
				if ((object)movementController == null || movementController.ControlMode != MovementController.Mode.Seated)
				{
					return characterRotationY.rotation;
				}
			}
			return base.ActiveRigidbody.rotation;
		}
	}

	public virtual float BreathingEfficiency
	{
		get
		{
			if (base.InternalAtmosphere == null)
			{
				return 0f;
			}
			return Mathf.Clamp((base.InternalAtmosphere.PartialPressureO2 / Chemistry.MinimumOxygenPartialPressure).ToFloat(), 0f, 1.5f);
		}
	}

	public Ladder TargetLadder { get; protected set; }

	protected bool GravityEnabled { get; private set; }

	public EntityState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state == value)
			{
				return;
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
			if (_state != value && _state != EntityState.Decay)
			{
				_oldState = _state;
				_state = value;
				if (!ThreadedManager.IsThread)
				{
					OnStateChanged();
				}
				else
				{
					WaitHandleState().Forget();
				}
				OnStateChangeEvent?.Invoke();
			}
		}
	}

	public bool IsRagdoll { get; set; }

	public override bool HasAuthority
	{
		get
		{
			if (!OrganBrain)
			{
				return base.HasAuthority;
			}
			return OrganBrain.HasAuthority;
		}
	}

	public virtual Atmosphere BreathingAtmosphere
	{
		get
		{
			return base.WorldAtmosphere;
		}
		protected set
		{
			base.WorldAtmosphere = value;
		}
	}

	public virtual Atmosphere LungAtmosphere => base.InternalAtmosphere;

	public override bool PreventStateChange => true;

	public bool Unconscious => State == EntityState.Unconscious;

	public bool Dead => State == EntityState.Dead;

	public bool HasRecentParent
	{
		get
		{
			if (!_recentParent)
			{
				return base.IsChild;
			}
			return true;
		}
	}

	public override bool RunPhysicsUpdate => true;

	public float DehydrationDamageRate
	{
		get
		{
			if (!IsSleeping)
			{
				return 0.1f;
			}
			return 0.033000004f;
		}
	}

	public float NutritionDamageRate
	{
		get
		{
			if (!IsSleeping)
			{
				return 0.1f;
			}
			return 0.033000004f;
		}
	}

	public ushort DaysLived
	{
		get
		{
			return _daysLived;
		}
		private set
		{
			_daysLived = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float Nutrition
	{
		get
		{
			return _nutrition;
		}
		set
		{
			_nutrition = Mathf.Clamp(value, 0f, BaseNutritionStorage);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float RespawnStressTime
	{
		get
		{
			return _respawnStressTime;
		}
		set
		{
			_respawnStressTime = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public bool ExperiencingRespawnStress => RespawnStressTime > 0f;

	public virtual float BaseNutritionStorage => 5f;

	public float Hydration
	{
		get
		{
			return _hydration;
		}
		set
		{
			_hydration = Mathf.Clamp(value, 0f, 8.75f);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float HydrationRatio => Hydration / 5f;

	public float NutritionRatio => Nutrition / BaseNutritionStorage;

	public float MoodRatio => Mood / 1f;

	public float HygieneRatio => Hygiene;

	public float FoodQualityRatio => FoodQuality / 1f;

	public float SanitationRatio { get; protected set; }

	public float Oxygenation
	{
		get
		{
			return _oxygenation;
		}
		set
		{
			_oxygenation = Mathf.Clamp(value, 0f, 0.024f);
		}
	}

	public float OxygenQuality
	{
		get
		{
			return _oxygenQuality;
		}
		protected set
		{
			_oxygenQuality = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float OxygenationRatio => Oxygenation / 100f;

	public float Mood
	{
		get
		{
			return _mood;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 1f);
			_mood = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float Hygiene
	{
		get
		{
			return _hygiene;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 1.5f);
			_hygiene = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public float FoodQuality
	{
		get
		{
			return _foodQuality;
		}
		set
		{
			value = Mathf.Clamp(value, 0f, 1f);
			_foodQuality = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public virtual bool IsArtificial => false;

	protected float _currentDecayTime { get; set; }

	protected override float DragYOffset => 0.4f;

	public bool IsLocalPlayer
	{
		get
		{
			if ((bool)OrganBrain)
			{
				return OrganBrain.LocalControl;
			}
			return false;
		}
	}

	public bool IsDead
	{
		get
		{
			EntityState state = State;
			return state == EntityState.Dead || state == EntityState.Decay;
		}
	}

	public bool IsUnresponsive
	{
		get
		{
			if (State != EntityState.Alive)
			{
				return !IsSleeping;
			}
			return false;
		}
	}

	public bool IsDraggable
	{
		get
		{
			if (State != EntityState.Alive)
			{
				return !base.IsBeingDragged;
			}
			return false;
		}
	}

	public bool IsSleeping
	{
		get
		{
			if (State == EntityState.Unconscious)
			{
				if (base.ParentSlot?.Parent is ILifeSuspender lifeSuspender)
				{
					return lifeSuspender.IsSuspendingLife;
				}
				return false;
			}
			return false;
		}
	}

	public bool IsSoiled
	{
		get
		{
			if (!IsArtificial)
			{
				return IsAtmosphereSoiled(SoilingAtmosphere);
			}
			return false;
		}
	}

	protected virtual Atmosphere SoilingAtmosphere => base.WorldAtmosphere;

	public event Event OnEntityUnconciousEvent;

	public event Event OnEntityConsciousEvent;

	public virtual bool CanEat()
	{
		return true;
	}

	public virtual bool CanDefecate()
	{
		return true;
	}

	public virtual bool CanDrink()
	{
		return true;
	}

	public ExteriorState GetExteriorState()
	{
		AtmosphereHelper.AtmosphereMode? atmosphereMode = base.WorldAtmosphere?.Mode;
		if (atmosphereMode.HasValue && atmosphereMode == AtmosphereHelper.AtmosphereMode.Thing)
		{
			return ExteriorState.Interior;
		}
		if (base.WorldAtmosphere?.Room != null)
		{
			return ExteriorState.Room;
		}
		return ExteriorState.World;
	}

	public float GetSurvivalPropertyRatio(EntitySurvivalProperty property)
	{
		return property switch
		{
			EntitySurvivalProperty.None => 0f, 
			EntitySurvivalProperty.OxygenQuality => OxygenQuality, 
			EntitySurvivalProperty.Nutrition => NutritionRatio, 
			EntitySurvivalProperty.Hydration => HydrationRatio, 
			EntitySurvivalProperty.Mood => MoodRatio, 
			EntitySurvivalProperty.Hygiene => HygieneRatio, 
			EntitySurvivalProperty.FoodQuality => FoodQualityRatio, 
			EntitySurvivalProperty.Health => Mathf.Clamp01(1f - DamageState.TotalRatio), 
			_ => throw new ArgumentOutOfRangeException("property", property, null), 
		};
	}

	public override Vector3 GetStormWindVector()
	{
		if (OrganBrain == null)
		{
			return base.GetStormWindVector();
		}
		if (!(this is Npc) && !OrganBrain.IsOnline)
		{
			return Vector3.zero;
		}
		return base.GetStormWindVector();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Thing.RenderChange(setRenderer: true, this).Forget();
	}

	protected virtual void OnStateChanged()
	{
	}

	private async UniTaskVoid WaitHandleState()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		OnStateChanged();
	}

	public override void OnDamageDestroyed()
	{
		if (GameManager.RunSimulation && State != EntityState.Dead)
		{
			State = EntityState.Dead;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		switch (State)
		{
		case EntityState.Dead:
			result.State = GameStrings.EntityDeceased.AsColor("red");
			break;
		case EntityState.Unconscious:
			result.State = GameStrings.EntityUnconsious.AsColor("yellow");
			break;
		}
		result.Extended = GetExtendedText().ToString();
		return result;
	}

	public override void InitializeDamageState()
	{
		DamageState = new EntityDamageState(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new EntitySaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is EntitySaveData entitySaveData)
		{
			_state = entitySaveData.State;
			Nutrition = entitySaveData.Nutrition;
			Hydration = entitySaveData.Hydration;
			Oxygenation = entitySaveData.Oxygenation;
			DaysLived = entitySaveData.DaysLived;
			Mood = entitySaveData.Mood;
			Hygiene = entitySaveData.Hygiene;
			FoodQuality = entitySaveData.FoodQuality;
			RespawnStressTime = entitySaveData.RespawnStressTime;
			FloatReference currentDecayTime = entitySaveData.CurrentDecayTime;
			_currentDecayTime = ((currentDecayTime != null) ? ((float)currentDecayTime) : 0f);
			WaitThenCheckDamage().Forget();
			if ((bool)MovementController)
			{
				MovementController.ControlMode = (MovementController.Mode)entitySaveData.MovementControllerControlMode;
			}
		}
	}

	public static Entity GetClientEntity(ulong clientId)
	{
		foreach (Entity allEntity in AllEntities)
		{
			if (allEntity.OwnerClientId == clientId)
			{
				return allEntity;
			}
		}
		return null;
	}

	private async UniTaskVoid WaitThenCheckDamage()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		DamageState.OnDamageUpdated();
		OnStateChanged();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (GameManager.GameState != GameState.None && savedData is EntitySaveData entitySaveData)
		{
			entitySaveData.State = State;
			entitySaveData.Nutrition = Nutrition;
			entitySaveData.Hydration = Hydration;
			entitySaveData.Mood = Mood;
			entitySaveData.Hygiene = Hygiene;
			entitySaveData.FoodQuality = FoodQuality;
			entitySaveData.Oxygenation = Oxygenation;
			entitySaveData.MovementControllerControlMode = (int)((MovementController != null) ? MovementController.ControlMode : MovementController.Mode.Animation);
			entitySaveData.DaysLived = DaysLived;
			entitySaveData.RespawnStressTime = RespawnStressTime;
			entitySaveData.CurrentDecayTime = new FloatReference(_currentDecayTime);
		}
	}

	public override bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		if (IsRagdoll)
		{
			SetRagdoll(active: false);
		}
		return base.MoveToSlot(destinationSlot, originThing, forced);
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		bool flag = base.ParentSlot != null;
		bool num = base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
		if (num && flag)
		{
			if (HasAuthority && InventoryManager.ParentHuman != null && InventoryManager.ParentHuman == this)
			{
				MovementController.ControlMode = MovementController.Mode.Animation;
				CameraController.Instance.RotationY = worldRotation.eulerAngles.y;
			}
			EntityState state = State;
			if (state == EntityState.Unconscious || state == EntityState.Dead)
			{
				SetRagdoll(active: true);
			}
		}
		return num;
	}

	public override void OutOfBounds()
	{
		if (IsOutOfBounds)
		{
			Vector3 vector = Vector3.zero - base.ThingTransformPosition;
			if (!base.ActiveRigidbody.isKinematic)
			{
				base.ActiveRigidbody.velocity = Vector3.zero;
				base.ActiveRigidbody.AddForce(vector * 10f);
			}
			else
			{
				Vector3 vector2 = Vector3.Lerp(base.ThingTransformPosition, base.ThingTransformPosition + vector, Time.fixedDeltaTime);
				base.ActiveRigidbody.MovePosition(vector2);
			}
			IsOutOfBounds = false;
		}
	}

	private async UniTaskVoid RecentExit()
	{
		_recentParent = true;
		await UniTask.Delay(500);
		_recentParent = false;
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		Rotation = EntityRotation;
		Forward = EntityForward;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		MovementController.ControlMode = base.ParentSlot.EntityControlMode;
		ThingTransform.rotation = base.ParentSlot.Location.rotation;
		if (parent is IExitable exitable)
		{
			HasEnteredExitable(exitable);
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		ThingTransform.rotation = Quaternion.identity;
		MovementController.ControlMode = MovementController.Mode.Animation;
		if (parent is IExitable exitable)
		{
			HasExitedExitable(exitable);
		}
		EntityState state = State;
		if (state == EntityState.Unconscious || state == EntityState.Dead)
		{
			ResetRagDollVelocities();
		}
		else
		{
			if (!HasAuthority)
			{
				return;
			}
			if (!GameManager.IsBatchMode)
			{
				if (!InventoryManager.ParentHuman.LockedToSeat)
				{
					RecentExit().Forget();
				}
			}
			else
			{
				RecentExit().Forget();
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (!(newChild is Brain brain))
		{
			if (newChild is Lungs lungs && lungs.ParentSlot == LungsSlot)
			{
				OrganLungs = lungs;
				Organs.Add(lungs);
			}
		}
		else if (brain.ParentSlot == BrainSlot)
		{
			OrganBrain = brain;
			Organs.Add(brain);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (!(previousChild is Brain brain))
		{
			if (previousChild is Lungs lungs && OrganLungs == lungs)
			{
				OrganLungs = null;
				Organs.Remove(lungs);
			}
		}
		else if (OrganBrain == brain)
		{
			OrganBrain = null;
			Organs.Remove(brain);
		}
	}

	private void SetRagdollTransformDefaults()
	{
		foreach (RagdollTransformDefaults item in _ragdollPartsToReset)
		{
			item.RestPosition = item.Transform.localPosition;
			item.RestRotation = item.Transform.localEulerAngles;
		}
	}

	public override void Awake()
	{
		base.Awake();
		AsHuman = this as Human;
		CameraController = CameraController.Instance;
		List<SkinnedMeshRendererInstance> list = new List<SkinnedMeshRendererInstance>();
		SkinnedMeshRenderer[] componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
		{
			if (!skinnedMeshRenderer.name.Contains("Shadow") && !skinnedMeshRenderer.name.Contains("_RENDERER"))
			{
				SkinnedMeshRendererInstance item = new SkinnedMeshRendererInstance
				{
					Parent = this,
					Renderer = skinnedMeshRenderer
				};
				list.Add(item);
			}
		}
		SkinnedMeshes = list.ToArray();
		BrainSlot = Slots.Find((Slot slot) => slot.StringHash == Slot.BrainHash);
		LungsSlot = Slots.Find((Slot slot) => slot.StringHash == Slot.LungsHash);
		AllEntities.Add(this);
		foreach (RagdollPart ragdollPart in RagdollParts)
		{
			if (ragdollPart.Collider != null)
			{
				RagdollPartType type = ragdollPart.Type;
				if ((type == RagdollPartType.LeftArm || type == RagdollPartType.RightArm || type == RagdollPartType.LeftHips || type == RagdollPartType.RightHips || type == RagdollPartType.Head) && (bool)ragdollPart.Joint)
				{
					ragdollPart.Joint.connectedBody = null;
				}
				ragdollPart.Rigidbody.isKinematic = true;
				ragdollPart.Collider.enabled = false;
			}
		}
	}

	public virtual void OnNewDay()
	{
		DaysLived++;
	}

	private void ResetRagdollPartTransforms()
	{
		foreach (RagdollTransformDefaults item in _ragdollPartsToReset)
		{
			item.Transform.localPosition = item.RestPosition;
			item.Transform.localEulerAngles = item.RestRotation;
		}
	}

	private void ResetRagDollVelocities()
	{
		base.ActiveRigidbody.angularVelocity = Vector3.zero;
		foreach (RagdollPart ragdollPart in RagdollParts)
		{
			if (ragdollPart.Rigidbody != null && !ragdollPart.Rigidbody.isKinematic)
			{
				ragdollPart.Rigidbody.angularVelocity = Vector3.zero;
				ragdollPart.Rigidbody.velocity = Vector3.zero;
			}
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		if (SkinnedMeshes == null)
		{
			return;
		}
		SkinnedMeshRendererInstance[] skinnedMeshes = SkinnedMeshes;
		foreach (SkinnedMeshRendererInstance skinnedMeshRendererInstance in skinnedMeshes)
		{
			if (!(skinnedMeshRendererInstance.Renderer == null))
			{
				skinnedMeshRendererInstance.Renderer.enabled = false;
			}
		}
	}

	public virtual void LifeEffects()
	{
	}

	public virtual bool OnLifeTick()
	{
		if (State == EntityState.Dead || GameManager.GameState != GameState.Running)
		{
			return false;
		}
		LifeEffects();
		foreach (Organ organ in Organs)
		{
			organ?.OnLifeTick();
		}
		if (LungAtmosphere != null)
		{
			LifeBreathe();
		}
		if (IsArtificial)
		{
			return true;
		}
		if (RootParent is ILifeSuspender { IsSuspendingLife: not false })
		{
			return true;
		}
		LifeNutrition();
		LifeDehydrate();
		AssessMood();
		AssessHygiene();
		UpdateRespawnStress();
		return true;
	}

	private void UpdateRespawnStress()
	{
		RespawnStressTime -= GameManager.GameTickSpeedSeconds;
	}

	public virtual void Hydrate(Mole water)
	{
		Hydration += water.Quantity.ToFloat() * HydrationBase.HydrationPerMole;
		Achievements.AssessSomeHighQualityH2O(this);
	}

	protected virtual void LifeDehydrate()
	{
		if (!IsArtificial)
		{
			if (Hydration <= 0f)
			{
				DamageState.Damage(ChangeDamageType.Increment, DehydrationDamageRate, DamageUpdateType.Hydration);
			}
			else if (DamageState.Hydration > 0f)
			{
				DamageState.Damage(ChangeDamageType.Decrement, DehydrationDamageRate, DamageUpdateType.Hydration);
			}
		}
	}

	protected virtual void AssessMood()
	{
		_ = IsArtificial;
	}

	protected virtual void AssessHygiene()
	{
	}

	public void OnDamaged(DamageUpdateType damageType, float value)
	{
		if (damageType <= DamageUpdateType.Hydration)
		{
			switch (damageType)
			{
			default:
				return;
			case DamageUpdateType.Burn:
			case DamageUpdateType.Brute:
			case DamageUpdateType.Oxygen:
			case DamageUpdateType.Hydration:
				break;
			}
		}
		else if (damageType <= DamageUpdateType.Starvation)
		{
			if (damageType != DamageUpdateType.Radiation && damageType != DamageUpdateType.Starvation)
			{
				return;
			}
		}
		else if (damageType != DamageUpdateType.Toxic)
		{
			_ = 256;
			return;
		}
		ReduceMoodDueToDamage(value);
	}

	private void ReduceMoodDueToDamage(float damage)
	{
		if (!IsArtificial)
		{
			Mood -= damage * 0.05f;
		}
	}

	protected virtual void LifeNutrition()
	{
		if (!IsArtificial)
		{
			if (Nutrition <= 0f)
			{
				DamageState.Damage(ChangeDamageType.Increment, NutritionDamageRate, DamageUpdateType.Starvation);
			}
			else if (DamageState.Starvation > 0f)
			{
				DamageState.Damage(ChangeDamageType.Decrement, NutritionDamageRate, DamageUpdateType.Starvation);
			}
		}
	}

	public void OnCameraUpdate(CameraController cameraController)
	{
		float num = DamageState.Stun;
		if (IsDead)
		{
			num = DamageState.MaxDamage;
		}
		float b = Mathf.Lerp(0f, 0.5f, num / 80f);
		float b2 = Mathf.Lerp(0f, 1f, num / 50f);
		float b3 = Mathf.Lerp(1f, 0f, num / 100f);
		float b4 = Mathf.Lerp(0.98f, 0f, num / 100f);
		cameraController.CameraVignette.intensity = Mathf.Lerp(cameraController.CameraVignette.intensity, b, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraVignette.blur = Mathf.Lerp(cameraController.CameraVignette.blur, b2, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraColorControl.Saturation = Mathf.Lerp(cameraController.CameraColorControl.Saturation, b3, Time.deltaTime * _stunUpdateSpeed);
		cameraController.CameraColorControl.Brightness = Mathf.Lerp(cameraController.CameraColorControl.Brightness, b4, Time.deltaTime * _stunUpdateSpeed);
	}

	protected void ResetCameraEffects(CameraController cameraController)
	{
		cameraController.CameraVignette.intensity = 0f;
		cameraController.CameraVignette.blur = 0f;
		cameraController.CameraColorControl.Saturation = 1f;
		cameraController.CameraColorControl.Brightness = 0.95f;
	}

	public float GetHydrationStorage()
	{
		return 5f * GetFoodQualityMultiplier();
	}

	public float GetNutritionStorage()
	{
		return BaseNutritionStorage;
	}

	public float GetFoodQualityMultiplier()
	{
		float foodQuality = FoodQuality;
		if (foodQuality < 0.7f)
		{
			if (foodQuality < 0.45f)
			{
				return 0.75f;
			}
			return 1f;
		}
		if (foodQuality < 0.9f)
		{
			return 1.25f;
		}
		return 1.75f;
	}

	protected virtual void TakeBreath()
	{
		float num = TakeBreath(LungAtmosphere, LungAtmosphere.GasMixture.Oxygen, BreathingAtmosphere.GasMixture.CarbonDioxide, 0.5f);
		Oxygenation += num;
		OxygenQuality = num / 0.0048f;
	}

	protected float TakeBreath(Atmosphere lungAtmosphere, Mole source, Mole destination, float returnScale)
	{
		double num = Math.Min(0.0048f * BreathingEfficiency, source.Quantity.ToDouble() * (double)BreathingEfficiency);
		destination.AddAtTemperature(BodyTemperature, source.Remove(new MoleQuantity(num)).Quantity * returnScale);
		lungAtmosphere.GasMixture.AddEnergy(EnergyReleasedPerTick);
		return (float)num;
	}

	protected void LifeBreathe()
	{
		if (IsArtificial)
		{
			Oxygenation = 100f;
			return;
		}
		if (BreathingAtmosphere == null)
		{
			Atmosphere atmosphere = (BreathingAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L));
		}
		TakeBreath();
		if (N2OExceedsSafeLimit(out var stunDamage))
		{
			float num = ((this is Npc || OrganBrain.IsOnline) ? 2f : (2f * Mathf.Clamp(DifficultySetting.Current.OfflineMetabolism, 0.1f, 1f)));
			DamageState.Damage(ChangeDamageType.Increment, stunDamage * num, DamageUpdateType.Stun);
		}
		if (LungAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.001))
		{
			LungAtmosphere.GasMixture.Reset();
		}
		if (BreathingAtmosphere != null)
		{
			if (BreathingAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && BreathingAtmosphere.LiquidVolumeRatio > 0.5f)
			{
				AtmosphereHelper.Mix(LungAtmosphere, BreathingAtmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.Mix(LungAtmosphere, BreathingAtmosphere, AtmosphereHelper.MatterState.Gas);
			}
		}
		else
		{
			OxygenQuality = 0f;
		}
	}

	protected virtual bool N2OExceedsSafeLimit(out float stunDamage)
	{
		stunDamage = 0f;
		return false;
	}

	public virtual void OnLifeCreated()
	{
		Nutrition = BaseNutritionStorage;
		Hydration = 5f;
		RespawnStressTime = 0f;
		Mood = 1f;
		Hygiene = 1f;
		FoodQuality = 0.75f;
		OrganBrain = OnServer.Create<Brain>(Prefab.Organ.Brain, BrainSlot);
		OnServer.SetCustomName(OrganBrain, DisplayName + "'s Brain");
	}

	public void UpdateOwnPhysicsUpdate()
	{
		LastPhysicsUpdate.WorldPosition = base.ThingTransformPosition;
		LastPhysicsUpdate.WorldRotation = ThingTransform.rotation;
		LastPhysicsUpdate.AngularVelocity = RigidBody.angularVelocity;
		LastPhysicsUpdate.Velocity = RigidBody.velocity;
	}

	protected override void PrepareUpdateMessage(ref DynamicThingPosition currentPosition)
	{
		base.PrepareUpdateMessage(ref currentPosition);
		LastPhysicsUpdate = currentPosition;
	}

	public virtual void TakeControl(bool setPhysics)
	{
		if (RootParent is LanderCapsule)
		{
			RigidBody.interpolation = RigidbodyInterpolation.None;
		}
		else
		{
			RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
		}
	}

	public void HasAcquiredControlInExitable(IExitable exitable)
	{
		_previousCameraRig = InventoryManager.Parent.CameraRig;
		InventoryManager.Parent.CameraRig = exitable.GetCameraPoint(this);
		RigidBody.interpolation = RigidbodyInterpolation.None;
		if (exitable.FreeLook)
		{
			CameraController.Instance.ControlMode = CameraControlMode.MouseLook;
			AllowChildInteraction = true;
		}
	}

	private void HasEnteredExitable(IExitable exitable)
	{
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: false, hideOnPlayer: true, isRecursive: true);
			}
		}
		if (!(OrganBrain == null) && HasAuthority && (bool)InventoryManager.Parent)
		{
			_previousCameraRig = InventoryManager.Parent.CameraRig;
			InventoryManager.Parent.CameraRig = exitable.GetCameraPoint(this);
			RigidBody.interpolation = RigidbodyInterpolation.None;
			if (exitable.FreeLook)
			{
				CameraController.Instance.ControlMode = CameraControlMode.MouseLook;
				AllowChildInteraction = true;
			}
		}
	}

	private void HasExitedExitable(IExitable exitable)
	{
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: true, hideOnPlayer: false, isRecursive: true);
			}
		}
		if (!(OrganBrain == null) && HasAuthority && (bool)InventoryManager.Parent)
		{
			InventoryManager.Parent.CameraRig = _previousCameraRig;
			CameraController.Instance.ControlMode = CameraControlMode.Default;
			RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
			AllowChildInteraction = false;
		}
	}

	public virtual void OnEntityDecay()
	{
		AllEntities.Remove(this);
		if (GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			AllEntities.Remove(this);
		}
	}

	protected virtual void EntityDeath()
	{
		if (GameManager.RunSimulation)
		{
			WaitForDecay().Forget();
		}
	}

	protected virtual float GetDecayTime()
	{
		return 60f;
	}

	private async UniTaskVoid WaitForDecay()
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		if (Mathf.Approximately(_currentDecayTime, 0f))
		{
			_currentDecayTime = GetDecayTime();
		}
		while (_currentDecayTime > 0f && State == EntityState.Dead)
		{
			if (GameManager.GameState == GameState.Paused)
			{
				await UniTask.NextFrame(cancelToken);
				continue;
			}
			Thing thing = base.ParentSlot?.Parent;
			if (thing is Entity || thing is CryoTube)
			{
				_currentDecayTime = Mathf.Max(60f, _currentDecayTime);
			}
			else
			{
				_currentDecayTime -= GameManager.DeltaTime;
			}
			await UniTask.NextFrame(cancelToken);
		}
		if (GameManager.GameState == GameState.Running && !cancelToken.IsCancellationRequested && State == EntityState.Dead)
		{
			State = EntityState.Decay;
		}
	}

	protected virtual void OnEntityUnconscious()
	{
		this.OnEntityUnconciousEvent?.Invoke();
		if (base.ParentSlot == null)
		{
			SetRagdoll(active: true);
		}
	}

	protected virtual void OnEntityConscious()
	{
		this.OnEntityConsciousEvent?.Invoke();
		if (base.ParentSlot != null)
		{
			Sleeper obj = base.ParentSlot.Parent as Sleeper;
			SpawnPointAtmospherics spawnPointAtmospherics = base.ParentSlot.Parent as SpawnPointAtmospherics;
			if (!obj && !spawnPointAtmospherics)
			{
				Vector3 vector = ((base.ParentSlot.Parent is IExitable exitable) ? exitable.GetExitPosition(this) : base.ParentSlot.Location.position);
				OnServer.MoveToWorld(base.ParentSlot.Occupant, vector, base.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
			}
		}
		if ((bool)base.Joint)
		{
			MoveToWorld();
		}
		SetRagdoll(active: false);
	}

	public override void OnGravityEnabled()
	{
		GravityEnabled = (object)TargetLadder == null;
		RigidBody.useGravity = GravityEnabled;
		SetRagdollGravity(GravityEnabled);
	}

	public override void OnGravityDisabled()
	{
		base.OnGravityDisabled();
		SetRagdollGravity(isGravity: false);
		GravityEnabled = false;
	}

	private void SetRagdollGravity(bool isGravity)
	{
		foreach (RagdollPart ragdollPart in RagdollParts)
		{
			if ((bool)ragdollPart.Collider)
			{
				ragdollPart.Rigidbody.useGravity = isGravity;
			}
		}
	}

	public override bool DragInSlot(Slot destinationSlot, Vector3 offset)
	{
		if (!(destinationSlot.Parent is DynamicThing) || (bool)base.Joint)
		{
			return false;
		}
		SetRagdoll(active: true, carried: true);
		return base.DragInSlot(destinationSlot, offset);
	}

	public override ShadowCastingMode GetShadowCastingMode()
	{
		return ShadowCastingMode.On;
	}

	private static bool IsAtmosphereSoiled(Atmosphere atmosphere)
	{
		if (atmosphere == null)
		{
			return false;
		}
		MoleQuantity getTotalMolesGassesAndLiquids = atmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		if (getTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
		{
			return false;
		}
		return (atmosphere.GasMixture.PollutedWater.Quantity / getTotalMolesGassesAndLiquids).ToFloat() > 0.1f;
	}

	public void SetRagdoll(bool active, bool carried = false)
	{
		if (active == IsRagdoll)
		{
			foreach (RagdollPart ragdollPart in RagdollParts)
			{
				if ((bool)ragdollPart.GameObject)
				{
					ragdollPart.GameObject.layer = (carried ? Layers.IgnoreRaycast : Layers.PlayerRagdoll);
				}
			}
			return;
		}
		IsRagdoll = active;
		BaseAnimator.enabled = !active;
		if (active)
		{
			ResetRagdollPartTransforms();
			SetNoInterpolationForClients();
		}
		foreach (Collider movementCollider in MovementColliders)
		{
			movementCollider.enabled = !active;
		}
		Vector3 velocity = RigidBody.velocity;
		Vector3 direction = ((UnityEngine.Random.value > 0.5f) ? ThingTransform.forward : (-ThingTransform.forward));
		foreach (RagdollPart ragdollPart2 in RagdollParts)
		{
			if (!ragdollPart2.Collider || ragdollPart2.Type == RagdollPartType.Pelvis)
			{
				continue;
			}
			RagdollPartType type = ragdollPart2.Type;
			if ((type == RagdollPartType.LeftArm || type == RagdollPartType.RightArm || type == RagdollPartType.LeftHips || type == RagdollPartType.RightHips || type == RagdollPartType.Head) && (bool)ragdollPart2.Joint)
			{
				ragdollPart2.Joint.connectedBody = (active ? RigidBody : null);
			}
			ragdollPart2.Collider.enabled = active;
			ragdollPart2.Rigidbody.useGravity = base.Room != null || WorldManager.HasGravityAtHeight(Transform.position.y);
			ragdollPart2.Rigidbody.isKinematic = !active;
			ragdollPart2.Rigidbody.maxDepenetrationVelocity = 10f;
			if (active)
			{
				if (!ragdollPart2.Rigidbody.isKinematic)
				{
					ragdollPart2.Rigidbody.velocity = velocity;
				}
				ragdollPart2.SetCarried(carried, direction);
			}
		}
		if (active)
		{
			_rotationBeforeRagdoll = RigidBody.rotation;
		}
		else
		{
			base.ActiveRigidbody.MoveRotation(_rotationBeforeRagdoll);
		}
		if (!active && !base.ActiveRigidbody.isKinematic)
		{
			base.ActiveRigidbody.velocity = velocity;
		}
		base.ActiveRigidbody.constraints = ((!active) ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None);
	}

	public virtual void RefreshClothing()
	{
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteByte((byte)State);
			writer.WriteSingle(OxygenQuality);
			writer.WriteSingle(Nutrition);
			writer.WriteSingle(RespawnStressTime);
			writer.WriteSingle(Hydration);
			writer.WriteSingle(Mood);
			writer.WriteSingle(Hygiene);
			writer.WriteSingle(FoodQuality);
			writer.WriteUInt16(DaysLived);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			State = (EntityState)reader.ReadByte();
			OxygenQuality = reader.ReadSingle();
			Nutrition = reader.ReadSingle();
			RespawnStressTime = reader.ReadSingle();
			Hydration = reader.ReadSingle();
			Mood = reader.ReadSingle();
			Hygiene = reader.ReadSingle();
			FoodQuality = reader.ReadSingle();
			DaysLived = reader.ReadUInt16();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)State);
		writer.WriteSingle(OxygenQuality);
		writer.WriteSingle(Nutrition);
		writer.WriteSingle(RespawnStressTime);
		writer.WriteSingle(Hydration);
		writer.WriteSingle(Mood);
		writer.WriteSingle(Hygiene);
		writer.WriteSingle(FoodQuality);
		writer.WriteUInt16(DaysLived);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		State = (EntityState)reader.ReadByte();
		OxygenQuality = reader.ReadSingle();
		Nutrition = reader.ReadSingle();
		RespawnStressTime = reader.ReadSingle();
		Hydration = reader.ReadSingle();
		Mood = reader.ReadSingle();
		Hygiene = reader.ReadSingle();
		FoodQuality = reader.ReadSingle();
		DaysLived = reader.ReadUInt16();
	}

	public bool IsDamaged()
	{
		if (DamageState.TotalRatio > 0f)
		{
			return true;
		}
		foreach (Organ organ in Organs)
		{
			if (organ.DamageState.TotalRatio > 0f)
			{
				return true;
			}
		}
		return false;
	}

	public void ReleaseControl()
	{
		if (!GameManager.RunSimulation)
		{
			base.ActiveRigidbody.interpolation = RigidbodyInterpolation.None;
		}
	}

	public string PrintDebug()
	{
		ConsoleWindow.Print(DisplayName);
		ConsoleWindow.Print("Nutrition: " + StringManager.Get(Nutrition / BaseNutritionStorage * 100f) + "%");
		ConsoleWindow.Print("Hydration: " + StringManager.Get(Hydration / 5f * 100f) + "%");
		ConsoleWindow.Print("Oxygen Quality: " + StringManager.Get(OxygenQuality * 100f) + "%");
		ConsoleWindow.Print("Mood: " + StringManager.Get(Mood) + "%");
		ConsoleWindow.Print("Hygiene: " + StringManager.Get(Hygiene) + "%");
		ConsoleWindow.Print("Food Quality: " + StringManager.Get(FoodQuality) + "%");
		ConsoleWindow.Print("Respawn Stress: " + StringManager.Get(RespawnStressTime) + "%");
		return null;
	}

	public void DrawDebug()
	{
		ImGui.Begin("debug", (ImGuiWindowFlags)799685);
		ImGui.SetWindowPos(new Vector2(10f, 10f), ImGuiCond.Always);
		ImguiHelper.DrawText($"RelativeVelocity: {RelativeVelocity:F1}");
		ImguiHelper.DrawText($"Orientation: {Orientation:F1}");
		ImGui.End();
	}

	public virtual bool IsHealing()
	{
		if (base.ParentSlot?.Parent is ICryogenicRegenerator cryogenicRegenerator)
		{
			return cryogenicRegenerator.IsCryogenicActive;
		}
		return false;
	}

	public virtual bool IsStimmed()
	{
		return false;
	}

	public virtual bool IsStunned()
	{
		return false;
	}

	public virtual float GetTime<T>() where T : IMedicalEffect
	{
		return 0f;
	}
}
