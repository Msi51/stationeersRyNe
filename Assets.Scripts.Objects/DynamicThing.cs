using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Events;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Objects.RoboticArm;
using Objects.Rockets;
using Reagents;
using TerrainSystem;
using TerrainSystem.Lods;
using Trading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Weather;

namespace Assets.Scripts.Objects;

[RequireComponent(typeof(Rigidbody))]
public class DynamicThing : Thing, IComparable<DynamicThing>, IPhysical, IProfile, IDensePoolable, IWeatherDamagable, ILodRequester, IReferencable, IEvaluable, IThreadable
{
	public delegate void OnParentChangedHash(Thing thing);

	public delegate void OnParentChangedHashAndQuantityEvent(Thing thing, float quantity);

	public delegate void OnCollisionEnterEvent(Thing thing);

	public static DynamicThing LastManufacturedItem;

	private DensePoolReference<IPhysical> _physicalDensePool = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	private DensePoolReference<DynamicThing> _dynamicThingsDensePool = new DensePoolReference<DynamicThing>(OcclusionManager.AllDynamicThings);

	public static List<DynamicThing> DynamicThingPrefabs = new List<DynamicThing>();

	[Header("Dynamic Thing")]
	public SortingClass SortingClass;

	public MachineTier RecipeTier = MachineTier.TierOne;

	[SerializeField]
	private CollisionClass CollisionSound;

	public SlotWearAction SlotWearAction;

	public Slot.Class SlotType;

	public bool DeleteOnDestroyed = true;

	[FormerlySerializedAs("UseSecondarySound")]
	public string UsingSound;

	[FormerlySerializedAs("UseSecondaryCompleteSound")]
	public string UseCompleteSound;

	[SerializeField]
	protected bool deleteSlotItemsOnDeconstruct;

	public bool HasLight;

	private bool _movingFast;

	private float _boundsMinSize;

	[ReadOnly]
	public Rigidbody RigidBody;

	public Vector3 WorldCenterOfMass;

	private Room _currentRoom;

	[Tooltip("Enum that defines the way the character will hold the item")]
	public HandPosition GripType;

	[Tooltip("Defines type of animation played when Casting with Item")]
	public CastingAnimation CastAnimation;

	private static int[] CollisionSoundMap;

	public Event OnEnterRoom;

	public Event OnLeaveRoom;

	private Atmosphere _worldAtmosphere;

	[Range(0f, 1f)]
	[Tooltip("A lower number reduces the velocity effect an atmosphere will have on the object.")]
	public float AtmosphereDampeningScale = 1f;

	[Tooltip("Where the attackWith event should run for this item.")]
	public AttackWithEvent AttackWithEvent = AttackWithEvent.Server;

	[Tooltip("What the Center of Mass should be offset with.")]
	public Vector3 CenterOfMassOffset;

	[ReadOnly]
	[Tooltip("List of machine triggers the object is currently in. Used for callbacks.")]
	public HashSet<MachineInputTrigger> CurrentMachineInputTriggers = new HashSet<MachineInputTrigger>();

	[ReadOnly]
	public bool AtmosControl = true;

	[ReadOnly]
	public bool OnConveyor;

	[ReadOnly]
	public bool IsWatched;

	[Tooltip("Trade value per instance.")]
	public float TradeValue = -1f;

	[ReadOnly]
	public float PlayerSellScale = 1f;

	private List<Collider> _colliders = new List<Collider>();

	private List<Collider> _triggers = new List<Collider>();

	public static List<DynamicThing> DynamicObjects = new List<DynamicThing>();

	private long _parentReferenceId;

	public static HashSet<DynamicThing> AwaitingMoveWhenReady = new HashSet<DynamicThing>();

	public ToolUse ExitTool;

	public float ImpactForceThreshhold = 5f;

	public const float ImpactSoundMaxForce = 300f;

	public float SizeMultiplier = 1f;

	public float ImpactAudioCoolDown;

	public const float CoolDownBufferMin = 0.15f;

	public const float CoolDownBufferMax = 0.2f;

	public float YHeight;

	public static readonly float DraggedMassScale = 0.1f;

	private float _startMass;

	private Vector3 _dragOffset;

	protected bool _movePosition;

	private GameObject _pivotObject;

	private HingeJoint _pivotJoint;

	private float _rotationOnStartDrag;

	private const float DRAG_PIVOT_SPRING = 2000f;

	private const float DRAG_PIVOT_DAMPER = 200f;

	public DynamicThingPosition LastPhysicsUpdate;

	public bool IsOutOfBounds;

	public Event OnVelocityManitudeChange;

	private Vector3 _velocity;

	private float _velocityMagnitude;

	private float _atmosVelocityMagnitude;

	private Vector3 _velocityVector;

	private Vector3 _directionToCenter;

	private int _lastFrameAnimationUpdate = -1;

	private bool _snapNextPosition;

	private float _pressureVelocityScale;

	private Vector3 _atmosphericVelocity;

	public Vector3 RelativeVelocity;

	public float Orientation;

	private Vector3 _lastLocalPostion = Vector3.zero;

	private Vector3 _currentLocalPostion = Vector3.zero;

	public float ForceKinematicDistance = 0.01f;

	public bool IsForceKinematic;

	private Coroutine _smoothKinematic;

	protected float _stmoothKinematicRate;

	public bool AddExtraGravity = true;

	private float _angularDrag;

	private Vector3 _parentVelocity;

	private bool _staticParent;

	private bool _slotStateDirty = true;

	public bool _isBuoyant = true;

	private const float BUOYANCY_COEFFICIENT = 2.5f;

	private const float BUOYANCY_DAMPING = 0.85f;

	private const float BUOYANCY_SURFACE_OFFSET = 0.075f;

	private const float BUOYANCY_DISPLACEMENT_SCALE = -10f;

	private const float BUOYANCY_FORCE_CLAMP_VALUE = 15f;

	private const float BUOYANCY_UP_MULTIPLIER = 5f;

	private const float BUOYANCY_ANGULAR_MOMENTUM_DAMPING = 0.01f;

	private const float BUOYANCY_VELOCITY_DAMPING = 0.5f;

	private const float BUOYANCY_FLOW_FORCE_MULTIPLIER = 0.3f;

	private Vector3 _liquidFlowForce;

	public Vector3 ChildSlotOffset = Vector3.zero;

	public Vector3 ChildSlotOffsetPosition = Vector3.zero;

	private CollisionDetectionMode _origionalCollisionMode;

	private float _continuousCollisionTime = 1f;

	private DynamicThingPosition _lastSentValues;

	private DynamicThingPosition _currentPhysicsValues;

	private DynamicThingPosition _lastBatchValues;

	private bool _physicsBatchEligible;

	public uint LastPhysicsTick;

	private DynamicThingPosition _currentOwnerUpdate;

	private DynamicThingPosition _lastOwnerUpdate;

	protected const float DYNAMIC_THING_HEALTH_DAMAGE = 0.03f;

	public bool IsUnderLava;

	public bool IsInLavaObject;

	public bool CanTogglePower
	{
		get
		{
			if (HasOnOffState)
			{
				return base.InteractOnOff.CanKeyInteract;
			}
			return false;
		}
	}

	public override bool HasAuthority
	{
		get
		{
			if (base.HasAuthority)
			{
				return true;
			}
			if ((object)RootParentHuman != null && (object)InventoryManager.ParentBrain != null && (object)RootParentHuman.OrganBrain != null)
			{
				return RootParentHuman.OrganBrain.ReferenceId == InventoryManager.ParentBrain.ReferenceId;
			}
			return false;
		}
	}

	public bool MovingFast
	{
		get
		{
			return _movingFast;
		}
		set
		{
			if (_movingFast != value)
			{
				_movingFast = value;
				ActiveRigidbody.collisionDetectionMode = (value ? ((!ActiveRigidbody.isKinematic) ? CollisionDetectionMode.Continuous : CollisionDetectionMode.ContinuousSpeculative) : CollisionDetectionMode.Discrete);
			}
		}
	}

	public Slot ParentSlot { get; set; }

	public virtual int EquipSoundHash => -1;

	public virtual int UnEquipSoundHash => -1;

	public override float ConvectionFactor
	{
		get
		{
			if (IsChild)
			{
				if (IsHiddenInParentSlot())
				{
					return 0f;
				}
				return ParentSlot.Parent.ConvectionFactor * ThermodynamicsScale;
			}
			return ThermodynamicsScale;
		}
	}

	public override float RadiationFactor
	{
		get
		{
			if (IsChild)
			{
				if (IsHiddenInParentSlot())
				{
					return 0f;
				}
				return ParentSlot.Parent.RadiationFactor * ThermodynamicsScale;
			}
			return ThermodynamicsScale;
		}
	}

	public override float SolarHeatingFactor
	{
		get
		{
			if (IsChild)
			{
				if (IsHiddenInParentSlot())
				{
					return 0f;
				}
				return ParentSlot.Parent.SolarHeatingFactor * SolarHeatingScale;
			}
			return SolarHeatingScale;
		}
	}

	public Room Room
	{
		get
		{
			return _currentRoom;
		}
		set
		{
			if (_currentRoom == value)
			{
				return;
			}
			if (_currentRoom != null)
			{
				if (OnLeaveRoom != null)
				{
					OnLeaveRoomThreadSafe().Forget();
				}
				_currentRoom.Deregister(this);
			}
			_currentRoom = value;
			if (_currentRoom != null)
			{
				if (OnEnterRoom != null)
				{
					OnEnterRoomThreadSafe().Forget();
				}
				_currentRoom.Register(this);
			}
		}
	}

	public Atmosphere WorldAtmosphere
	{
		get
		{
			if (_worldAtmosphere == null)
			{
				return null;
			}
			if (_worldAtmosphere.IsInvalidWorld())
			{
				_worldAtmosphere = null;
			}
			return _worldAtmosphere;
		}
		set
		{
			_worldAtmosphere = value;
		}
	}

	public override bool HasAtmosphere
	{
		get
		{
			if (WorldAtmosphere != null)
			{
				return WorldAtmosphere.IsActive();
			}
			return false;
		}
	}

	public override Thing RootParent
	{
		get
		{
			if (!IsChild)
			{
				return base.RootParent;
			}
			Thing result = ParentSlot?.Parent;
			DynamicThing dynamicThing = ParentSlot?.Parent as DynamicThing;
			while ((bool)dynamicThing)
			{
				if (dynamicThing.ParentSlot == null || !ParentSlot.Parent)
				{
					return result;
				}
				result = dynamicThing.ParentSlot.Parent;
				dynamicThing = dynamicThing.ParentSlot.Parent as DynamicThing;
			}
			return result;
		}
	}

	public override Human RootParentHuman
	{
		get
		{
			if (!IsChild)
			{
				return base.RootParentHuman;
			}
			Thing parent = ParentSlot.Parent;
			DynamicThing dynamicThing = ParentSlot.Parent as DynamicThing;
			while (dynamicThing != null && !(parent is Human))
			{
				if (dynamicThing.ParentSlot == null || ParentSlot.Parent == null)
				{
					return null;
				}
				parent = dynamicThing.ParentSlot.Parent;
				dynamicThing = dynamicThing.ParentSlot.Parent as DynamicThing;
			}
			return parent as Human;
		}
	}

	public bool IsChild => ParentSlot?.Parent;

	public bool IsHiddenInSlot
	{
		get
		{
			Thing thing = ParentSlot?.Parent;
			if ((object)thing == null)
			{
				return false;
			}
			if (ParentSlot.OccupantAlwaysVisible)
			{
				return false;
			}
			if (ParentSlot.HidesOccupant)
			{
				return true;
			}
			if (thing is DynamicThing dynamicThing)
			{
				return dynamicThing.IsHiddenInSlot;
			}
			return false;
		}
	}

	public override Vector3 CenterPosition => WorldCenterOfMass;

	public long ParentReferenceId
	{
		get
		{
			return _parentReferenceId;
		}
		set
		{
			_parentReferenceId = value;
		}
	}

	public CharacterJoint Joint { get; protected set; }

	private Vector3 DragOffset
	{
		get
		{
			return _dragOffset;
		}
		set
		{
			if (!(value == _dragOffset))
			{
				_dragOffset = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	protected virtual float DragYOffset => 0.7f;

	public bool IsBeingDragged
	{
		get
		{
			if ((object)Joint != null)
			{
				return Joint;
			}
			return false;
		}
	}

	public Rigidbody ActiveRigidbody => RigidBody;

	protected virtual float LerpRate => 3f;

	protected virtual float RotationLerpRate => 1f;

	public Vector3 PreviousLodRequestPosition { get; set; }

	public long LodRequesterId => base.ReferenceId;

	public HashSet<Vector3Int>[] RequestedLods { get; set; } = LodHelper.InitArray(6);

	public virtual bool ShouldRender => false;

	public virtual LodInfo LodInfo => LodManager.ThingLodInfo;

	public float VelocityMagnitude
	{
		get
		{
			return _velocityMagnitude;
		}
		set
		{
			_velocityMagnitude = value;
			if (OnVelocityManitudeChange != null)
			{
				OnVelocityManitudeChange();
			}
		}
	}

	public Vector3 Velocity => _velocity;

	public static float StormWindStrength
	{
		get
		{
			if (!WeatherManager.IsWeatherEventRunning)
			{
				return 0f;
			}
			FloatReference floatReference = WeatherManager.CurrentWeatherEvent?.WindStrength;
			if (floatReference == null)
			{
				return 0f;
			}
			return floatReference;
		}
	}

	public virtual float MaxMovementSpeed => 20f;

	public virtual bool RunPhysicsUpdate
	{
		get
		{
			if (_staticParent)
			{
				return _slotStateDirty;
			}
			return true;
		}
	}

	public override bool IsBurnable
	{
		get
		{
			if (base.AutoignitionTemperature > TemperatureKelvin.Zero || base.FlashPointTemperature > TemperatureKelvin.Zero)
			{
				return !base.Indestructable;
			}
			return false;
		}
	}

	public bool IsKinematic { get; set; }

	public ICollector RegisteredCollector { get; set; }

	public bool IsInLava
	{
		get
		{
			if (!IsUnderLava)
			{
				return IsInLavaObject;
			}
			return true;
		}
	}

	public virtual float LavaDamage => OcclusionManager.LavaDamage;

	public int ThreadCost => 1;

	public static event Event OnManufactured;

	public event Event OnParentChanged;

	public static event OnParentChangedHash ParentChangedHashEvent;

	public static event OnParentChangedHashAndQuantityEvent ParentChangedHashAndQuantityEvent;

	public event OnCollisionEnterEvent OnCollision;

	public virtual bool IsLogicSlotReadable()
	{
		return HasAnySlots;
	}

	public virtual bool IsLogicReadable()
	{
		for (int i = 0; i < EnumCollections.LogicTypes.Values.Length; i++)
		{
			LogicType logicType = EnumCollections.LogicTypes.Values[i];
			if (CanLogicRead(logicType))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool IsLogicWritable()
	{
		for (int i = 0; i < EnumCollections.LogicTypes.Values.Length; i++)
		{
			LogicType logicType = EnumCollections.LogicTypes.Values[i];
			if (CanLogicWrite(logicType))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.ReferenceId:
			return true;
		case LogicType.PrefabHash:
			return true;
		case LogicType.Color:
			return HasColorState;
		case LogicType.Activate:
			return HasActivateState;
		case LogicType.Power:
			return HasPowerState;
		case LogicType.Open:
			return HasOpenState;
		case LogicType.Mode:
			return HasModeState;
		case LogicType.Error:
			return HasErrorState;
		case LogicType.Lock:
			return HasLockState;
		case LogicType.On:
			return HasOnOffState;
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.TotalMoles:
		case LogicType.RatioNitrousOxide:
		case LogicType.Combustion:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return HasReadableAtmosphere;
		case LogicType.Reagents:
			return HasReadableReagentMixture;
		default:
			return false;
		}
	}

	public virtual bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Color => HasColorState, 
			LogicType.Activate => HasActivateState, 
			LogicType.Open => HasOpenState, 
			LogicType.Mode => HasModeState, 
			LogicType.Lock => HasLockState, 
			LogicType.On => HasOnOffState, 
			_ => false, 
		};
	}

	public virtual void SetLogicValue(LogicType logicType, double value)
	{
		int state = (int)Mathf.Clamp((float)value, 0f, 1f);
		switch (logicType)
		{
		case LogicType.Color:
		{
			int num = (int)value.Clamp(0.0, GameManager.ColorCount - 1);
			if (GameManager.IsLogicSelectableColor(num))
			{
				OnServer.Interact(base.InteractColor, num);
			}
			break;
		}
		case LogicType.Activate:
			OnServer.Interact(base.InteractActivate, state);
			break;
		case LogicType.Open:
			OnServer.Interact(base.InteractOpen, state);
			break;
		case LogicType.Mode:
		{
			int state2 = (int)value.Clamp(0.0, ModeStrings.Length);
			OnServer.Interact(base.InteractMode, state2);
			break;
		}
		case LogicType.Lock:
			OnServer.Interact(base.InteractLock, state);
			break;
		case LogicType.On:
			OnServer.Interact(base.InteractOnOff, state);
			break;
		}
	}

	public override StringBuilder GetExtendedText()
	{
		if (ParentSlot == null || !(this is IUnfastenable))
		{
			return base.GetExtendedText();
		}
		StringBuilder extendedText = base.GetExtendedText();
		Thing parent = ParentSlot.Parent;
		if (parent is IFastenedConnector || parent is Lander)
		{
			extendedText.AppendLine(GameStrings.ThingIsFastenedIn.AsString(ParentSlot.ToTooltip(), ParentSlot.Parent.ToTooltip()));
			extendedText.AppendLine(GameStrings.CanBeUnfastenedWithWrench.AsString());
		}
		return extendedText;
	}

	public virtual double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PrefabHash:
			return PrefabHash;
		case LogicType.ReferenceId:
			return base.ReferenceId;
		case LogicType.Color:
			return ColorState;
		case LogicType.Activate:
			return Activate;
		case LogicType.On:
			return OnOff ? 1 : 0;
		case LogicType.Power:
			return Powered ? 1 : 0;
		case LogicType.Open:
			return IsOpen ? 1 : 0;
		case LogicType.Mode:
			return Mode;
		case LogicType.Error:
			return Error;
		case LogicType.Lock:
			return IsLocked ? 1 : 0;
		case LogicType.Combustion:
		{
			Atmosphere internalAtmosphere = base.InternalAtmosphere;
			if (internalAtmosphere == null || !internalAtmosphere.Inflamed)
			{
				return 0.0;
			}
			return 1.0;
		}
		case LogicType.Pressure:
			return base.InternalAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.Temperature:
			return base.InternalAtmosphere?.Temperature.ToDouble() ?? 0.0;
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return GasRatio(logicType);
		case LogicType.TotalMoles:
			return base.InternalAtmosphere?.TotalMoles.ToDouble() ?? 0.0;
		case LogicType.Reagents:
			return (ReagentMixture != null) ? ((float)ReagentMixture.TotalReagents) : 0f;
		default:
			return 0.0;
		}
	}

	public virtual bool SetGene(Gene gene, float value)
	{
		return false;
	}

	public virtual void SetQuantity(float value)
	{
	}

	public void Set(GasMixture newGasMix, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		if (base.InternalAtmosphere != null)
		{
			AtmosphericEventInstance.CreateSet(base.InternalAtmosphere, newGasMix);
		}
	}

	public virtual int GetNextSlotId(int slotIndex, bool isForward)
	{
		if (Slots == null)
		{
			return -1;
		}
		if (Slots.Count == 0)
		{
			return 0;
		}
		int num = slotIndex;
		num += (isForward ? 1 : (-1));
		num %= Slots.Count;
		if (num < 0)
		{
			num += Slots.Count;
		}
		return num;
	}

	public virtual bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (!HasAnySlots)
		{
			return false;
		}
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return false;
		}
		Slot slot = Slots[slotId];
		switch (logicSlotType)
		{
		case LogicSlotType.Occupied:
		case LogicSlotType.OccupantHash:
		case LogicSlotType.Quantity:
		case LogicSlotType.Damage:
		case LogicSlotType.Class:
		case LogicSlotType.MaxQuantity:
		case LogicSlotType.PrefabHash:
		case LogicSlotType.ReferenceId:
			return true;
		case LogicSlotType.Pressure:
		case LogicSlotType.Temperature:
			return slot.Type == Slot.Class.GasCanister;
		case LogicSlotType.Charge:
		case LogicSlotType.ChargeRatio:
			return slot.Type == Slot.Class.Battery;
		case LogicSlotType.FilterType:
			return slot.Type == Slot.Class.GasFilter;
		default:
			return false;
		}
	}

	public virtual double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (!HasAnySlots)
		{
			return 0.0;
		}
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return 0.0;
		}
		Slot slot = Slots[slotId];
		switch (logicSlotType)
		{
		case LogicSlotType.PrefabHash:
			return ((double?)slot.Occupant?.PrefabHash) ?? 0.0;
		case LogicSlotType.ReferenceId:
			return slot.Occupant?.ReferenceId ?? 0;
		case LogicSlotType.Occupied:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.OccupantHash:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return slot.Occupant.PrefabHash;
		case LogicSlotType.Quantity:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return ((double?)slot.Get<IQuantity>()?.GetQuantity) ?? 1.0;
		case LogicSlotType.MaxQuantity:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return ((double?)slot.Get<IQuantity>()?.GetMaxQuantity) ?? 1.0;
		case LogicSlotType.Damage:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return slot.Occupant.DamageState.TotalRatio;
		case LogicSlotType.Pressure:
			return (slot.Occupant?.InternalAtmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicSlotType.Temperature:
			return (slot.Occupant?.InternalAtmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicSlotType.Charge:
		case LogicSlotType.ChargeRatio:
			return BatteryCell.GetLogicValue(slot.Occupant, logicSlotType);
		case LogicSlotType.Class:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return (int)slot.Occupant.SlotType;
		case LogicSlotType.FilterType:
		{
			if (!slot.Contains<GasFilter>(out var occupant))
			{
				return 0.0;
			}
			return (double)occupant.FilterType;
		}
		default:
			return 0.0;
		}
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_physicalDensePool.CanAddToPool(densePool))
		{
			return _physicalDensePool.AddToPool(densePool, slot);
		}
		if (_dynamicThingsDensePool.CanAddToPool(densePool))
		{
			return _dynamicThingsDensePool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_physicalDensePool.OnRemovedFrom(densePool);
		_dynamicThingsDensePool.OnRemovedFrom(densePool);
	}

	public static void ItemManufactured(DynamicThing item, int quantity)
	{
		LastManufacturedItem = item;
		if (LastManufacturedItem is Stackable stackable)
		{
			stackable.SetQuantity(quantity);
		}
		DynamicThing.OnManufactured?.Invoke();
	}

	public virtual bool UseDefaultUiUsingSounds()
	{
		return true;
	}

	public virtual bool CanBeExposedToStorm()
	{
		return Room == null;
	}

	public virtual bool CheckTogglePower()
	{
		return CanTogglePower;
	}

	public override ShadowCastingMode GetShadowCastingMode()
	{
		ThingShadowMode thingShadowMode = OcclusionManager.ThingShadowMode;
		if (thingShadowMode == ThingShadowMode.High || (uint)(thingShadowMode - 2) <= 1u)
		{
			if (ParentSlot != null)
			{
				if (!base.AsDynamicThing.ParentSlot.OccupantCastsShadows || ParentSlot.Parent.IsOccluded)
				{
					return ShadowCastingMode.Off;
				}
				return ShadowCastingMode.On;
			}
			if (!(SurfaceArea < WorldManager.ShadowCullSize))
			{
				return ShadowCastingMode.On;
			}
			return ShadowCastingMode.Off;
		}
		return ShadowCastingMode.Off;
	}

	public static void InitCollisionSoundMap()
	{
		string[] names = Enum.GetNames(typeof(CollisionClass));
		CollisionSoundMap = new int[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			CollisionSoundMap[i] = Animator.StringToHash(names[i]);
		}
	}

	public static int GetCollisionSoundHash(CollisionClass collisionClass)
	{
		if ((int)collisionClass >= CollisionSoundMap.Length)
		{
			return -1;
		}
		return CollisionSoundMap[(int)collisionClass];
	}

	private async UniTaskVoid OnLeaveRoomThreadSafe()
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		OnLeaveRoom?.Invoke();
	}

	private async UniTaskVoid OnEnterRoomThreadSafe()
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		OnEnterRoom?.Invoke();
	}

	public virtual void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.Smelt(localAtmosphere, reagentMixture);
				return;
			}
		}
		if (ReagentMixture != null)
		{
			reagentMixture.Add(ReagentMixture);
		}
		Achievements.AssessOops(this);
		OnServer.Destroy(this);
	}

	public virtual void Recycle()
	{
		if (ThreadedManager.IsThread)
		{
			DestroyFromThread().Forget();
		}
		else
		{
			OnServer.Destroy(this);
		}
	}

	public virtual bool ShouldToggleOn()
	{
		return true;
	}

	public virtual void LateUpdateEachFrame()
	{
	}

	public override bool ExistsInHierarchy(Thing otherThing)
	{
		if (otherThing == null || !IsChild)
		{
			return false;
		}
		for (Thing parent = ParentSlot.Parent; parent is DynamicThing dynamicThing; parent = dynamicThing.ParentSlot.Parent)
		{
			if (parent == otherThing)
			{
				return true;
			}
			if (!dynamicThing.IsChild)
			{
				return false;
			}
		}
		return false;
	}

	public virtual void SetRenderersToDefaultLayer()
	{
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.BaseLayer = Layers.Default;
			renderer.SetLayer(Layers.Default);
		}
	}

	private void SetRenderersToLayer(int layer)
	{
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.SetLayer(layer);
		}
	}

	private void SetRenderersToBaseLayer()
	{
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.SetLayer(renderer.BaseLayer);
		}
	}

	public virtual void OnParentLocalityChange()
	{
		foreach (Slot slot in Slots)
		{
			if (!(slot.Occupant == null))
			{
				slot.Occupant.OnParentLocalityChange();
			}
		}
		SetSoundMixerGroup();
	}

	public void SetWorldAtmosphere()
	{
		Slot parentSlot = ParentSlot;
		if (parentSlot != null)
		{
			if (parentSlot.Parent is IRocketInterior rocketInterior && rocketInterior.CrewModule != null)
			{
				WorldAtmosphere = rocketInterior.CrewModule.InternalAtmosphere;
				return;
			}
			if (parentSlot.UseInternalAtmosphere)
			{
				WorldAtmosphere = parentSlot.Parent.InternalAtmosphere;
				return;
			}
			if ((bool)parentSlot.Parent.AsDynamicThing)
			{
				WorldAtmosphere = parentSlot.Parent.AsDynamicThing.WorldAtmosphere;
				return;
			}
			Thing thing = parentSlot.Parent?.RootParent ?? this;
			Cell cell = base.GridController.GetCell(thing.WorldGrid);
			WorldAtmosphere = (Cell.IsInCrewModule(cell, out var crewModule) ? crewModule.InternalAtmosphere : base.GridController.AtmosphericsController.SampleGlobalAtmosphere(thing.WorldGrid));
			return;
		}
		WorldAtmosphere = (Cell.IsInCrewModule(Cell, out var crewModule2) ? crewModule2.InternalAtmosphere : base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid));
		if (_hasAtmosphere != HasAtmosphere)
		{
			if (HasAtmosphere)
			{
				OnAtmosphereGain();
			}
			else
			{
				OnAtmosphereLost();
			}
			_hasAtmosphere = HasAtmosphere;
		}
	}

	public override object GetModXmlType()
	{
		return new DynamicThingModData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is DynamicThingModData dynamicThingModData)
		{
			if (!float.IsNaN(dynamicThingModData.AtmosphereDampeningScale))
			{
				AtmosphereDampeningScale = dynamicThingModData.AtmosphereDampeningScale;
			}
			if (dynamicThingModData.TradeValue >= 0)
			{
				TradeValue = dynamicThingModData.TradeValue;
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (GameManager.GameState != GameState.None && savedData is DynamicThingSaveData dynamicThingSaveData)
		{
			dynamicThingSaveData.ParentSlotId = (IsChild ? ParentSlot.SlotIndex : 0);
			dynamicThingSaveData.ParentReferenceId = (IsChild ? ParentSlot.Parent.ReferenceId : 0);
			dynamicThingSaveData.Dragged = Joint != null;
			dynamicThingSaveData.DragOffset = ((Joint != null) ? DragOffset : Vector3.zero);
			dynamicThingSaveData.Velocity = ((RigidBody != null && !RigidBody.isKinematic) ? RigidBody.velocity : Vector3.zero);
			dynamicThingSaveData.AngularVelocity = ((RigidBody != null && !RigidBody.isKinematic) ? RigidBody.angularVelocity : Vector3.zero);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DynamicThingSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public DynamicThingSaveData ToSiloData()
	{
		return SerializeSave() as DynamicThingSaveData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is DynamicThingSaveData dynamicThingSaveData)
		{
			LastPhysicsUpdate.WorldPosition = dynamicThingSaveData.WorldPosition;
			LastPhysicsUpdate.WorldRotation = dynamicThingSaveData.WorldRotation;
			if (!RigidBody.isKinematic)
			{
				RigidBody.velocity = dynamicThingSaveData.Velocity;
				RigidBody.angularVelocity = dynamicThingSaveData.AngularVelocity;
			}
			LastPhysicsUpdate.Velocity = dynamicThingSaveData.Velocity;
			LastPhysicsUpdate.AngularVelocity = dynamicThingSaveData.AngularVelocity;
			ParentReferenceId = dynamicThingSaveData.ParentReferenceId;
			MoveToParent(dynamicThingSaveData.ParentReferenceId, dynamicThingSaveData.Dragged, dynamicThingSaveData.ParentSlotId, dynamicThingSaveData.DragOffset);
			if (dynamicThingSaveData.ParentReferenceId == 0L)
			{
				LodManager.EnqueueRequesterToUpdate(this);
			}
		}
	}

	private void MoveToParent(long parentReferenceId, bool dragged, int parentSlotId, Vector3 dragOffset)
	{
		if (parentReferenceId == 0L)
		{
			return;
		}
		Thing thing = Referencable.Find<Thing>(parentReferenceId);
		if ((object)thing != null)
		{
			if (!dragged)
			{
				try
				{
					MoveToSlot(thing.Slots[parentSlotId], thing);
					return;
				}
				catch (ArgumentOutOfRangeException)
				{
					ConsoleWindow.PrintError($"Slot {parentSlotId} does not exist on thing {thing.DisplayName}({thing.ReferenceId}) so cannot move {DisplayName}({base.ReferenceId}) to slot.");
					return;
				}
				catch (Exception ex2)
				{
					ConsoleWindow.PrintError("An unexpected error occurred: " + ex2.Message);
					throw;
				}
			}
			DragInSlot(thing.Slots[parentSlotId], dragOffset);
		}
		else
		{
			MoveToParentWhenReady(parentReferenceId, dragged, parentSlotId, dragOffset).Forget();
		}
	}

	private async UniTaskVoid MoveToParentWhenReady(long parentReferenceId, bool dragged, int parentSlotId, Vector3 dragOffset)
	{
		AwaitingMoveWhenReady.Add(this);
		float startTime = Time.unscaledTime;
		float timeToWait = 10f;
		Thing parent = null;
		while (!parent)
		{
			parent = Referencable.Find<Thing>(parentReferenceId);
			if (Time.unscaledTime - startTime > timeToWait)
			{
				AwaitingMoveWhenReady.Remove(this);
				ConsoleWindow.PrintError(" not initialized ParentId : " + parentReferenceId);
				return;
			}
			await UniTask.NextFrame();
		}
		if (dragged)
		{
			DragInSlot(parent.Slots[parentSlotId], dragOffset);
		}
		else
		{
			MoveToSlot(parent.Slots[parentSlotId], parent);
		}
		AwaitingMoveWhenReady.Remove(this);
	}

	public virtual string GetDeconstructText()
	{
		return string.Format(Localization.GetInterface("DeconstructToolTipText"), Localization.GetThingName(ExitTool.ToolExit.PrefabName));
	}

	public virtual StringBuilder GetSlotTooltip()
	{
		return new StringBuilder();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = (Settings.CurrentData.ExtendedTooltips ? GetExtendedText().ToString() : string.Empty);
		PassiveTooltip result = passiveTooltip;
		if ((bool)ExitTool?.ToolExit && (bool)InventoryManager.ParentHuman && InventoryManager.ActiveHandSlot.Contains<Tool>())
		{
			result.DeconstructString = GetDeconstructText();
		}
		return result;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem || !CanDeconstruct())
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = ExitTool.ExitTime,
			ActionMessage = ActionStrings.Deconstruct
		};
		Tool tool = sourceItem as Tool;
		if ((bool)tool && (bool)ExitTool.ToolExit && ExitTool.IsToolExit(tool))
		{
			if ((bool)tool && !tool.IsOperable)
			{
				delayedActionInstance.IsDisabled = true;
				PowerTool powerTool = tool as PowerTool;
				if ((bool)powerTool && !powerTool.Battery)
				{
					return delayedActionInstance.Fail(GameStrings.ToolDoesNotHaveAnythingInSlot, tool.ToTooltip(), powerTool.BatterySlot.ToTooltip());
				}
				if ((bool)powerTool && (bool)powerTool.Battery && powerTool.Battery.IsEmpty)
				{
					return delayedActionInstance.Fail(GameStrings.ToolDoesNotHaveEnoughCharge, tool.ToTooltip(), powerTool.Battery.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.ToolCanNotCompleteTask, tool.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance;
				}
				return null;
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				ConstructionEventInstance eventInstance = new ConstructionEventInstance
				{
					Parent = this,
					Position = attack.Position,
					Rotation = ThingTransform.rotation,
					SteamId = base.OwnerClientId,
					OtherHandSlot = attack.OtherHand
				};
				foreach (Slot slot in Slots)
				{
					if (deleteSlotItemsOnDeconstruct)
					{
						OnServer.Destroy(slot.Get());
					}
					else if ((bool)slot.Occupant)
					{
						slot.PlayerMoveToWorld();
					}
				}
				if (!(this is DynamicGasCanister { HasBlown: not false }))
				{
					ExitTool.Deconstruct(eventInstance);
				}
				OnServer.Destroy(this);
				return delayedActionInstance;
			}
			if ((bool)tool && !tool.OnUseItem(ExitTool.ExitQuantity, this))
			{
				return null;
			}
		}
		return base.AttackWith(attack, doAction);
	}

	public virtual bool TryInteractWithSlotOccupant(Interactable interactable, out DelayedActionInstance actionInstance, bool doAction = true)
	{
		actionInstance = null;
		return false;
	}

	protected void ItemMerged()
	{
		if (DynamicThing.ParentChangedHashAndQuantityEvent != null && this is IQuantity quantity)
		{
			DynamicThing.ParentChangedHashAndQuantityEvent(this, quantity.GetQuantity);
		}
	}

	public virtual bool CanDeconstruct()
	{
		return true;
	}

	public virtual void OnCollisionEnter(Collision collision)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (this.OnCollision != null)
		{
			Thing thing = Thing.Find(collision.collider);
			if (thing != null)
			{
				this.OnCollision(thing);
			}
		}
		if (XmlSaveLoad.IsReadyToPlayWorldAudio && !SuppressSound && !(ImpactAudioCoolDown > GameManager.GameTime))
		{
			float num = ((GameManager.DeltaTime > 0f) ? GameManager.DeltaTime : 1f);
			float num2 = collision.impulse.magnitude / num;
			if (!(num2 < ImpactForceThreshhold))
			{
				float volumeMultiplier = Mathf.Min(num2, 300f) / 300f;
				ImpactAudioCoolDown = GameManager.GameTime + UnityEngine.Random.Range(0.15f, 0.2f);
				float pitchMultiplier = Mathf.Lerp(2f, 0.5f, Mathf.Clamp01(SizeMultiplier / 2f)) * UnityEngine.Random.Range(0.985f, 1.03f);
				PlayCollisionSound(GetCollisionSoundHash(CollisionSound), volumeMultiplier, pitchMultiplier);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("LavaObject"))
		{
			IsInLavaObject = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("LavaObject"))
		{
			IsInLavaObject = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("LavaObject"))
		{
			IsInLavaObject = false;
		}
	}

	public virtual void OnPrimaryUseStart()
	{
	}

	public virtual void OnPrimaryUseEnd()
	{
	}

	public Vector3 GetDragPosition(DynamicThing parentThing, Vector3 offset)
	{
		Vector3 forward = Vector3.forward;
		forward.z += Mathf.Abs(offset.x);
		return parentThing.Position + parentThing.ThingTransform.rotation * forward;
	}

	public virtual string GetQuantityText()
	{
		return null;
	}

	public virtual string GetSecondaryNameText()
	{
		return string.Empty;
	}

	private Vector3 FlatWorldSpaceDragOffset()
	{
		return Vector3.ProjectOnPlane(base.transform.TransformVector(DragOffset), Vector3.up);
	}

	private float ClampJointAngle(float angle)
	{
		if (angle > 180f)
		{
			return -360f + angle;
		}
		if (angle < -180f)
		{
			return 360f + angle;
		}
		return angle;
	}

	private void UpdatePivotJoint()
	{
		if ((bool)_pivotJoint && (bool)_pivotObject)
		{
			if (!Joint)
			{
				UnityEngine.Object.Destroy(_pivotObject);
			}
			else if (ParentSlot?.Parent is Human { HasAuthority: not false } human)
			{
				float y = human.CharacterRotationY.eulerAngles.y;
				float targetPosition = ClampJointAngle(y - _rotationOnStartDrag);
				_pivotJoint.spring = new JointSpring
				{
					spring = 2000f,
					damper = 200f,
					targetPosition = targetPosition
				};
			}
		}
	}

	private float GetRemainderAngle(float angle)
	{
		if (angle > 0f)
		{
			return 180f - angle;
		}
		return -180f - angle;
	}

	private async UniTaskVoid GraduallyIncreaseMassScale()
	{
		while ((bool)_pivotJoint && _pivotJoint.connectedMassScale < 1f)
		{
			_pivotJoint.connectedMassScale += Time.deltaTime;
			await UniTask.WaitForEndOfFrame();
		}
	}

	protected void AlignObjectToDragPoint(DynamicThing parentThing, Vector3 flatCameraForward)
	{
		Vector3 vector = FlatWorldSpaceDragOffset();
		ThingTransform.up = Vector3.up;
		Vector3 to = FlatWorldSpaceDragOffset();
		float num = Vector3.SignedAngle(vector, to, Vector3.up);
		ThingTransform.Rotate(Vector3.up, 0f - num);
		Vector3 to2 = FlatWorldSpaceDragOffset();
		float angle = Vector3.SignedAngle(flatCameraForward, to2, Vector3.up);
		ThingTransform.RotateAround(ThingTransform.TransformPoint(DragOffset), Vector3.up, GetRemainderAngle(angle));
		Vector3 vector2 = ThingTransform.TransformPoint(DragOffset);
		Vector3 vector3 = parentThing.ThingTransform.position + flatCameraForward.normalized + Vector3.up * DragYOffset;
		ThingTransform.position -= vector2 - vector3;
	}

	protected void AttachJoints(DynamicThing parentThing, Vector3 flatCameraForward, Human humanDragging)
	{
		_pivotObject = new GameObject();
		_pivotObject.transform.position = parentThing.ThingTransform.position;
		_pivotObject.name = "DragPivot";
		Rigidbody rigidbody = _pivotObject.AddComponent<Rigidbody>();
		rigidbody.mass = 0.1f;
		rigidbody.useGravity = false;
		rigidbody.maxDepenetrationVelocity = 10f;
		_pivotJoint = _pivotObject.AddComponent<HingeJoint>();
		_pivotJoint.connectedBody = parentThing.RigidBody;
		_pivotJoint.autoConfigureConnectedAnchor = false;
		_pivotJoint.connectedAnchor = Vector3.up * DragYOffset;
		_pivotJoint.anchor = -flatCameraForward.normalized;
		_pivotJoint.axis = Vector3.up;
		_pivotJoint.useSpring = true;
		_pivotJoint.spring = new JointSpring
		{
			spring = 2000f,
			damper = 200f
		};
		_pivotJoint.connectedMassScale = 0.001f;
		GraduallyIncreaseMassScale().Forget();
		Joint = base.gameObject.AddComponent<CharacterJoint>();
		Joint.connectedBody = rigidbody;
		Joint.autoConfigureConnectedAnchor = false;
		Joint.connectedAnchor = Vector3.zero;
		Joint.anchor = DragOffset;
		Joint.axis = Vector3.right;
		Joint.swingAxis = Vector3.up;
		Joint.highTwistLimit = default(SoftJointLimit);
		Joint.lowTwistLimit = default(SoftJointLimit);
		Joint.swing1Limit = new SoftJointLimit
		{
			limit = 80f
		};
		Joint.swing2Limit = new SoftJointLimit
		{
			limit = 20f
		};
		if (parentThing.HasAuthority)
		{
			Joint.breakForce = float.PositiveInfinity;
			Joint.breakTorque = float.PositiveInfinity;
		}
		_rotationOnStartDrag = humanDragging.CharacterRotationY.eulerAngles.y;
	}

	private void OnJointBreak(float breakForce)
	{
		Debug.Log("A joint has just been broken!, force: " + breakForce);
	}

	public virtual bool DragInSlot(Slot destinationSlot, Vector3 offset)
	{
		DynamicThing dynamicThing = destinationSlot.Parent as DynamicThing;
		if (!dynamicThing || Joint != null)
		{
			return false;
		}
		DragOffset = offset;
		float mass = dynamicThing.RigidBody.mass;
		float mass2 = RigidBody.mass;
		float num = mass / mass2;
		if (dynamicThing.HasAuthority)
		{
			RigidBody.interpolation = RigidbodyInterpolation.Extrapolate;
		}
		Human human = dynamicThing as Human;
		Vector3 flatCameraForward = (dynamicThing.HasAuthority ? human.CharacterRotationY.forward : dynamicThing.ThingTransform.forward);
		AlignObjectToDragPoint(dynamicThing, flatCameraForward);
		AttachJoints(dynamicThing, flatCameraForward, human);
		RigidBody.mass = _startMass * DraggedMassScale * num;
		HandleCollisionWith(dynamicThing);
		destinationSlot.Take(this);
		ParentSlot = destinationSlot;
		ParentSlot.RefreshSlotDisplay();
		CheckOnSlot();
		return true;
	}

	public virtual void OnGravityEnabled()
	{
		RigidBody.useGravity = true;
	}

	public virtual void OnGravityDisabled()
	{
		RigidBody.useGravity = false;
	}

	public void ResetInterpolation()
	{
		LastPhysicsUpdate.WorldPosition = base.ThingTransformPosition;
		LastPhysicsUpdate.WorldRotation = ThingTransform.rotation;
		LastPhysicsUpdate.Velocity = Vector3.zero;
		LastPhysicsUpdate.AngularVelocity = Vector3.zero;
	}

	public void ResetInterpolation(Vector3 velocity, Vector3 angularVelocity)
	{
		ResetInterpolation();
		LastPhysicsUpdate.Velocity = velocity;
		LastPhysicsUpdate.AngularVelocity = angularVelocity;
		if (!RigidBody.isKinematic)
		{
			RigidBody.velocity = velocity;
			RigidBody.angularVelocity = angularVelocity;
		}
		LastPhysicsUpdate.UseLocal = false;
	}

	public virtual void ProcessPhysicsUpdate(DynamicThingPosition updateData)
	{
		if (!HasAuthority)
		{
			LastPhysicsUpdate = updateData;
		}
	}

	public virtual void UpdateNetworkPosition()
	{
		bool flag = this is Npc;
		if (GameManager.RunSimulation && IsEntity && base.AsEntity.OrganBrain != null && (flag || !base.AsEntity.OrganBrain.IsOnline || base.AsEntity.IsRagdoll))
		{
			return;
		}
		Vector3 worldPosition = LastPhysicsUpdate.WorldPosition;
		if (!IsOccluded && !LastPhysicsUpdate.IsSleeping)
		{
			float num = (LastPhysicsUpdate.Snap ? 20f : LerpRate);
			float num2 = (LastPhysicsUpdate.Snap ? 20f : RotationLerpRate);
			if (RocketMath.DistanceSquared(base.Position, LastPhysicsUpdate.WorldPosition) > 0.0001f)
			{
				RigidBody.MovePosition(Vector3.Lerp(RigidBody.position, worldPosition, Time.deltaTime * num + _stmoothKinematicRate));
			}
			if (Quaternion.Dot(RigidBody.rotation, LastPhysicsUpdate.WorldRotation) < 0.9999f)
			{
				RigidBody.MoveRotation(Quaternion.Lerp(RigidBody.rotation, LastPhysicsUpdate.WorldRotation, Mathf.Clamp01(Time.deltaTime * num2)));
			}
			if (!RigidBody.isKinematic)
			{
				RigidBody.velocity = LastPhysicsUpdate.Velocity;
			}
			if (RocketMath.DistanceSquared(RigidBody.angularVelocity, LastPhysicsUpdate.AngularVelocity) > 0.0001f)
			{
				RigidBody.angularVelocity = Vector3.Lerp(RigidBody.angularVelocity, LastPhysicsUpdate.AngularVelocity, Time.deltaTime * num);
			}
		}
		else
		{
			ActiveRigidbody.MovePosition(worldPosition);
			ActiveRigidbody.MoveRotation(LastPhysicsUpdate.WorldRotation.normalized);
		}
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			UpdatePivotJoint();
			if (IsEmergency && ParentSlot == null)
			{
				DamageState.Damage(ChangeDamageType.Increment, 0.05f, DamageUpdateType.Brute);
			}
			if (!IsChild)
			{
				OutOfBounds();
			}
		}
	}

	public virtual void OutOfBounds()
	{
		if (IsOutOfBounds && GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
		}
	}

	protected virtual void PrepareUpdateMessage(ref DynamicThingPosition currentPosition)
	{
		if (!base.IsBeingDestroyed)
		{
			currentPosition.ForceUpdate = false;
			currentPosition.DynamicThingId = base.netId;
			currentPosition.WorldPosition = base.Position;
			currentPosition.WorldRotation = Rotation;
			currentPosition.AngularVelocity = ActiveRigidbody.angularVelocity;
			currentPosition.Velocity = ActiveRigidbody.velocity;
			currentPosition.IsSleeping = ActiveRigidbody.IsSleeping();
			currentPosition.Snap = _snapNextPosition;
			_snapNextPosition = false;
		}
	}

	private float GetRotationRelativeToUp()
	{
		return Mathf.Repeat(Vector3.SignedAngle(Forward, Vector3.forward, Vector3.up), 360f);
	}

	public virtual Vector3 GetStormWindVector()
	{
		if (WeatherManager.IsAboveStormHeight(RootParent.Position.y))
		{
			return Vector3.zero;
		}
		return WeatherManager.StormDirectionVector * StormWindStrength;
	}

	public override void OnThreadUpdate()
	{
		VelocityMagnitude = _velocity.magnitude;
		_parentVelocity = Vector3.zero;
		RelativeVelocity = RocketMath.InverseTransformDirecton(_velocity, Rotation);
		Orientation = GetRotationRelativeToUp();
		if (WorldAtmosphere != null)
		{
			_velocityVector = WorldAtmosphere.Direction * AtmosphereDampeningScale;
			_directionToCenter = (WorldAtmosphere.WorldPosition - CenterPosition) * (_velocityVector.magnitude * AtmosphereDampeningScale * 0.5f);
			_pressureVelocityScale = Mathf.Clamp(WorldAtmosphere.PressureGassesAndLiquids.ToFloat() / Chemistry.PressureFullyApplyVelocity.ToFloat(), 0f, 1f);
			_pressureVelocityScale = ((Room != null) ? (_pressureVelocityScale * 0.5f) : _pressureVelocityScale);
			_atmosphericVelocity = (_velocityVector + _directionToCenter) * _pressureVelocityScale;
			if (CanBeExposedToStorm())
			{
				_atmosphericVelocity += GetStormWindVector();
			}
			_atmosVelocityMagnitude = _atmosphericVelocity.magnitude;
		}
		else
		{
			_atmosVelocityMagnitude = 0f;
			_velocityVector = Vector3.zero;
			_directionToCenter = Vector3.zero;
			_pressureVelocityScale = 0f;
			_atmosphericVelocity = Vector3.zero;
		}
		_movePosition = RocketMath.DistanceSquared(base.Position, LastPhysicsUpdate.WorldPosition) > 0.0001f;
		_angularDrag = Mathf.Lerp(0.2f, 2f, WorldAtmosphere?.RatioOneAtmosphereUnclamped ?? 0f);
	}

	public PlayableAreaRule CheckPlayableArea()
	{
		PlayableAreaRule playableAreaRule = PlayableAreaRule.Valid;
		foreach (PlayableAreaData playableAreaData in WorldSetting.Current.Data.PlayableAreaDatas)
		{
			if (playableAreaData.Evaluate(this) && playableAreaData.Rule > playableAreaRule)
			{
				playableAreaRule = playableAreaData.Rule;
			}
		}
		return playableAreaRule;
	}

	public void SetOnBaseOfSlot()
	{
		base.ThingTransformLocalPosition = new Vector3(0f, Bounds.extents.y - Bounds.center.y, 0f);
	}

	public void ScaleToInteractable(Interactable interactable, float scaleFactor = 1f, bool attemptDelayedScaled = true)
	{
		if (interactable != null)
		{
			Vector3 size = interactable.Bounds.size;
			if (!(size == Vector3.zero))
			{
				Vector3 size2 = Bounds.size;
				float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f) * scaleFactor;
				ThingTransform.localScale = Vector3.one * num;
			}
		}
	}

	public void ScaleToSlot(float scaleFactor = 1f)
	{
		Slot parentSlot = ParentSlot;
		if (!(parentSlot.Collider == null) && !(parentSlot.Size == Vector3.zero))
		{
			Vector3 size = Bounds.size;
			float num = Mathf.Min(parentSlot.Size.x / size.x, parentSlot.Size.y / size.y, parentSlot.Size.z / size.z, 1f) * scaleFactor;
			ThingTransform.localScale = Vector3.one * num;
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (ParentSlot != null && ParentSlot.Display != null)
		{
			ParentSlot.RefreshSlotDisplay();
		}
	}

	public virtual bool ForceKinematicClient(bool kinematic, bool localCheck)
	{
		return false;
	}

	public virtual bool IsStop()
	{
		if (GameManager.RunSimulation)
		{
			if (_lastLocalPostion.Equals(Vector3.zero) || _currentLocalPostion.Equals(Vector3.zero))
			{
				_lastLocalPostion = _currentLocalPostion;
				return false;
			}
			if (_lastLocalPostion.Equals(_currentLocalPostion))
			{
				return true;
			}
			if ((_lastLocalPostion - _currentLocalPostion).sqrMagnitude > ForceKinematicDistance)
			{
				_lastLocalPostion = _currentLocalPostion;
				return false;
			}
			_lastLocalPostion = _currentLocalPostion;
		}
		else
		{
			if (_lastLocalPostion.Equals(Vector3.zero) || _currentLocalPostion.Equals(Vector3.zero))
			{
				_lastLocalPostion = LastPhysicsUpdate.LocalPosition;
				return false;
			}
			if (_lastLocalPostion.Equals(_currentLocalPostion))
			{
				return true;
			}
			if ((_lastLocalPostion - LastPhysicsUpdate.LocalPosition).sqrMagnitude > ForceKinematicDistance)
			{
				_lastLocalPostion = LastPhysicsUpdate.LocalPosition;
				return false;
			}
			_lastLocalPostion = LastPhysicsUpdate.LocalPosition;
		}
		return true;
	}

	public virtual void PhysicsUpdate()
	{
		if ((!(this is ISpatial) && IsHiddenInSlot) || !ActiveRigidbody)
		{
			return;
		}
		if (IsOccluded && !GameManager.RunSimulation)
		{
			WorldCenterOfMass = LastPhysicsUpdate.WorldPosition;
			base.Position = LastPhysicsUpdate.WorldPosition;
			Rotation = LastPhysicsUpdate.WorldRotation;
		}
		else
		{
			WorldCenterOfMass = ActiveRigidbody.worldCenterOfMass;
			Rotation = ActiveRigidbody.rotation;
			base.Position = ActiveRigidbody.position;
		}
		MovingFast = IsMovingFast();
		Forward = ThingTransform.forward;
		_slotStateDirty = false;
		if (ParentSlot != null && !IsBeingDragged)
		{
			return;
		}
		if (ActiveRigidbody.isKinematic)
		{
			if (!RootParent.HasAuthority && IsForceKinematic && !IsOccluded)
			{
				ActiveRigidbody.MoveRotation(Quaternion.Lerp(ActiveRigidbody.rotation, LastPhysicsUpdate.WorldRotation, Time.deltaTime * LerpRate));
				ActiveRigidbody.MovePosition(Vector3.Lerp(ActiveRigidbody.position, LastPhysicsUpdate.WorldPosition, Time.deltaTime * LerpRate));
			}
			CacheVelocity();
			return;
		}
		if (!RootParent.HasAuthority && (!Joint || IsEntity))
		{
			UpdateNetworkPosition();
		}
		if (!OnConveyor)
		{
			RigidBody.angularDrag = _angularDrag;
		}
		if (_pressureVelocityScale > 0f && WorldAtmosphere != null && _atmosVelocityMagnitude > 0.1f && AtmosControl && !RigidBody.isKinematic)
		{
			RigidBody.velocity = Vector3.Lerp(RigidBody.velocity, _atmosphericVelocity + _parentVelocity, Time.fixedDeltaTime * 0.5f);
		}
		if (VelocityMagnitude > MaxMovementSpeed && !RigidBody.isKinematic)
		{
			RigidBody.velocity = Vector3.ClampMagnitude(RigidBody.velocity, MaxMovementSpeed);
		}
		if (Room != null && AddExtraGravity && !RigidBody.isKinematic)
		{
			RigidBody.AddForce(WorldManager.EarthGravityOffset, ForceMode.Acceleration);
		}
		bool flag = WorldManager.HasGravityAtHeight(Transform.position.y);
		if (!ActiveRigidbody.useGravity && (Room != null || flag))
		{
			OnGravityEnabled();
		}
		else if (ActiveRigidbody.useGravity && Room == null && !flag)
		{
			OnGravityDisabled();
		}
		bool flag2 = GlobalAtmosphereLiquid.IsUnderGlobalLiquid(base.Position);
		if (!IsEntity && !RigidBody.isKinematic)
		{
			Atmosphere worldAtmosphere = WorldAtmosphere;
			if ((worldAtmosphere != null && worldAtmosphere.LiquidVolumeRatio > 0f) || flag2)
			{
				float num = ((position - base.WorldGrid.Value.ToVector3()).y + 1f) * 0.5f;
				Atmosphere worldAtmosphere2 = _worldAtmosphere;
				float num2 = ((worldAtmosphere2 != null) ? (worldAtmosphere2.LiquidVolumeRatio + 0.075f) : (-1000f));
				if (flag2)
				{
					num2 = GlobalAtmosphereLiquid.GetWorldGridLiquidVolumeRatio(base.WorldGrid) + 0.075f;
				}
				if (num < num2)
				{
					float num3 = (num2 - num) * -10f;
					float y = RigidBody.velocity.y;
					Vector3 force = Vector3.up * Math.Clamp((-2.5f * num3 - 0.85f * y) * 5f, 0f, 15f);
					force *= (_isBuoyant ? 1f : 0.5f);
					RigidBody.AddForce(force, ForceMode.Acceleration);
					RigidBody.angularVelocity = Vector3.Lerp(RigidBody.angularVelocity, Vector3.zero, 0.01f);
					RigidBody.AddForce(-RigidBody.velocity * 0.5f, ForceMode.Acceleration);
					ApplyLiquidFlowForce();
				}
			}
		}
		CacheVelocity();
	}

	private bool IsMovingFast()
	{
		if (ParentSlot != null)
		{
			return false;
		}
		if (this is Entity)
		{
			return false;
		}
		return VelocityMagnitude * Time.deltaTime > _boundsMinSize;
	}

	private void ApplyLiquidFlowForce()
	{
		if (WorldAtmosphere != null)
		{
			_liquidFlowForce = Vector3.zero;
			Vector4 flowDirection = WorldAtmosphere.GetFlowDirection();
			if (flowDirection.x > 0f && flowDirection.y <= 0f)
			{
				_liquidFlowForce.x = -1f;
			}
			else if (flowDirection.x <= 0f && flowDirection.y > 0f)
			{
				_liquidFlowForce.x = 1f;
			}
			if (flowDirection.z > 0f && flowDirection.w <= 0f)
			{
				_liquidFlowForce.z = 1f;
			}
			else if (flowDirection.z <= 0f && flowDirection.w > 0f)
			{
				_liquidFlowForce.z = -1f;
			}
			if (!RocketMath.Approximately(_liquidFlowForce, Vector3.zero, 0.01f))
			{
				RigidBody.AddForce(_liquidFlowForce * 0.3f, ForceMode.Acceleration);
			}
		}
	}

	public void CacheVelocity()
	{
		_velocity = RigidBody.velocity;
	}

	public override bool ShouldIgnite(Atmosphere atmos)
	{
		Thing rootParent = RootParent;
		if (rootParent is Lander || rootParent is IRocketInternals)
		{
			return false;
		}
		return base.ShouldIgnite(atmos);
	}

	public virtual bool IsHiddenInParentSlot()
	{
		if (ParentSlot == null)
		{
			return false;
		}
		if (ParentSlot.HidesOccupant)
		{
			return true;
		}
		if (ParentSlot.Parent is DynamicThing dynamicThing)
		{
			return dynamicThing.IsHiddenInParentSlot();
		}
		return false;
	}

	public override void OnFireTick()
	{
		if (!IsBurnable || WorldAtmosphere == null || WorldAtmosphere.PressureGasses < Thing.MinimumIgnitionPressurePropane || IsHiddenInParentSlot())
		{
			if (base.IsBurning)
			{
				Extinguish();
			}
			return;
		}
		if (WorldAtmosphere != null && !base.IsBurning && ShouldIgnite(WorldAtmosphere))
		{
			base.IsBurning = true;
			OnFireStart();
		}
		if (!base.IsBurning)
		{
			return;
		}
		float num = ThingHealth * AtmosphericsManager.Instance.TickSpeedSeconds / BurnTime;
		float heatEnergyReleased = EnergyReleasedWhenBurned * num / 100f;
		DamageState?.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Burn);
		WorldAtmosphere = AtmosphericsManager.CloneGlobalAtmosphereThreadSafe(base.WorldGrid);
		if (WorldAtmosphere == null)
		{
			return;
		}
		lock (WorldAtmosphere)
		{
			if (WorldAtmosphere != null && OnFireConsume(WorldAtmosphere, heatEnergyReleased))
			{
				WorldAtmosphere.Sparked = true;
			}
			else
			{
				Extinguish();
			}
		}
	}

	public override void Explosion(Vector3 position, float force = 0f)
	{
		RigidBody.AddExplosionForce(force, position, 5f);
	}

	public override void OnDamageDestroyed()
	{
		base.OnDamageDestroyed();
		if (DeleteOnDestroyed && GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
		}
	}

	public override void Awake()
	{
		base.Awake();
		ParentSlot = null;
		LastPhysicsUpdate = new DynamicThingPosition
		{
			WorldPosition = base.ThingTransformPosition,
			WorldRotation = ThingTransform.rotation
		};
		ParentSlot = null;
		RigidBody.centerOfMass += CenterOfMassOffset;
		RigidBody.maxDepenetrationVelocity = 10f;
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (!collider.CompareTag("CollidersAlwaysVisible"))
			{
				if (collider.isTrigger)
				{
					_triggers.Add(collider);
				}
				else
				{
					_colliders.Add(collider);
				}
			}
		}
		_startMass = RigidBody.mass;
		OcclusionManager.Register(this);
		_boundsMinSize = Mathf.Min(Bounds.size.x, Mathf.Min(Bounds.size.y, Bounds.size.z));
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		WorldCenterOfMass = ActiveRigidbody.worldCenterOfMass;
		base.WorldGrid = new WorldGrid(CenterPosition);
	}

	public void SetColliders(bool on, bool always = false)
	{
		foreach (Collider collider in _colliders)
		{
			if (always || !collider.gameObject.CompareTag("CollidersAlwaysVisible"))
			{
				collider.enabled = on;
			}
		}
		foreach (Collider trigger in _triggers)
		{
			if (always || !trigger.gameObject.CompareTag("CollidersAlwaysVisible"))
			{
				trigger.enabled = on;
			}
		}
	}

	public virtual void SetPhysics(bool on, bool always = false)
	{
		SetColliders(on, always);
		IsKinematic = !on;
		RigidBody.isKinematic = !on;
	}

	public async UniTaskVoid SetPhysicsDelayedColliders(bool on, bool always = false)
	{
		float timer = 0.1f;
		IsKinematic = !on;
		RigidBody.isKinematic = !on;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			await UniTask.NextFrame();
		}
		SetColliders(on, always);
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		base.BeingDestroyed = true;
		LodManager.EnqueueRequesterToRemove(this);
		if (GameManager.GameState == GameState.None)
		{
			base.OnDestroy();
			return;
		}
		MoveToWorld();
		if (_currentRoom != null)
		{
			_currentRoom.Deregister(this);
			_currentRoom = null;
		}
		OcclusionManager.Deregister(this);
		foreach (MachineInputTrigger currentMachineInputTrigger in CurrentMachineInputTriggers)
		{
			currentMachineInputTrigger.ParentMachine.OnInputTriggerExit(this, currentMachineInputTrigger);
		}
		if (ParentSlot != null)
		{
			Thing parent = ParentSlot.Parent;
			Slot parentSlot = ParentSlot;
			ParentSlot.Empty();
			ParentSlot = null;
			if (!parent)
			{
				return;
			}
			parent.OnChildExitInventory(this);
			parent.OnChildDropped(this);
			parentSlot.RefreshSlotDisplay();
		}
		base.OnDestroy();
	}

	public virtual void SetWearVisibility(bool shouldUpdateLayers = true)
	{
		if (ParentSlot != null && ParentSlot.HidesOccupant)
		{
			if (shouldUpdateLayers)
			{
				base.gameObject.layer = 0;
			}
			SetVisibility(isVisible: false);
		}
		else if (ParentSlot != null && ParentSlot.Type == SlotType)
		{
			if (!(ParentSlot.Parent is Human))
			{
				if (shouldUpdateLayers)
				{
					SetRenderersToBaseLayer();
				}
				SetVisibility(!IsHiddenInSlot);
				return;
			}
			switch (SlotWearAction)
			{
			case SlotWearAction.None:
				if (shouldUpdateLayers)
				{
					SetRenderersToBaseLayer();
				}
				SetVisibility(!IsHiddenInSlot);
				break;
			case SlotWearAction.HideAll:
				if (shouldUpdateLayers)
				{
					SetRenderersToBaseLayer();
				}
				SetLightVisibility(isVisible: true);
				break;
			case SlotWearAction.HidePlayer:
				if (shouldUpdateLayers)
				{
					if (ParentSlot.Parent.HasAuthority)
					{
						SetRenderersToLayer(Layers.PlayerInvisible);
					}
					else
					{
						SetRenderersToBaseLayer();
					}
				}
				SetLightVisibility(isVisible: true, ParentSlot.Parent.HasAuthority && !CameraController.IsThirdPerson);
				break;
			case SlotWearAction.Player:
				if (shouldUpdateLayers)
				{
					if (ParentSlot.Parent.HasAuthority)
					{
						SetRenderersToLayer(Layers.Player);
					}
					else
					{
						SetRenderersToBaseLayer();
					}
				}
				break;
			}
		}
		else
		{
			if (shouldUpdateLayers)
			{
				SetRenderersToBaseLayer();
			}
			SetVisibility(isVisible: true);
		}
	}

	public virtual void OnDisplayInPlayerWindow()
	{
		if (ParentSlot != null)
		{
			ParentSlot.RefreshState();
		}
	}

	public virtual void CheckInteractionKeys()
	{
		if (!base.AllowInteraction || IsLocked)
		{
			return;
		}
		foreach (Interactable interactable in Interactables)
		{
			if (!string.IsNullOrEmpty(interactable.KeyMap) && Input.GetKeyDown(KeyManager.GetKey(interactable.KeyMap)))
			{
				interactable.PlayerInteractWith();
			}
		}
	}

	public virtual bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		if (base.IsBeingDestroyed || destinationSlot == null)
		{
			return false;
		}
		if (!CanEnter(destinationSlot))
		{
			return false;
		}
		if ((bool)destinationSlot.Occupant)
		{
			return false;
		}
		foreach (MachineInputTrigger currentMachineInputTrigger in CurrentMachineInputTriggers)
		{
			currentMachineInputTrigger.ParentMachine.OnInputTriggerExit(this, currentMachineInputTrigger);
		}
		if (IsChild)
		{
			Thing parent = ParentSlot.Parent;
			Slot parentSlot = ParentSlot;
			ParentSlot = null;
			parentSlot.Empty();
			parent.OnChildExitInventory(this);
			parentSlot.RefreshSlotDisplay();
			OnExitInventory(parent);
			parentSlot.OnExitEvent();
			HandleCollisionWith(parentSlot.Parent, ignore: false);
			if ((bool)Joint)
			{
				UnityEngine.Object.Destroy(Joint);
				RigidBody.mass = _startMass;
				if (parent.HasAuthority)
				{
					RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
				}
			}
		}
		else
		{
			base.WorldGrid = new WorldGrid(CenterPosition);
			base.GridController.AlertGridWatchers(base.WorldGrid, this, GridEvent.GridEventType.Leave);
		}
		if (IsForceKinematic)
		{
			ForceKinematicClient(kinematic: false, localCheck: true);
		}
		destinationSlot.Take(this);
		ParentSlot = destinationSlot;
		Transform parent2 = (destinationSlot.Location ? destinationSlot.Location : destinationSlot.Parent.ThingTransform);
		SetPhysics(on: false);
		ThingTransform.SetParent(parent2);
		base.ThingTransformLocalPosition = Vector3.zero;
		base.ThingTransformLocalRotation = Quaternion.identity;
		ParentSlot.RefreshSlotDisplay();
		ParentSlot.Parent.OnChildEnterInventory(this);
		OnEnterInventory(ParentSlot.Parent);
		ParentSlot.OnEnterEvent();
		HandleCollisionWith(ParentSlot.Parent);
		if (!(this is Human))
		{
			LodManager.EnqueueRequesterToRemove(this);
		}
		return true;
	}

	public Vector3 GetPrecisionPlacePoint(Vector3 start, Vector3 forward, float distance)
	{
		Vector3 vector = Vector3.zero;
		Human human = this as Human;
		if (human != null && Physics.Raycast(start, forward, out var hitInfo, distance, human.AllButPlayerImmune))
		{
			vector = hitInfo.point;
		}
		if (!(vector == Vector3.zero))
		{
			return vector;
		}
		return start + forward * distance;
	}

	public Vector3 GetSafeDropPosition(Vector3 start, Vector3 forward, float distance)
	{
		Human human = this as Human;
		Ray ray = new Ray(start, forward);
		RaycastHit hitInfo;
		if ((object)human == null)
		{
			Physics.Raycast(ray, out hitInfo, distance);
		}
		else
		{
			Physics.Raycast(ray, out hitInfo, distance, human.AllButPlayerImmune);
		}
		if (!hitInfo.collider || hitInfo.collider.isTrigger)
		{
			return start + forward * distance;
		}
		return hitInfo.point;
	}

	private Vector3 GetSafeDropPositionWithBoundsCheck(Vector3 start, Vector3 forward, float distance)
	{
		Ray ray = new Ray(start, forward);
		if (!((this is Human human) ? Physics.Raycast(ray, out var hitInfo, distance, human.AllButPlayerImmune) : Physics.Raycast(ray, out hitInfo, distance, ~(int)LayerMasks.CursorVoxel)) || hitInfo.collider.isTrigger)
		{
			return start + forward * distance;
		}
		return hitInfo.point - Bounds.extents.MaxComponent() * forward;
	}

	public bool GetSafeDropHit(Thing parent)
	{
		Human human = parent as Human;
		Vector3 vector = (((bool)human && (bool)human.AimIk) ? human.HelmetSlot.Location.position : ParentSlot.Parent.CenterPosition);
		Vector3 vector2 = ParentSlot.Parent.Bounds.extents.z * ParentSlot.Parent.ThingTransform.forward;
		Vector3 direction = (((bool)human && (bool)human.AimIk) ? human.HelmetSlot.Location.forward : ParentSlot.Parent.ThingTransform.forward);
		Ray ray = new Ray(vector + vector2, direction);
		RaycastHit hitInfo;
		if (human != null)
		{
			return Physics.Raycast(ray, out hitInfo, 2f, human.AllButPlayerImmune);
		}
		return false;
	}

	public override void SnapTransform(Vector3 transformPosition, Quaternion transformRotation)
	{
		if (ParentSlot == null)
		{
			base.SnapTransform(transformPosition, transformRotation);
			_snapNextPosition = true;
			ResetInterpolation();
		}
	}

	public virtual bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		if (ParentSlot == null)
		{
			return true;
		}
		Thing parent = ParentSlot.Parent;
		Slot parentSlot = ParentSlot;
		_snapNextPosition = true;
		if (!base.BeingDestroyed)
		{
			ThingTransform.parent = null;
		}
		if (!IsBeingDragged || !GameManager.RunSimulation)
		{
			if (!(this is Human { HasAuthority: not false }))
			{
				ThingTransform.rotation = worldRotation;
			}
			base.Position = worldPosition;
			base.ThingTransformPosition = base.Position;
		}
		if ((bool)parent.AsDynamicThing)
		{
			velocity += (parent.HasAuthority ? parent.AsDynamicThing.RigidBody.velocity : parent.AsDynamicThing.LastPhysicsUpdate.Velocity);
		}
		if (velocity.magnitude > 0f)
		{
			ResetInterpolation(velocity, angularVelocity);
		}
		else
		{
			ResetInterpolation();
		}
		if (force > 0f && !GetSafeDropHit(parent))
		{
			SetPhysicsDelayedColliders(on: true).Forget();
		}
		else
		{
			SetPhysics(on: true);
		}
		if (!RigidBody.isKinematic)
		{
			RigidBody.velocity = velocity;
			RigidBody.angularVelocity = angularVelocity;
		}
		if ((bool)Joint)
		{
			UnityEngine.Object.Destroy(Joint);
			RigidBody.mass = _startMass;
			if (parent.HasAuthority)
			{
				RigidBody.interpolation = (GameManager.RunSimulation ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
			}
		}
		else
		{
			SetNoInterpolationForClients();
		}
		SetWearVisibility();
		ParentSlot?.Empty();
		ParentSlot = null;
		parent.OnChildExitInventory(this);
		if (force > 0f)
		{
			parent.OnChildThrown(this, force);
		}
		else if (velocity.magnitude <= 0f && force <= 0f)
		{
			parent.OnChildDropped(this);
		}
		OnExitInventory(parent);
		parentSlot.RefreshSlotDisplay();
		if (parentSlot.HidesOccupant)
		{
			SetVisibility(isVisible: true);
		}
		base.WorldGrid = new WorldGrid(CenterPosition);
		base.GridController.AlertGridWatchers(base.WorldGrid, this, GridEvent.GridEventType.Enter);
		HandleCollisionWith(parentSlot.Parent, ignore: false);
		parentSlot.OnExitEvent();
		if (base.isActiveAndEnabled)
		{
			LimitedContinuousCollision().Forget();
		}
		if (!(this is Human))
		{
			LodManager.EnqueueRequesterToUpdate(this);
		}
		return true;
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		if (ParentSlot != null)
		{
			Thing parent = ParentSlot.Parent;
			ConsoleWindow.Print($"ParentSlot: {ParentSlot.DisplayName} ({ParentSlot.Type})");
			ConsoleWindow.Print($"Parent: {parent.DisplayName} #{parent.ReferenceId}");
		}
		else
		{
			ConsoleWindow.Print("ParentSlot: none");
		}
	}

	public virtual bool MoveToWorld(float force = 0f)
	{
		Entity obj = this as Entity;
		_snapNextPosition = true;
		Vector3 velocity = ThingTransform.forward;
		if ((object)obj == null && GameManager.RunSimulation && RootParent is Human human)
		{
			Vector3 vector = (human.AimIk ? human.HelmetSlot.Location.position : RootParent.CenterPosition);
			Vector3 vector2 = (human.AimIk ? human.HelmetSlot.Location.forward : RootParent.ThingTransform.forward);
			velocity = vector2 * 0.5f;
			if (!(this is DraggableThing))
			{
				Quaternion thingTransformRotation = Quaternion.AngleAxis(human.EntityRotation.eulerAngles.y + 90f, Vector3.up);
				Vector3 safeDropPositionWithBoundsCheck = GetSafeDropPositionWithBoundsCheck(vector + vector2 * 0.1f, vector2, 0.5f);
				base.ThingTransformRotation = thingTransformRotation;
				base.ThingTransformPosition = safeDropPositionWithBoundsCheck;
			}
		}
		if (ParentSlot != null)
		{
			HandleCollisionWith(ParentSlot.Parent, ignore: false);
		}
		return MoveToWorld(base.ThingTransformPosition, base.ThingTransformRotation, velocity, Vector3.zero, force);
	}

	private async UniTask LimitedContinuousCollision()
	{
		if (RigidBody.collisionDetectionMode == CollisionDetectionMode.Continuous)
		{
			return;
		}
		_origionalCollisionMode = RigidBody.collisionDetectionMode;
		RigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		float t = 0f;
		while (t < _continuousCollisionTime)
		{
			t += Time.deltaTime;
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		if (!MovingFast)
		{
			RigidBody.collisionDetectionMode = _origionalCollisionMode;
		}
	}

	protected virtual void PhysicsOnRender(bool isRendered)
	{
		if (isRendered)
		{
			ActiveRigidbody.MovePosition(LastPhysicsUpdate.WorldPosition);
			ActiveRigidbody.MoveRotation(LastPhysicsUpdate.WorldRotation.normalized);
			SetPhysics(on: true);
			ActiveRigidbody.WakeUp();
		}
		else
		{
			ActiveRigidbody.Sleep();
			SetPhysics(on: false);
		}
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		if (!GameManager.RunSimulation && !IsChild && GameManager.GameState == GameState.Running && (bool)ActiveRigidbody)
		{
			PhysicsOnRender(isRendered: true);
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		if (!GameManager.RunSimulation && !IsChild && GameManager.GameState == GameState.Running && (bool)ActiveRigidbody)
		{
			PhysicsOnRender(isRendered: false);
		}
	}

	protected void SetNoInterpolationForClients()
	{
		if (!GameManager.RunSimulation && RigidBody.interpolation != RigidbodyInterpolation.None)
		{
			RigidBody.interpolation = RigidbodyInterpolation.None;
		}
	}

	public virtual void OnEnterInventory(Thing parent)
	{
		_slotStateDirty = true;
		_staticParent = parent is Structure;
		SetSoundMixerGroup();
		SetWearVisibility();
		this.OnParentChanged?.Invoke();
		DynamicThing.ParentChangedHashEvent?.Invoke(this);
		if (DynamicThing.ParentChangedHashAndQuantityEvent != null && this is IQuantity quantity)
		{
			DynamicThing.ParentChangedHashAndQuantityEvent(this, quantity.GetQuantity);
		}
		foreach (Slot slot in Slots)
		{
			slot.Occupant?.OnAncestryChangeAsChild();
		}
	}

	public virtual void OnAncestryChangeAsChild()
	{
	}

	public virtual void OnExitInventory(Thing oldParent)
	{
		_staticParent = false;
		_slotStateDirty = true;
		SetSoundMixerGroup();
		SetWearVisibility();
		this.OnParentChanged?.Invoke();
		DynamicThing.ParentChangedHashEvent?.Invoke(this);
		if (DynamicThing.ParentChangedHashAndQuantityEvent != null && this is IQuantity quantity)
		{
			DynamicThing.ParentChangedHashAndQuantityEvent(this, quantity.GetQuantity);
		}
		foreach (Slot slot in Slots)
		{
			slot.Occupant?.OnAncestryChangeAsChild();
		}
	}

	public override void OnChildThrown(DynamicThing childThing, float force)
	{
		base.OnChildThrown(childThing, force);
		Human human = this as Human;
		Vector3 vector = (((bool)human && (bool)human.AimIk) ? (human.AimIk.position - human.BrainSlot.Location.position).normalized : ParentSlot.Parent.ThingTransform.forward);
		if (!RigidBody.useGravity)
		{
			if (!RigidBody.isKinematic)
			{
				RigidBody.velocity -= vector * force / 2f;
			}
			if (!childThing.RigidBody.isKinematic)
			{
				Vector3 vector2 = (HasAuthority ? RigidBody.velocity : LastPhysicsUpdate.Velocity);
				childThing.RigidBody.velocity = vector2 + vector * force;
			}
		}
		else if (!childThing.RigidBody.isKinematic)
		{
			Vector3 velocity = (HasAuthority ? RigidBody.velocity : LastPhysicsUpdate.Velocity) + vector * force + Vector3.up * force / 2f;
			childThing.RigidBody.velocity = velocity;
		}
		Vector3 vector3 = (HasAuthority ? RigidBody.angularVelocity : LastPhysicsUpdate.AngularVelocity);
		childThing.RigidBody.angularVelocity = vector3 + new Vector3(UnityEngine.Random.Range(0f - force, force), UnityEngine.Random.Range(0f - force, force), UnityEngine.Random.Range(0f - force, force));
	}

	public override void OnChildDropped(DynamicThing childThing)
	{
		base.OnChildDropped(childThing);
		if (!childThing.RigidBody.isKinematic)
		{
			Vector3 velocity = (HasAuthority ? RigidBody.velocity : LastPhysicsUpdate.Velocity);
			Vector3 angularVelocity = (HasAuthority ? RigidBody.angularVelocity : LastPhysicsUpdate.AngularVelocity);
			childThing.RigidBody.velocity = velocity;
			childThing.RigidBody.angularVelocity = angularVelocity;
		}
	}

	public virtual void FollowPath(RoomManager.PathfindingTask pathfindingTask)
	{
	}

	public string GetFallbackName()
	{
		return Localization.GetFallbackName(this);
	}

	public int CompareTo(DynamicThing other)
	{
		if ((int)SortingClass > (int)other.SortingClass)
		{
			return 1;
		}
		if ((int)SortingClass < (int)other.SortingClass)
		{
			return -1;
		}
		return string.Compare(GetFallbackName(), other.GetFallbackName(), StringComparison.Ordinal);
	}

	public override bool IsNetworkUpdate()
	{
		bool num = base.IsNetworkUpdate();
		bool num2 = ActiveRigidbody.IsSleeping() && ActiveRigidbody.IsSleeping() == _lastSentValues.IsSleeping;
		bool flag = ParentSlot?.Parent != null && ParentSlot.HidesOccupant;
		_physicsBatchEligible = !ActiveRigidbody.IsSleeping() && !flag;
		if (!num2 && !flag)
		{
			_currentPhysicsValues = default(DynamicThingPosition);
			PrepareUpdateMessage(ref _currentPhysicsValues);
		}
		_currentPhysicsValues.SetUpdateFlags(in _lastSentValues);
		bool flag2 = (_currentPhysicsValues.UpdateFlags & 0x80) != 0;
		bool flag3 = _currentPhysicsValues.UpdateFlags != 0;
		if (!(num || flag2))
		{
			return IsEntity && flag3;
		}
		return true;
	}

	public PhysicsBatchResult BuildPhysicsBatchRecord(uint tick, out DynamicThingPosition record)
	{
		record = _currentPhysicsValues;
		if (!_physicsBatchEligible)
		{
			return PhysicsBatchResult.Ineligible;
		}
		if ((ulong)(tick + base.ReferenceId) % 10uL != 0)
		{
			record.SetUpdateFlags(in _lastBatchValues);
			if (record.UpdateFlags == 0)
			{
				return PhysicsBatchResult.Suppressed;
			}
		}
		_lastBatchValues = record;
		return PhysicsBatchResult.Write;
	}

	public bool TryPeekPhysicsRecord(out DynamicThingPosition record)
	{
		record = _currentPhysicsValues;
		return _physicsBatchEligible;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		_currentPhysicsValues.WriteLite(writer, ref _lastSentValues);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteVector3(DragOffset);
		}
		base.BuildUpdate(writer, networkUpdateType);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		_lastSentValues.ReadLite(reader, this);
		if (FragmentHandler.CurrentApplyTick >= LastPhysicsTick)
		{
			LastPhysicsTick = FragmentHandler.CurrentApplyTick;
			ProcessPhysicsUpdate(_lastSentValues);
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			DragOffset = reader.ReadVector3();
		}
		base.ProcessUpdate(reader, networkUpdateType);
	}

	public override void BuildOwnerUpdate(RocketBinaryWriter writer)
	{
		base.BuildOwnerUpdate(writer);
		_currentOwnerUpdate = default(DynamicThingPosition);
		PrepareUpdateMessage(ref _currentOwnerUpdate);
		_currentOwnerUpdate.SetUpdateFlags(in _lastOwnerUpdate, isPlayer: true);
		_currentOwnerUpdate.WriteLite(writer, ref _lastOwnerUpdate);
	}

	public override void ProcessOwnerUpdate(RocketBinaryReader reader)
	{
		base.ProcessOwnerUpdate(reader);
		_currentOwnerUpdate.ReadLite(reader, this);
		ProcessPhysicsUpdate(_currentOwnerUpdate);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		DynamicThingPosition currentPosition = default(DynamicThingPosition);
		bool flag = ParentSlot != null && ParentSlot.Parent != null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, ParentSlot.Parent);
			writer.WriteUInt16((ushort)ParentSlot.SlotIndex);
			writer.WriteBoolean(Joint);
			writer.WriteVector3(Joint ? DragOffset : Vector3.zero);
			writer.WriteVector3(WorldCenterOfMass);
			writer.WriteVector3(base.Position);
		}
		else
		{
			PrepareUpdateMessage(ref currentPosition);
			currentPosition.Write(writer);
			writer.WriteVector3(((bool)RigidBody && !RigidBody.isKinematic) ? RigidBody.velocity : Vector3.zero);
			writer.WriteVector3(((bool)RigidBody && !RigidBody.isKinematic) ? RigidBody.angularVelocity : Vector3.zero);
		}
	}

	public override void OnFinishedThingSync()
	{
		base.OnFinishedThingSync();
		if (ParentSlot == null)
		{
			LodManager.EnqueueRequesterToUpdate(this);
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			ushort index = reader.ReadUInt16();
			bool flag = reader.ReadBoolean();
			Vector3 offset = reader.ReadVector3();
			WorldCenterOfMass = reader.ReadVector3();
			base.Position = reader.ReadVector3();
			Thing thing = Referencable.Find<Thing>(referenceId);
			if (referenceId != 0L && thing == null)
			{
				ConsoleWindow.PrintError($"error {DisplayName} with #{base.ReferenceId} cant find its parent #{referenceId}");
			}
			else if (flag)
			{
				DragInSlot(thing.Slots[index], offset);
			}
			else
			{
				MoveToSlot(thing.Slots[index], thing);
			}
		}
		else
		{
			_lastSentValues = default(DynamicThingPosition);
			_lastSentValues.Read(reader);
			Vector3 velocity = reader.ReadVector3();
			Vector3 angularVelocity = reader.ReadVector3();
			if ((bool)RigidBody && !RigidBody.isKinematic)
			{
				RigidBody.velocity = velocity;
				RigidBody.angularVelocity = angularVelocity;
			}
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (NetworkManager.IsClient)
		{
			ProcessPhysicsUpdate(_lastSentValues);
		}
	}

	protected override void BuildUpdateTransform(RocketBinaryWriter writer)
	{
		writer.WriteVector3(base.Position);
		writer.WriteQuaternion(Rotation);
		writer.WriteInt64(ParentSlot?.Parent.ReferenceId ?? 0);
		writer.WriteByte((byte)(ParentSlot?.SlotIndex ?? 255));
	}

	protected override void ProcessUpdateTransform(RocketBinaryReader reader)
	{
		Vector3 worldPosition = reader.ReadVector3();
		Quaternion quaternion = reader.ReadQuaternion();
		if (!HasAuthority && ParentSlot == null)
		{
			ThingTransform.SetPositionAndRotation(worldPosition, quaternion);
		}
		long num = reader.ReadInt64();
		byte index = reader.ReadByte();
		if (num > 0)
		{
			Thing thing = Thing.Find(num);
			Slot slot = thing.Slots[index];
			if (slot.Occupant != this)
			{
				slot.Occupant?.MoveToWorld();
			}
			if (thing is Entity)
			{
				if (this is DraggableThing || this is Entity)
				{
					DragInSlot(slot, DragOffset);
					return;
				}
			}
			MoveToSlot(slot, thing);
		}
		else
		{
			MoveToWorld(worldPosition, quaternion, Vector3.zero, Vector3.zero);
		}
	}

	public virtual bool CanBeWeathered()
	{
		if (WeatherDamageScale <= 0f)
		{
			return false;
		}
		if (Room == null && ParentSlot == null && !(this is Human))
		{
			return WeatherManager.DamagingRandom.GetChance(0, 10);
		}
		return false;
	}

	public virtual void DoWeatherDamage(float damageMultiplier)
	{
		DamageState.Damage(ChangeDamageType.Increment, ThingHealth * (WeatherDamageScale * 0.03f * damageMultiplier), DamageUpdateType.Brute);
	}

	public virtual void ApplyLavaDamage()
	{
		DamageState.Damage(ChangeDamageType.Increment, LavaDamage, DamageUpdateType.Burn);
		foreach (Slot slot in Slots)
		{
			if (slot.Contains<DynamicThing>(out var occupant))
			{
				occupant.ApplyLavaDamage();
			}
		}
	}

	public virtual void CheckForCollectorProximity()
	{
	}

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return PrefabName;
	}

	public override void CachePositionOnSpawn()
	{
		base.CachePositionOnSpawn();
		WorldCenterOfMass = (ActiveRigidbody ? ActiveRigidbody.worldCenterOfMass : base.Position);
	}

	public virtual bool ShouldResetPosition()
	{
		if (ParentSlot != null)
		{
			return false;
		}
		return base.Position.y < 0f;
	}
}
