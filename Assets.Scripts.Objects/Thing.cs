using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using DLC;
using Effects;
using Objects;
using Objects.Electrical;
using Objects.Rockets;
using Reagents;
using Sentry;
using Sound;
using Trading;
using Trading.Waypoints;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

public class Thing : MonoBehaviour, IReferencable, IEvaluable, IAudioParent, ISyncListable, ITooltip, IDensePoolable, ICreativeSpawnable
{
	public delegate void Event();

	public delegate void OnMinedOreEvent(Ore ore);

	public delegate void OnMinedOreAmount(Thing thing, float quantity);

	public class DelayedActionInstance
	{
		public float Duration;

		public string OverrideTitle = string.Empty;

		public string ActionMessage = string.Empty;

		public bool IsDisabled;

		public bool SwitchTitleForTooltip;

		public string ExtendedMessage = string.Empty;

		public float Slider = -1f;

		public SelectionInstance Selection;

		public int ActionSoundHash;

		public int ActionCompleteSoundHash;

		private readonly StringBuilder _stateMessageBuilder = new StringBuilder();

		public Color color;

		public string GetExtendedText()
		{
			return ExtendedMessage?.Trim();
		}

		public DelayedActionInstance()
		{
		}

		public DelayedActionInstance(float duration)
		{
			Duration = duration;
		}

		public DelayedActionInstance Fail()
		{
			IsDisabled = true;
			return this;
		}

		public DelayedActionInstance Fail(string failureMessage)
		{
			IsDisabled = true;
			ClearStateMessage();
			AppendStateMessage(failureMessage);
			return this;
		}

		public DelayedActionInstance Fail(Assets.Scripts.Localization2.GameString failureMessage)
		{
			IsDisabled = true;
			ClearStateMessage();
			AppendStateMessage(failureMessage.DisplayString);
			return this;
		}

		public DelayedActionInstance Fail(Assets.Scripts.Localization2.GameString failureMessage, string arg0)
		{
			IsDisabled = true;
			ClearStateMessage();
			AppendStateMessage(failureMessage, arg0);
			return this;
		}

		public DelayedActionInstance Fail(Assets.Scripts.Localization2.GameString failureMessage, string arg0, string arg1)
		{
			IsDisabled = true;
			ClearStateMessage();
			AppendStateMessage(failureMessage, arg0, arg1);
			return this;
		}

		public DelayedActionInstance Succeed()
		{
			IsDisabled = false;
			return this;
		}

		public void ClearStateMessage()
		{
			_stateMessageBuilder.Clear();
		}

		public string GetStateMessage()
		{
			return _stateMessageBuilder.ToString();
		}

		public void AppendStateMessage(string appendString)
		{
			_stateMessageBuilder.AppendLine(appendString);
		}

		public void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString)
		{
			_stateMessageBuilder.AppendLine(gameString.AsString());
		}

		public void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0)
		{
			_stateMessageBuilder.AppendLine(gameString.AsString(arg0));
		}

		public void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0, string arg1)
		{
			_stateMessageBuilder.AppendLine(gameString.AsString(arg0, arg1));
		}

		public void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0, string arg1, string arg2)
		{
			_stateMessageBuilder.AppendLine(gameString.AsString(arg0, arg1, arg2));
		}

		public void AppendStateMessage(Assets.Scripts.Localization2.GameString gameString, string arg0, string arg1, string arg2, string arg3)
		{
			_stateMessageBuilder.AppendLine(gameString.AsString(arg0, arg1, arg2, arg3));
		}

		public static DelayedActionInstance Success(string contextMessage)
		{
			return new DelayedActionInstance
			{
				ActionMessage = contextMessage
			}.Succeed();
		}

		public static DelayedActionInstance Failure(string contextMessage, Assets.Scripts.Localization2.GameString failureMessage)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
			delayedActionInstance.ActionMessage = contextMessage;
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(failureMessage);
			return delayedActionInstance.Fail();
		}

		public static DelayedActionInstance Failure(string contextMessage, Assets.Scripts.Localization2.GameString failureMessage, string arg0)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
			delayedActionInstance.ActionMessage = contextMessage;
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(failureMessage, arg0);
			return delayedActionInstance.Fail();
		}

		public static DelayedActionInstance Failure(string contextMessage, Assets.Scripts.Localization2.GameString failureMessage, string arg0, string arg1)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
			delayedActionInstance.ActionMessage = contextMessage;
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(failureMessage, arg0, arg1);
			return delayedActionInstance.Fail();
		}
	}

	public delegate void ColorEvent(ColorSwatch colorSwatch);

	[SerializeField]
	[ReadOnly]
	private long _referenceId;

	[SerializeField]
	private DLCType _dlcType;

	private readonly DensePoolReference<Thing> _allThingsPool = new DensePoolReference<Thing>(OcclusionManager.AllThings);

	private readonly DensePoolReference<Thing> _updatePool = new DensePoolReference<Thing>(OcclusionManager.UpdatingThings);

	private readonly DensePoolReference<Thing> _atmosphericThingPool = new DensePoolReference<Thing>(AtmosphericsManager.AtmosphericThings);

	private readonly DensePoolReference<IPowered> _allPoweredThingsPool = new DensePoolReference<IPowered>(ElectricityManager.AllPoweredThings);

	private readonly DensePoolReference<ILightActivated> _lightActivatedPool = new DensePoolReference<ILightActivated>(LightManager.AllLightActivated);

	private const int MAX_PHYSICAL = 10000;

	public static readonly DensePool<IPhysical> PhysicalPoolActive = new DensePool<IPhysical>("PhysicalPoolActive", 10000);

	public static List<ILight> AllILights = new List<ILight>();

	public static List<IWearableLight> AllIWearableLights = new List<IWearableLight>();

	[SerializeField]
	private SwitchOnOff switchOnOff;

	[SerializeField]
	private ActivateButton activateButton;

	[SerializeField]
	private LeverAnimationComponent leverAnimationComponent;

	[HideInInspector]
	public bool SuppressSound = true;

	private Dictionary<int, GameAudioEvent> _audioEventLookup = new Dictionary<int, GameAudioEvent>();

	[ReadOnly]
	public Vector3 Forward;

	public bool HideInStationpedia;

	public bool IsEmergency;

	public static Dictionary<int, List<IResourceConsumer>> _resourceLookup = new Dictionary<int, List<IResourceConsumer>>();

	public static int TotalThings;

	public static int TotalThingsToSpawn = -1;

	[Header("Thing")]
	[ReadOnly]
	public string PrefabName;

	[ReadOnly]
	public int PrefabHash;

	private bool _indestructable;

	private Thing _originalPrefab;

	public Transform ThingTransform;

	public Cell Cell;

	public List<Slot> Slots;

	[ReadOnly]
	public Bounds Bounds;

	[ReadOnly]
	public float SurfaceArea;

	[Range(0.001f, 100f)]
	public float SurfaceAreaScale = 1f;

	[ReadOnly]
	public Vector3 BoundsOffset;

	[ReadOnly]
	public bool IgnoreSave;

	public bool generateTerrain = true;

	public bool WillSave = true;

	protected Vector3 position;

	public Quaternion Rotation;

	public bool CanFrustumCull;

	[Tooltip("Amount of heat transfred from item to enviroment")]
	[Range(0f, 1f)]
	public float ThermodynamicsScale = 0.1f;

	[Tooltip("Amount of heat transfred from the solar heat to the item")]
	public float SolarHeatingScale = 0.1f;

	[Tooltip("Scales damage taken during weather events. Values less than 1 reduce damage. Values greater than 1 increase damage.")]
	[Range(0f, 2f)]
	public float WeatherDamageScale = 1f;

	[NonSerialized]
	public ReagentMixture ReagentMixture;

	[Header("Damage")]
	public IndestructableDamageState DamageState;

	[Tooltip("Temperature below which will start taking damage in kelvins")]
	[SerializeField]
	private float shatterTemperature = 1f;

	[Tooltip("Minimum temperature at which this item will burn when sparked in kelvins")]
	[SerializeField]
	[FormerlySerializedAs("FlashpointTemperature")]
	public float flashpointTemperature = -1f;

	[Tooltip("Minimum temperature at which this item will spontaneously burn without a spark in kelvins")]
	[SerializeField]
	[FormerlySerializedAs("AutoignitionTemperature")]
	public float autoignitionTemperature = -1f;

	[Tooltip("How many seconds it will burn for before being totally destroyed")]
	public float BurnTime = 5f;

	public float ThingHealth = 100f;

	public float EnergyReleasedWhenBurned = 100000f;

	private bool _isBurning;

	protected List<CustomColorMapping> _customMaterials;

	[SerializeField]
	[ReadOnly]
	private string _customName;

	[ReadOnly]
	private int _nameHash;

	private ulong _ownerClientId;

	[Header("Thing Colors")]
	[Tooltip("If set, will allow any parts of the thing with this material to be spraypainted")]
	public Material PaintableMaterial;

	[ReadOnly]
	public ColorSwatch CustomColor;

	[Header("Interactables")]
	public List<Interactable> Interactables = new List<Interactable>();

	[Header("Audio Events")]
	[Tooltip("List of prepopulated Audio Events.")]
	public List<GameAudioEvent> AudioEvents = new List<GameAudioEvent>();

	[Header("AudioSources")]
	[Tooltip("List of GameAudioSources.")]
	public List<GameAudioSource> AudioSources = new List<GameAudioSource>();

	[NonSerialized]
	public List<PooledAudioSource> PooledAudioSources = new List<PooledAudioSource>(8);

	[Header("General")]
	[ReadOnly]
	public Animator BaseAnimator;

	public bool IsCursor;

	[Tooltip("Generated blueprint for creation visualizing")]
	public GameObject Blueprint;

	[Tooltip("If not zero, an arrow will point upward at this offset from the root of the blueprint")]
	public Vector3 BlueprintOrientVector;

	[ReadOnly]
	[Tooltip("Configured Wireframe view")]
	public Wireframe Wireframe;

	[ReadOnly]
	public List<LensFlare> lodFlares;

	public bool IsCustomThing;

	private bool _hasBaseAnimator;

	private bool _allowInteraction = true;

	protected bool _hasAtmosphere;

	public static float StressedRatio = 0.8f;

	protected bool _stressed;

	public static string[] DefaultModeStrings = new string[2] { "Mode0", "Mode1" };

	public Vector3 ThumbnailOffset;

	public Quaternion ThumbnailRotation;

	public Sprite Thumbnail;

	public Sprite[] Thumbnails;

	[ReadOnly]
	public bool IsEntity;

	private Entity _asEntity;

	private DynamicThing _asDynamicThing;

	private Structure _asStructure;

	private int _frameButton1Updated;

	private int _button1;

	private int _frameButton2Updated;

	private int _button2;

	private int _frameButton3Updated;

	private int _button3;

	private int _frameErrorUpdated;

	private int _error;

	private int _frameIsLockedUpdated;

	private bool _isLocked;

	private int _frameModeUpdated;

	private int _mode;

	private int _frameActivateUpdated;

	private int _activate;

	private int _frameImportUpdated;

	private int _import;

	private int _frameImport2Updated;

	private int _import2;

	private int _frameExportUpdated;

	private int _export;

	private int _frameColorUpdated;

	private int _color;

	private int _frameAccessUpdated;

	private int _access;

	private int _frameExport2Updated;

	private int _export2;

	private int _frameOnOffUpdated;

	private bool _onOff;

	protected int _framePoweredUpdated;

	protected int _powered;

	private int _frameIsOpenUpdated;

	private bool _isOpen;

	[FormerlySerializedAs("_isRendered")]
	[SerializeField]
	private bool _isOccluded;

	public Event OnPositionUpdate;

	public bool HasRunOnAtmospherics;

	[SerializeField]
	protected MaterialChanger MaterialChanger;

	[SerializeField]
	public List<StateMaterialChanger> AnimComponents = new List<StateMaterialChanger>(5);

	protected Task ErrorAnimTask;

	private static Ore _defaultSlag;

	private ShadowCastingMode _shadowMode = ShadowCastingMode.On;

	private const float RENDER_MAX_DISTANCE = 100f;

	private float SHADOW_MAX_DISTANCE = 50f;

	private bool _renderChangeScheduled;

	protected Vector3 BoundingSphereCenter;

	protected DateTime LastVisibleTime;

	private static readonly float TimeBeforeOcclude = 5f;

	protected float BoundingSphereRadius;

	[NonSerialized]
	public float CurrentCameraDistanceSquared;

	[NonSerialized]
	public float OcclusionTimeout;

	[ReadOnly]
	public List<ThingLight> Lights = new List<ThingLight>();

	[ReadOnly]
	public List<ThingRenderer> Renderers = new List<ThingRenderer>();

	public List<RocketRendererInstance> RocketRenderers = new List<RocketRendererInstance>();

	private float _lastSentEnergyRadiated;

	protected float _energyRadiated;

	private float _lastSentEnergyConvected;

	protected float _energyConvected;

	[ReadOnly]
	public bool HasErrorState;

	[ReadOnly]
	public bool HasPowerState;

	[ReadOnly]
	public bool HasActivateState;

	[ReadOnly]
	public bool HasLockState;

	[ReadOnly]
	public bool HasOnOffState;

	[ReadOnly]
	public bool HasModeState;

	[ReadOnly]
	public bool HasOpenState;

	[ReadOnly]
	public bool HasImportState;

	[ReadOnly]
	public bool HasImport2State;

	[ReadOnly]
	public bool HasExportState;

	[ReadOnly]
	public bool HasExport2State;

	[ReadOnly]
	public bool HasButton1State;

	[ReadOnly]
	public bool HasButton2State;

	[ReadOnly]
	public bool HasButton3State;

	[ReadOnly]
	public bool HasColorState;

	[ReadOnly]
	public bool HasAccessState;

	private Interactable _interactableAccess;

	private Interactable _interactableColor;

	private Interactable _interactableActivate;

	private Interactable _interactableOnOff;

	private Interactable _interactablePowered;

	private Interactable _interactableError;

	private Interactable _interactableMode;

	private Interactable _interactableOpen;

	private Interactable _interactableLock;

	private Interactable _interactableImport;

	private Interactable _interactableImport2;

	private Interactable _interactableExport;

	private Interactable _interactableExport2;

	private Interactable _interactableButton1;

	private Interactable _interactableButton2;

	private Interactable _interactableButton3;

	private Interactable _interactableButton4;

	protected Dictionary<Collider, Interactable> _interactableColliderLookup;

	protected Dictionary<InteractableType, Interactable> _interactableLookup;

	private readonly Dictionary<Collider, Slot> _slotLookup = new Dictionary<Collider, Slot>();

	public static Dictionary<Collider, Thing> _colliderLookup = new Dictionary<Collider, Thing>();

	public List<Collider> _staticColliders = new List<Collider>();

	public List<Collider> _dynamicColliders = new List<Collider>();

	public List<Collider> _selfColliders = new List<Collider>();

	[ReadOnly]
	public bool IsUpdateEachFrame;

	[ReadOnly]
	public bool IsUpdateAudio;

	[ReadOnly]
	public bool IsUpdate100MS;

	[ReadOnly]
	public bool IsUpdate1000MS;

	private static int _lodFlareUpdateIndex = 0;

	[ReadOnly]
	public bool CanPickup = true;

	public bool DestroyChildrenOnDead = true;

	private bool _disableInteractionFromAnimation;

	protected const double MINIMUM_IGNITION_PRESSURE_PROPANE = 1.5;

	public static readonly PressurekPa MinimumIgnitionPressurePropane = new PressurekPa(1.5);

	private const double MINIMUM_AUTOIGNITION_ENERGY = 10000000.0;

	public static readonly MoleEnergy MinimumAutoignitionEnergy = new MoleEnergy(10000000.0);

	public readonly MoleQuantity FireConsumeMoleAmount = new MoleQuantity(0.3);

	private static readonly float ExtinguishDuration_Deprecated = 0.5f;

	public static readonly int DiffuseIndexPropertyID = Shader.PropertyToID("_DiffuseIndex");

	public static readonly int EMISSION_COLOR = Shader.PropertyToID("_EmissionColor");

	public static readonly int SmoothnessIndexPropertyID = Shader.PropertyToID("_SmoothnessIndex");

	[ColorUsage(true, true)]
	public Color EmissionColor = Color.white;

	[Range(0f, 100f)]
	public int DiffuseIndex;

	[Range(0f, 100f)]
	public int SmoothnessIndex;

	public static SyncList<Thing> NewToSend = new SyncList<Thing>(DeserializeNew);

	public static SyncList<DestroyEvent> DestroyToSend = new SyncList<DestroyEvent>(DeserializeDestroy);

	public DLCType DLCType => _dlcType;

	[Obsolete("Use ReferenceId instead")]
	public long netId => ReferenceId;

	public long ReferenceId
	{
		get
		{
			return _referenceId;
		}
		set
		{
			_referenceId = value;
			if (GameManager.RunSimulation && value == 0L)
			{
				SentrySdk.CaptureMessage("Warning: Set ReferenceId of " + DisplayName + " to 0. This should not happen.");
			}
		}
	}

	public static List<Thing> AllLodFlareThings => OcclusionManager.AllLodFlareThings;

	public Thing GetThing => this;

	public SwitchOnOff SwitchOnOff => switchOnOff;

	public ActivateButton ActivateButton => activateButton;

	public LeverAnimationComponent LeverAnimationComponent => leverAnimationComponent;

	public float SqDistanceFromListener
	{
		get
		{
			if (GameManager.IsBatchMode)
			{
				return 0f;
			}
			return Vector3.SqrMagnitude(CameraController.Instance.MainCameraTransform.position - Position);
		}
	}

	public virtual Transform SoundPosition => Transform;

	public virtual bool IsBroken
	{
		get
		{
			if (DamageState != null)
			{
				return DamageState.Total >= DamageState.MaxDamage;
			}
			return false;
		}
	}

	public virtual Human RootParentHuman => null;

	public virtual bool CanIceMelt => true;

	public bool Indestructable
	{
		get
		{
			return _indestructable;
		}
		set
		{
			_indestructable = value;
			if (_indestructable)
			{
				DamageState = new IndestructableDamageState(this, ThingHealth);
			}
			else
			{
				InitializeDamageState();
			}
		}
	}

	public Thing SourcePrefab
	{
		get
		{
			if ((bool)_originalPrefab)
			{
				return _originalPrefab;
			}
			Prefab._allPrefabs.TryGetValue(PrefabHash, out _originalPrefab);
			return _originalPrefab;
		}
	}

	public Thing GetAsThing => this;

	public virtual Transform Transform
	{
		get
		{
			return ThingTransform;
		}
		set
		{
			ThingTransform = value;
		}
	}

	public Vector3 ThingTransformPosition
	{
		get
		{
			return ThingTransform.position;
		}
		set
		{
			ThingTransform.position = value;
			Position = value;
		}
	}

	public Vector3 RegisteredPosition { get; set; }

	public Quaternion RegisteredRotation { get; set; }

	public Vector3 ThingTransformLocalPosition
	{
		get
		{
			return ThingTransform.localPosition;
		}
		set
		{
			ThingTransform.localPosition = value;
		}
	}

	public Quaternion ThingTransformRotation
	{
		get
		{
			return ThingTransform.rotation;
		}
		set
		{
			ThingTransform.rotation = value;
			Rotation = value;
		}
	}

	public Quaternion ThingTransformLocalRotation
	{
		get
		{
			return ThingTransform.localRotation;
		}
		set
		{
			ThingTransform.localRotation = value;
			Rotation = ThingTransform.rotation;
		}
	}

	public Vector3 ThingTransformLocalRotationEuler
	{
		get
		{
			return ThingTransform.localRotation.eulerAngles;
		}
		set
		{
			ThingTransform.localRotation = Quaternion.Euler(value);
			Rotation = ThingTransform.rotation;
		}
	}

	public bool BeingDestroyed { get; set; }

	public Atmosphere InternalAtmosphere { get; set; }

	public virtual Atmosphere ThermalAtmosphere => InternalAtmosphere;

	public virtual bool HasReadableAtmosphere => false;

	public virtual bool PreventStateChange => false;

	public Bounds GetLocalBounds => Bounds;

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public GridController GridController => GridController.World;

	public virtual float ConvectionFactor => ThermodynamicsScale;

	public virtual float RadiationFactor => ThermodynamicsScale;

	public virtual float SolarHeatingFactor => SolarHeatingScale;

	public virtual bool HasReadableReagentMixture => false;

	public virtual ReagentMixture ReadableReagentMixture
	{
		get
		{
			if (!HasReadableReagentMixture)
			{
				return null;
			}
			return ReagentMixture;
		}
	}

	public TemperatureKelvin FlashPointTemperature => new TemperatureKelvin(flashpointTemperature);

	public TemperatureKelvin AutoignitionTemperature => new TemperatureKelvin(autoignitionTemperature);

	public TemperatureKelvin ShatterTemperature => new TemperatureKelvin(shatterTemperature);

	public virtual bool IsLeaking => false;

	[ByteArraySync]
	public bool IsBurning
	{
		get
		{
			return _isBurning;
		}
		set
		{
			if (NetworkManager.IsServer && value != _isBurning)
			{
				NetworkUpdateFlags |= 16;
			}
			_isBurning = value;
			if (!GameManager.RunSimulation)
			{
				if (IsBurning)
				{
					AtmosphericsManager.RegisterBurningThing(this);
				}
				else
				{
					UpdateFlameVisualizer();
				}
			}
		}
	}

	public virtual string CustomName
	{
		get
		{
			return _customName;
		}
		set
		{
			_customName = value;
			_nameHash = Animator.StringToHash(DisplayName);
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 32;
			}
		}
	}

	public ulong OwnerClientId
	{
		get
		{
			return _ownerClientId;
		}
		set
		{
			if (value != _ownerClientId)
			{
				_ownerClientId = value;
				if (this is Human thing)
				{
					Client.Register(thing, _ownerClientId);
				}
			}
		}
	}

	public virtual string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(CustomName))
			{
				return Localization.GetThingName(PrefabName);
			}
			return CustomName;
		}
	}

	public virtual string TrackableName => DisplayName;

	public string SpawnableName => PrefabName;

	public virtual string ProfilerTag => PrefabName;

	public bool HasBaseAnimator
	{
		get
		{
			if (ThreadedManager.IsThread)
			{
				return _hasBaseAnimator;
			}
			return BaseAnimator;
		}
	}

	public bool AllowInteraction
	{
		get
		{
			if (_allowInteraction)
			{
				return !_disableInteractionFromAnimation;
			}
			return false;
		}
		set
		{
			_allowInteraction = value;
		}
	}

	public bool IsAnimating => _disableInteractionFromAnimation;

	public virtual bool HasAtmosphere
	{
		get
		{
			return _hasAtmosphere;
		}
		set
		{
			if (_hasAtmosphere != value)
			{
				_hasAtmosphere = value;
				if (_hasAtmosphere)
				{
					OnAtmosphereGain();
				}
				else
				{
					OnAtmosphereLost();
				}
			}
		}
	}

	public virtual bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			_stressed = value;
		}
	}

	public virtual string[] ModeStrings => DefaultModeStrings;

	public virtual bool HasRoom => false;

	public virtual bool HasAuthority
	{
		get
		{
			if (!GameManager.RunSimulation)
			{
				return ReferenceId == InventoryManager.ParentBrain?.ReferenceId;
			}
			return true;
		}
	}

	public virtual bool OccludeAudio => false;

	public int SpawnId => PrefabHash;

	public GameObject GameObject => base.gameObject;

	public virtual bool AttackWithAllowIncomplete => false;

	public Entity AsEntity => _asEntity;

	public DynamicThing AsDynamicThing => _asDynamicThing;

	public Structure AsStructure => _asStructure;

	public bool IsEntityChild
	{
		get
		{
			if (RootParent != null)
			{
				return RootParent.GetType().IsSubclassOf(typeof(Entity));
			}
			return false;
		}
	}

	public virtual Thing RootParent => this;

	public virtual bool HasSlots
	{
		get
		{
			if (Slots.Count > 0)
			{
				return Slots.FindIndex((Slot s) => s.IsInteractable) >= 0;
			}
			return false;
		}
	}

	public virtual bool HasOccupiedSlot => Slots.FindIndex((Slot s) => s.Occupant) >= 0;

	public virtual bool HasAnySlots
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null)
			{
				return false;
			}
			return slots.Count > 0;
		}
	}

	public virtual bool HasInteractions => Interactables.Count > 0;

	public virtual bool HasKeyInteractions => Interactables.Count((Interactable p) => p.CanKeyInteract && p.Slot == null) > 0;

	public virtual int Button1
	{
		get
		{
			if (HasButton1State && !HasBaseAnimator)
			{
				return InteractButton1.State;
			}
			if (ThreadedManager.IsThread || _frameButton1Updated == Time.frameCount)
			{
				return _button1;
			}
			_frameButton1Updated = Time.frameCount;
			_button1 = (((bool)BaseAnimator && HasButton1State) ? BaseAnimator.GetInteger(Interactable.Button1State) : 0);
			return _button1;
		}
		set
		{
			if (HasButton1State)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.Button1State, value);
				}
				else
				{
					InteractButton1.State = value;
				}
				_button1 = value;
			}
		}
	}

	public virtual int Button2
	{
		get
		{
			if (HasButton2State && !HasBaseAnimator)
			{
				return InteractButton2.State;
			}
			if (ThreadedManager.IsThread || _frameButton2Updated == Time.frameCount)
			{
				return _button2;
			}
			_frameButton2Updated = Time.frameCount;
			_button2 = (((bool)BaseAnimator && HasButton2State) ? BaseAnimator.GetInteger(Interactable.Button2State) : 0);
			return _button2;
		}
		set
		{
			if (HasButton2State)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.Button2State, value);
				}
				else
				{
					InteractButton2.State = value;
				}
				_button2 = value;
			}
		}
	}

	public virtual int Button3
	{
		get
		{
			if (HasButton3State && !HasBaseAnimator)
			{
				return InteractButton3.State;
			}
			if (ThreadedManager.IsThread || _frameButton3Updated == Time.frameCount)
			{
				return _button3;
			}
			_frameButton3Updated = Time.frameCount;
			_button3 = (((bool)BaseAnimator && HasButton3State) ? BaseAnimator.GetInteger(Interactable.Button3State) : 0);
			return _button3;
		}
		set
		{
			if (HasButton3State)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.Button3State, value);
				}
				else
				{
					InteractButton3.State = value;
				}
				_button3 = value;
			}
		}
	}

	public virtual int Error
	{
		get
		{
			if (HasErrorState && !HasBaseAnimator)
			{
				return InteractError.State;
			}
			if (ThreadedManager.IsThread || _frameErrorUpdated == Time.frameCount)
			{
				return _error;
			}
			_frameErrorUpdated = Time.frameCount;
			_error = (((bool)BaseAnimator && HasErrorState) ? BaseAnimator.GetInteger(Interactable.ErrorState) : 0);
			return _error;
		}
		set
		{
			if (HasErrorState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.ErrorState, value);
				}
				else
				{
					InteractError.State = value;
				}
				_error = value;
			}
		}
	}

	public virtual bool IsLocked
	{
		get
		{
			if (HasLockState && !HasBaseAnimator)
			{
				return InteractLock.State == 1;
			}
			if (ThreadedManager.IsThread || _frameIsLockedUpdated == Time.frameCount)
			{
				return _isLocked;
			}
			_frameIsLockedUpdated = Time.frameCount;
			_isLocked = (bool)BaseAnimator && HasLockState && BaseAnimator.GetInteger(Interactable.LockState) == 1;
			return _isLocked;
		}
		set
		{
			if (HasLockState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.LockState, value ? 1 : 0);
				}
				else
				{
					InteractLock.State = (value ? 1 : 0);
				}
				_isLocked = value;
			}
		}
	}

	public virtual int Mode
	{
		get
		{
			if (HasModeState && !HasBaseAnimator)
			{
				return InteractMode?.State ?? 0;
			}
			if (ThreadedManager.IsThread || _frameModeUpdated == Time.frameCount)
			{
				return _mode;
			}
			_mode = (((bool)BaseAnimator && HasModeState) ? BaseAnimator.GetInteger(Interactable.ModeState) : 0);
			_frameModeUpdated = Time.frameCount;
			return _mode;
		}
		set
		{
			if (HasModeState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.ModeState, value);
				}
				else
				{
					InteractMode.State = value;
				}
				_mode = value;
			}
		}
	}

	public virtual int Activate
	{
		get
		{
			if (ReferenceId == 0L)
			{
				return 0;
			}
			if (!HasBaseAnimator && HasActivateState)
			{
				return InteractActivate.State;
			}
			if (ThreadedManager.IsThread || _frameActivateUpdated == Time.frameCount)
			{
				return _activate;
			}
			_activate = (((bool)BaseAnimator && HasActivateState) ? BaseAnimator.GetInteger(Interactable.ActivateState) : 0);
			_frameActivateUpdated = Time.frameCount;
			return _activate;
		}
		set
		{
			if (ReferenceId != 0L && HasActivateState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.ActivateState, value);
				}
				else
				{
					InteractActivate.State = value;
				}
				_activate = value;
			}
		}
	}

	public virtual int Importing
	{
		get
		{
			if (HasImportState && !HasBaseAnimator)
			{
				return InteractImport.State;
			}
			if (ThreadedManager.IsThread || _frameImportUpdated == Time.frameCount)
			{
				return _import;
			}
			_import = (((bool)BaseAnimator && HasImportState) ? BaseAnimator.GetInteger(Interactable.ImportState) : 0);
			_frameImportUpdated = Time.frameCount;
			return _import;
		}
		set
		{
			if ((bool)BaseAnimator && HasImportState)
			{
				if (_import == 0 && value == 1 && BaseAnimator.HasParameter(Interactable.ImportEnteredState))
				{
					BaseAnimator.SetTrigger(Interactable.ImportEnteredState);
				}
				else if (_import == 1 && value == 0 && BaseAnimator.HasParameter(Interactable.ImportExitedState))
				{
					BaseAnimator.SetTrigger(Interactable.ImportExitedState);
				}
				SetIntegerSafe(Interactable.ImportState, value);
				_import = value;
			}
		}
	}

	public virtual int Importing2
	{
		get
		{
			if (HasImport2State && !HasBaseAnimator)
			{
				return InteractImport2.State;
			}
			if (ThreadedManager.IsThread || _frameImport2Updated == Time.frameCount)
			{
				return _import2;
			}
			_import2 = (((bool)BaseAnimator && HasImport2State) ? BaseAnimator.GetInteger(Interactable.Import2State) : 0);
			_frameImport2Updated = Time.frameCount;
			return _import2;
		}
		set
		{
			if ((bool)BaseAnimator && HasImport2State)
			{
				if (_import2 == 0 && value == 1 && BaseAnimator.HasParameter(Interactable.Import2EnteredState))
				{
					BaseAnimator.SetTrigger(Interactable.Import2EnteredState);
				}
				else if (_import2 == 1 && value == 0 && BaseAnimator.HasParameter(Interactable.Import2ExitedState))
				{
					BaseAnimator.SetTrigger(Interactable.Import2ExitedState);
				}
				SetIntegerSafe(Interactable.Import2State, value);
				_import2 = value;
			}
		}
	}

	public virtual int Exporting
	{
		get
		{
			if (HasExportState && !HasBaseAnimator)
			{
				return InteractExport.State;
			}
			if (ThreadedManager.IsThread || _frameExportUpdated == Time.frameCount)
			{
				return _export;
			}
			_export = (((bool)BaseAnimator && HasExportState) ? BaseAnimator.GetInteger(Interactable.ExportState) : 0);
			_frameExportUpdated = Time.frameCount;
			return _export;
		}
		set
		{
			if ((bool)BaseAnimator && HasExportState)
			{
				SetIntegerSafe(Interactable.ExportState, value);
				_export = value;
			}
		}
	}

	public virtual int ColorState
	{
		get
		{
			if (HasColorState && !HasBaseAnimator)
			{
				return InteractColor.State;
			}
			if (ThreadedManager.IsThread || _frameColorUpdated == Time.frameCount)
			{
				return _color;
			}
			_color = (((bool)BaseAnimator && HasColorState) ? BaseAnimator.GetInteger(Interactable.ColorState) : 0);
			_frameColorUpdated = Time.frameCount;
			return _color;
		}
		set
		{
			if (HasColorState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.ColorState, value);
				}
				else
				{
					InteractColor.State = value;
				}
				_color = value;
			}
		}
	}

	public virtual int AccessState
	{
		get
		{
			if (HasAccessState && !HasBaseAnimator)
			{
				return InteractAccess.State;
			}
			if (ThreadedManager.IsThread || _frameAccessUpdated == Time.frameCount)
			{
				return _access;
			}
			_access = (((bool)BaseAnimator && HasAccessState) ? BaseAnimator.GetInteger(Interactable.AccessState) : 0);
			_frameAccessUpdated = Time.frameCount;
			return _access;
		}
		set
		{
			if (HasAccessState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.AccessState, value);
				}
				else
				{
					InteractAccess.State = value;
				}
				_access = value;
			}
		}
	}

	public virtual int Exporting2
	{
		get
		{
			if (HasExport2State && !HasBaseAnimator)
			{
				return InteractExport2.State;
			}
			if (ThreadedManager.IsThread || _frameExport2Updated == Time.frameCount)
			{
				return _export2;
			}
			_export2 = (((bool)BaseAnimator && HasExport2State) ? BaseAnimator.GetInteger(Interactable.Export2State) : 0);
			_frameExport2Updated = Time.frameCount;
			return _export2;
		}
		set
		{
			if ((bool)BaseAnimator && HasExport2State)
			{
				SetIntegerSafe(Interactable.Export2State, value);
				_export2 = value;
			}
		}
	}

	public virtual bool OnOff
	{
		get
		{
			if (HasOnOffState && !HasBaseAnimator)
			{
				return InteractOnOff.State == 1;
			}
			if (ThreadedManager.IsThread || _frameOnOffUpdated == Time.frameCount)
			{
				return _onOff;
			}
			_onOff = (bool)BaseAnimator && HasOnOffState && BaseAnimator.GetInteger(Interactable.OnOffState) == 1;
			_frameOnOffUpdated = Time.frameCount;
			return _onOff;
		}
		set
		{
			if (HasOnOffState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.OnOffState, value ? 1 : 0);
				}
				else
				{
					InteractOnOff.State = (value ? 1 : 0);
				}
				_onOff = value;
			}
		}
	}

	public virtual bool Powered => PoweredValue >= 1;

	public virtual int PoweredValue
	{
		get
		{
			if (HasPowerState && !HasBaseAnimator)
			{
				return InteractPowered.State;
			}
			if (ThreadedManager.IsThread || _framePoweredUpdated == Time.frameCount)
			{
				return _powered;
			}
			_powered = (((bool)BaseAnimator && HasPowerState) ? BaseAnimator.GetInteger(Interactable.PoweredState) : 0);
			_framePoweredUpdated = Time.frameCount;
			return _powered;
		}
		set
		{
			if (HasPowerState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.PoweredState, value);
				}
				else
				{
					InteractPowered.State = value;
				}
				_powered = value;
			}
		}
	}

	public virtual bool IsOpen
	{
		get
		{
			if (HasOpenState && !HasBaseAnimator)
			{
				return InteractOpen.State == 1;
			}
			if (ThreadedManager.IsThread || _frameIsOpenUpdated == Time.frameCount)
			{
				return _isOpen;
			}
			_isOpen = (bool)BaseAnimator && HasOpenState && BaseAnimator.GetInteger(Interactable.OpenState) == 1;
			_frameIsOpenUpdated = Time.frameCount;
			return _isOpen;
		}
		set
		{
			if (HasOpenState)
			{
				if ((bool)BaseAnimator)
				{
					SetIntegerSafe(Interactable.OpenState, value ? 1 : 0);
				}
				else
				{
					InteractOpen.State = (value ? 1 : 0);
				}
				_isOpen = value;
			}
		}
	}

	public virtual bool IsOccluded
	{
		get
		{
			return _isOccluded;
		}
		set
		{
			_isOccluded = value;
			if (!_isOccluded)
			{
				OnStartRender();
			}
			else
			{
				OnStopRender();
			}
		}
	}

	public ushort NetworkUpdateFlags { get; set; }

	public virtual int GetAccess => 0;

	public WorldGrid WorldGrid { get; set; } = WorldGrid.INVALID;

	public virtual Vector3 CenterPosition => Position + Rotation * Bounds.center;

	public virtual Grid3 GridPosition => CenterPosition.ToGrid();

	public virtual float EnergyRadiated
	{
		get
		{
			return _energyRadiated;
		}
		set
		{
			_energyRadiated = value;
			if (NetworkManager.IsServer && !RocketMath.ApproximatelyByRatio(_energyRadiated, _lastSentEnergyRadiated, 0.05f))
			{
				_lastSentEnergyRadiated = _energyRadiated;
				NetworkUpdateFlags |= 128;
			}
		}
	}

	public virtual float EnergyConvected
	{
		get
		{
			return _energyConvected;
		}
		set
		{
			_energyConvected = value;
			if (NetworkManager.IsServer && !RocketMath.ApproximatelyByRatio(_energyConvected, _lastSentEnergyConvected, 0.05f))
			{
				_lastSentEnergyConvected = _energyConvected;
				NetworkUpdateFlags |= 128;
			}
		}
	}

	public Interactable InteractAccess => _interactableAccess;

	public Interactable InteractColor => _interactableColor;

	public Interactable InteractActivate => _interactableActivate;

	public Interactable InteractOnOff => _interactableOnOff;

	public Interactable InteractPowered => _interactablePowered;

	public Interactable InteractError => _interactableError;

	public Interactable InteractMode => _interactableMode;

	public Interactable InteractOpen => _interactableOpen;

	public Interactable InteractLock => _interactableLock;

	public Interactable InteractImport => _interactableImport;

	public Interactable InteractImport2 => _interactableImport2;

	public Interactable InteractExport => _interactableExport;

	public Interactable InteractExport2 => _interactableExport2;

	public Interactable InteractButton1 => _interactableButton1;

	public Interactable InteractButton2 => _interactableButton2;

	public Interactable InteractButton3 => _interactableButton3;

	public Interactable InteractButton4 => _interactableButton4;

	public bool IsInstantiated { get; private set; }

	public virtual float AudioDistanceSquared => 0f;

	public bool IsBeingDestroyed => BeingDestroyed;

	public virtual bool IsBurnable => false;

	public bool IsPaintable
	{
		get
		{
			if (!(PaintableMaterial != null))
			{
				return HasPaintableMaskMaterial;
			}
			return true;
		}
	}

	protected virtual bool HasPaintableMaskMaterial => false;

	public long NetworkId => netId;

	public virtual int TotalSlots => Slots.Count;

	public event Event OnInteractable;

	public event Event OnSlot;

	public event Event OnDestroyed;

	public event Event OnThingCustomName;

	public static event OnMinedOreEvent OnOreMined;

	public static event OnMinedOreAmount OnMinedOreAmountEvent;

	public event Event OnReagentChanged;

	public event ColorEvent OnColorChange;

	public virtual bool OnAddToPool(object densePool, int slot)
	{
		if (_allThingsPool.CanAddToPool(densePool))
		{
			return _allThingsPool.AddToPool(densePool, slot);
		}
		if (_updatePool.CanAddToPool(densePool))
		{
			return _updatePool.AddToPool(densePool, slot);
		}
		if (_atmosphericThingPool.CanAddToPool(densePool))
		{
			return _atmosphericThingPool.AddToPool(densePool, slot);
		}
		if (_allPoweredThingsPool.CanAddToPool(densePool))
		{
			return _allPoweredThingsPool.AddToPool(densePool, slot);
		}
		if (_lightActivatedPool.CanAddToPool(densePool))
		{
			return _lightActivatedPool.AddToPool(densePool, slot);
		}
		return false;
	}

	public virtual void OnRemoveFromPool(object densePool)
	{
		_allThingsPool.OnRemovedFrom(densePool);
		_updatePool.OnRemovedFrom(densePool);
		_atmosphericThingPool.OnRemovedFrom(densePool);
		_allPoweredThingsPool.OnRemovedFrom(densePool);
		_lightActivatedPool.OnRemovedFrom(densePool);
	}

	public static void Register(IPhysical physicalThing)
	{
		PhysicalPoolActive.Add(physicalThing);
	}

	public static void Deregister(IPhysical physicalThing)
	{
		PhysicalPoolActive.Remove(physicalThing);
	}

	private static Thing FindRef(long referenceId)
	{
		if (referenceId == 0L)
		{
			return null;
		}
		if (TryFind(referenceId, out var thing))
		{
			return thing;
		}
		return null;
	}

	public static bool TryFind(long referenceId, out Thing thing)
	{
		thing = Referencable.Find<Thing>(referenceId);
		return thing;
	}

	public static Thing Find(long referenceId)
	{
		return FindRef(referenceId);
	}

	public static T Find<T>(long referenceId)
	{
		if (referenceId == 0L)
		{
			return default(T);
		}
		Thing thing = Find(referenceId);
		if (thing is T)
		{
			return (T)(object)((thing is T) ? thing : null);
		}
		return default(T);
	}

	public static bool TryFind<T>(long referenceId, out T thing) where T : Thing
	{
		thing = null;
		if (referenceId == 0L)
		{
			return false;
		}
		if (!TryFind(referenceId, out var thing2))
		{
			return false;
		}
		if (!(thing2 is T val))
		{
			return false;
		}
		thing = val;
		return true;
	}

	public bool HasChild<T>(int prefabNameHash, out T childItem)
	{
		childItem = default(T);
		foreach (Slot slot in Slots)
		{
			if (slot.IsNotEmpty() && slot.Occupant.PrefabHash == prefabNameHash && slot.Occupant is T val)
			{
				childItem = val;
				return true;
			}
		}
		return false;
	}

	public bool HasChild<T>(int prefabNameHash, int slotKeyHash, out T childItem)
	{
		childItem = default(T);
		foreach (Slot slot in Slots)
		{
			if (slot.StringHash == slotKeyHash && slot.Occupant != null && slot.Occupant.PrefabHash == prefabNameHash && slot.Occupant is T val)
			{
				childItem = val;
				return true;
			}
		}
		return false;
	}

	public static async UniTask<(bool timedOut, T1 thing)> FindAsync<T1>(long referenceId, float timeout = 10f) where T1 : Thing
	{
		return await FindThingInternalAsync<T1>(referenceId).TimeoutWithoutException(TimeSpan.FromSeconds(timeout));
	}

	private static async UniTask<T1> FindThingInternalAsync<T1>(long referenceId) where T1 : Thing
	{
		T1 thing;
		while (!TryFind(referenceId, out thing))
		{
			await UniTask.Delay(500);
		}
		return thing;
	}

	public virtual void OnPrefabLoad()
	{
		CachePrefabBounds();
		CacheAnimComponents();
		IsUpdateEachFrame = IsOverrideMethod("UpdateEachFrame");
		IsUpdateAudio = IsOverrideMethod("UpdateAudio");
		IsUpdate100MS = IsOverrideMethod("Update100MS");
		IsUpdate1000MS = IsOverrideMethod("Update1000MS");
	}

	public virtual void OnAssignedReference()
	{
		HelperHintsManager.Register(this);
	}

	public virtual void PrintDebugInfo(bool verbose = false)
	{
		ConsoleWindow.Print("Name: " + DisplayName);
		ConsoleWindow.Print($"ReferenceId: {ReferenceId}");
		ConsoleWindow.Print($"Position: {position}");
		if (!verbose)
		{
			ConsoleWindow.Print($"Damage State Total: {DamageState.Total}");
			return;
		}
		ConsoleWindow.Print($"Damage State Brute: {DamageState.Brute}");
		ConsoleWindow.Print($"Damage State Burn: {DamageState.Burn}");
		ConsoleWindow.Print($"Damage State Decay: {DamageState.Decay}");
		ConsoleWindow.Print($"Damage State Hydration: {DamageState.Hydration}");
		ConsoleWindow.Print($"Damage State Oxygen: {DamageState.Oxygen}");
		ConsoleWindow.Print($"Damage State Radiation: {DamageState.Radiation}");
		ConsoleWindow.Print($"Damage State Starvation: {DamageState.Starvation}");
		ConsoleWindow.Print($"Damage State Stun: {DamageState.Stun}");
		ConsoleWindow.Print($"Damage State Toxic: {DamageState.Toxic}");
		ConsoleWindow.Print($"Damage State Total: {DamageState.Total}");
	}

	public GameAudioEvent GetAudioEvent(int eventNameHash)
	{
		if (_audioEventLookup.TryGetValue(eventNameHash, out var value))
		{
			return value;
		}
		return null;
	}

	public async UniTask PlaySoundFromThread(int nameHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		await UniTask.SwitchToMainThread();
		PlaySound(nameHash, volumeMultiplier, pitchMultiplier);
	}

	public void PlaySound(int nameHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (!GameManager.IsBatchMode)
		{
			GetAudioEvent(nameHash)?.Trigger(volumeMultiplier, pitchMultiplier);
		}
	}

	public void PlayNetworkSound(int clipsDataNameHash)
	{
		if (GameManager.RunSimulation)
		{
			AudioEvent.Create(this, clipsDataNameHash);
			return;
		}
		NetworkClient.SendToServer(new PlayAudioClipsDataMessage
		{
			ParentId = netId,
			ClipsDataNameHash = clipsDataNameHash
		});
	}

	public PooledAudioSource PlayPooledAudioSound(int clipsDataNameHash, Vector3 localPosition)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		return PlayPooledAudioSound(this, clipsDataNameHash, localPosition);
	}

	public static PooledAudioSource PlayPooledAudioSound(Thing targetThing, int clipsDataNameHash, Vector3 localPosition, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (targetThing == null)
		{
			return null;
		}
		return Singleton<AudioManager>.Instance.PlayAudioClipsData(targetThing, clipsDataNameHash, localPosition, null, volumeMultiplier, pitchMultiplier);
	}

	public static PooledAudioSource PlayPooledAudioSound(ISoundAlert iSoundAlert, ChannelData channelData)
	{
		return Singleton<AudioManager>.Instance.PlayAudioClipsData(iSoundAlert.GetAsThing, AudioManager.Find((SoundAlert)iSoundAlert.SoundAlert)?.NameHash ?? 0, Vector3.zero, channelData, (float)(int)iSoundAlert.SoundVolume / 50f);
	}

	public static PooledAudioSource PlayPooledAudioSound(ISoundAlert iSoundAlert)
	{
		return Singleton<AudioManager>.Instance.PlayAudioClipsData(iSoundAlert.GetAsThing, AudioManager.Find((SoundAlert)iSoundAlert.SoundAlert)?.NameHash ?? 0, Vector3.zero, null, (float)(int)iSoundAlert.SoundVolume / 50f);
	}

	protected void PlayCollisionSound(int clipsDataNameHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (!GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayCollisionSound(this, clipsDataNameHash, volumeMultiplier, pitchMultiplier);
		}
	}

	public void StopSound(int nameHash)
	{
		GetAudioEvent(nameHash)?.Stop();
	}

	public async UniTask StopSoundFromThread(int nameHash)
	{
		await UniTask.SwitchToMainThread();
		StopSound(nameHash);
	}

	public virtual void PlayAnimatorSound(AnimationEvent animationEvent)
	{
		GameAudioEvent audioEvent = GetAudioEvent(Animator.StringToHash(animationEvent.stringParameter));
		if (audioEvent != null)
		{
			float num = Mathf.Clamp01(animationEvent.floatParameter);
			if (num <= float.Epsilon)
			{
				num = 1f;
			}
			audioEvent.Trigger(num);
		}
	}

	public void SetVolumeMultiplier(int eventNameHash, float volume)
	{
		GetAudioEvent(eventNameHash)?.SetVolumeMultiplier(volume);
	}

	public void SetSourceVolume(int eventNameHash, float sourceVolume)
	{
		GetAudioEvent(eventNameHash)?.SetSourceVolume(sourceVolume);
	}

	public void SetPitch(int eventNameHash, float pitch)
	{
		GetAudioEvent(eventNameHash)?.SetPitchMultiplier(pitch);
	}

	public void SetVolumeAndPitch(int eventNameHash, float pitch, float volumeMultiplier)
	{
		GameAudioEvent audioEvent = GetAudioEvent(eventNameHash);
		if (audioEvent != null)
		{
			audioEvent.SetVolumeMultiplier(volumeMultiplier);
			audioEvent.SetPitchMultiplier(pitch);
		}
	}

	public void ResetAudioSourceMixer()
	{
		if (AudioSources == null)
		{
			return;
		}
		foreach (GameAudioSource audioSource in AudioSources)
		{
			audioSource.SetMixerGroup(this);
		}
	}

	public void StopAllAudio(bool immediate = false)
	{
		foreach (GameAudioSource audioSource in AudioSources)
		{
			if (audioSource.enabled)
			{
				audioSource.Stop(immediate);
			}
		}
		for (int num = PooledAudioSources.Count - 1; num >= 0; num--)
		{
			PooledAudioSource pooledAudioSource = PooledAudioSources[num];
			if (!(pooledAudioSource == null))
			{
				pooledAudioSource.Stop(immediate);
			}
		}
	}

	public GameAudioSource GetAudioSource(int channel)
	{
		return AudioSources[channel];
	}

	private void InitAudio()
	{
		if (IsCursor)
		{
			return;
		}
		foreach (GameAudioSource audioSource in AudioSources)
		{
			audioSource.Init(this);
		}
		foreach (GameAudioEvent audioEvent in AudioEvents)
		{
			audioEvent.SetConcurrencyIds();
		}
		foreach (Interactable interactable in Interactables)
		{
			foreach (GameAudioEvent associatedAudioEvent in interactable.AssociatedAudioEvents)
			{
				associatedAudioEvent.SetConcurrencyIds();
			}
		}
	}

	public int SqDistanceFromListenerComparator(Thing other)
	{
		float sqDistanceFromListener = SqDistanceFromListener;
		float sqDistanceFromListener2 = other.SqDistanceFromListener;
		if (sqDistanceFromListener < sqDistanceFromListener2)
		{
			return 1;
		}
		if (sqDistanceFromListener > sqDistanceFromListener2)
		{
			return -1;
		}
		return 0;
	}

	public virtual bool IsSoundLocal()
	{
		Human rootParentHuman = RootParentHuman;
		if (rootParentHuman != null)
		{
			return rootParentHuman.IsLocalPlayer;
		}
		return false;
	}

	public virtual double GasRatio(LogicType logicType)
	{
		return AtmosphereHelper.GasRatio(logicType, InternalAtmosphere);
	}

	public virtual SelectionInstance GetSelection()
	{
		return new SelectionInstance
		{
			Position = ThingTransformPosition,
			Rotation = ThingTransform.rotation,
			Bounds = Bounds,
			ParentThingRefernceId = ReferenceId,
			InteractableId = -1
		};
	}

	public SelectionInstance GetSelection(Collider selectedCollider)
	{
		return new SelectionInstance
		{
			Position = ThingTransformPosition,
			Rotation = ThingTransform.rotation,
			Bounds = selectedCollider.bounds,
			IsWorldMode = true,
			ParentThingRefernceId = ReferenceId,
			InteractableId = -1
		};
	}

	public int GetPrefabHash()
	{
		return PrefabHash;
	}

	public static T Create<T>(string prefabName)
	{
		return Create<T>(Prefab.Find(Animator.StringToHash(prefabName)));
	}

	public static T Create<T>(string prefabName, Vector3 position, Quaternion rotation)
	{
		return Create<T>(Prefab.Find(Animator.StringToHash(prefabName)), position, rotation, 0L);
	}

	public static T Create<T>(int prefabHash)
	{
		return Create<T>(Prefab.Find(prefabHash), Vector3.zero, Quaternion.identity, 0L);
	}

	public static T Create<T>(int prefabHash, Vector3 position, Quaternion rotation, long referenceId = 0L)
	{
		if (Prefab.TryFind(prefabHash, out var thing))
		{
			return Create<T>(thing, position, rotation, referenceId);
		}
		ConsoleWindow.PrintError(string.Format("Could not find prefab with {0}: {1}", "prefabHash", prefabHash));
		return default(T);
	}

	public static T Create<T>(Thing prefab)
	{
		return Create<T>(prefab, Vector3.zero, Quaternion.identity, 0L);
	}

	public static T Create<T>(Thing prefab, Transform transform)
	{
		if (!prefab)
		{
			throw new NullReferenceException();
		}
		if (!(prefab is T))
		{
			throw new InvalidCastException($"{prefab.GetType()} can not be cast to type {typeof(T)}");
		}
		Thing thing = Prefab.Find(prefab.PrefabHash);
		Thing thing2 = UnityEngine.Object.Instantiate(thing, transform);
		thing2.name = thing.name;
		thing2.PrefabName = thing.name;
		thing2.SetPrefab(thing);
		thing2.OnStartRender();
		if (!thing2.IsCursor)
		{
			Referencable.RegisterNew(thing2);
			if (GameManager.RunSimulation && !thing2.IsCursor)
			{
				thing2.InitInternalAtmosphere();
			}
			if (NetworkManager.IsServer && NetworkBase.Clients.Count > 0)
			{
				NewToSend.Add(thing2);
			}
			OcclusionManager.Register(thing2);
		}
		if (thing2 is T)
		{
			return (T)(object)((thing2 is T) ? thing2 : null);
		}
		throw new NullReferenceException();
	}

	public static T Create<T>(Thing prefab, Vector3 worldPosition, Quaternion worldRotation, long referenceId = 0L)
	{
		if (prefab == null)
		{
			throw new NullReferenceException("Parameter prefab is null");
		}
		if (!(prefab is T))
		{
			throw new InvalidCastException($"{prefab.GetType()} can not be cast to type {typeof(T)}");
		}
		Thing thing = Prefab.Find(prefab.PrefabHash);
		Thing thing2 = UnityEngine.Object.Instantiate(thing, worldPosition, worldRotation);
		thing2.name = thing.name;
		thing2.PrefabName = thing.name;
		thing2.SetPrefab(thing);
		thing2.OnStartRender();
		if (!thing2.IsCursor)
		{
			bool flag = false;
			if (referenceId == 0L)
			{
				flag = Referencable.RegisterNew(thing2);
				if (flag)
				{
					if (GameManager.RunSimulation)
					{
						thing2.InitInternalAtmosphere();
					}
					if (NetworkManager.IsServer && NetworkBase.Clients.Count > 0)
					{
						if (thing2 is LanderCapsule)
						{
							NewToSend.AddToFront(thing2);
						}
						else
						{
							NewToSend.Add(thing2);
						}
					}
				}
			}
			else
			{
				flag = Referencable.RegisterAs(thing2, referenceId);
			}
			if (flag)
			{
				OcclusionManager.Register(thing2);
			}
		}
		if (thing2 is T)
		{
			return (T)(object)((thing2 is T) ? thing2 : null);
		}
		throw new NullReferenceException();
	}

	public static void AddToLookup(IResourceConsumer resourceConsumer)
	{
		foreach (Item item in resourceConsumer.GetResourcesUsed())
		{
			_resourceLookup.TryGetValue(item.GetPrefabHash(), out var value);
			if (value == null)
			{
				value = new List<IResourceConsumer>();
				_resourceLookup.Add(item.GetPrefabHash(), value);
			}
			value.Add(resourceConsumer);
		}
	}

	public static List<IResourceConsumer> GetResourceConsumers(Thing prefab)
	{
		_resourceLookup.TryGetValue(prefab.GetPrefabHash(), out var value);
		return value;
	}

	public virtual void OnAllPrefabsLoaded()
	{
	}

	public static void ListDynamicPrefabs()
	{
	}

	public virtual void SetPrefab(Thing prefab)
	{
		_originalPrefab = prefab;
		prefab.CheckBounds();
		Bounds = prefab.Bounds;
	}

	public virtual void CheckBounds()
	{
		if (Bounds.extents.magnitude <= float.Epsilon)
		{
			CachePrefabBounds();
		}
	}

	public string ToTooltip()
	{
		return "<color=green>" + DisplayName + "</color>";
	}

	public void ToTooltip(StringBuilder sb)
	{
		sb.Append("<color=green>");
		sb.Append(DisplayName);
		sb.Append("</color>");
	}

	public string ToStationpediaLink()
	{
		return Localization.ParseHelpText("{THING:" + PrefabName + "}");
	}

	public string GetPrefabName()
	{
		return PrefabName;
	}

	public IndestructableDamageState GetDamageState()
	{
		return DamageState;
	}

	public int GetNameHash()
	{
		return _nameHash;
	}

	public static void RenameThing(long targetId, string newName)
	{
		Thing thing = Find(targetId);
		thing.CustomName = newName;
		thing.OnRenamed();
		thing.OnThingCustomNameInvoke();
	}

	public void RenameThing(string newName)
	{
		RenameThing(ReferenceId, newName);
	}

	public void SetSoundMixerGroup()
	{
		foreach (GameAudioEvent audioEvent in AudioEvents)
		{
			audioEvent.AudioSource.SetMixerGroup(this);
		}
	}

	public virtual void OnAtmosphereGain()
	{
		SetSoundMixerGroup();
	}

	public virtual void OnAtmosphereLost()
	{
		SetSoundMixerGroup();
	}

	public Sprite GetThumbnail()
	{
		if (!HasPaintableMaskMaterial && (PaintableMaterial == null || CustomColor == null))
		{
			return Thumbnail;
		}
		int colorIndex = GameManager.GetColorIndex(CustomColor);
		if (Thumbnails == null || Thumbnails.Length <= colorIndex || colorIndex < 0)
		{
			if ((bool)Thumbnail)
			{
				return Thumbnail;
			}
			return null;
		}
		if (Thumbnails == null || Thumbnails.Length <= colorIndex)
		{
			return null;
		}
		return Thumbnails[colorIndex];
	}

	public static Sprite GetThumbnail(Thing prefab, int colorIndex)
	{
		if (prefab?.Thumbnails != null && prefab.Thumbnails.Length > colorIndex && colorIndex > 0)
		{
			return prefab.Thumbnails[colorIndex];
		}
		if (!(prefab?.Thumbnail != null))
		{
			return null;
		}
		return prefab.Thumbnail;
	}

	public virtual string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.Other;
	}

	public virtual string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Other);
	}

	public static bool ThingHasThumbnailVariations(Thing thing)
	{
		if (!thing.HasPaintableMaskMaterial)
		{
			if (thing.PaintableMaterial != null)
			{
				if ((bool)thing.BaseAnimator)
				{
					return !thing.BaseAnimator.HasParameter("Color");
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public virtual void OnAtmosphereClient()
	{
	}

	public virtual bool ExistsInHierarchy(Thing otherThing)
	{
		return false;
	}

	public virtual void OnChildBatteryCellChange(BatteryCell batteryCell)
	{
	}

	public DirtCanister FindAvailableDirtCanister()
	{
		foreach (Slot slot in RootParent.Slots)
		{
			if (slot.Occupant is MiningBelt miningBelt)
			{
				bool flag = miningBelt.SlotType == slot.Type;
				if (miningBelt.ContainsUnfilledDirtCanister(out var dirtCanister) && flag)
				{
					return dirtCanister;
				}
			}
		}
		return null;
	}

	public bool IsState(InteractableType type, int value)
	{
		switch (type)
		{
		case InteractableType.Open:
			if (IsOpen == (value == 1))
			{
				return true;
			}
			break;
		case InteractableType.OnOff:
			if (OnOff == (value == 1))
			{
				return true;
			}
			break;
		case InteractableType.Mode:
			if (Mode == value)
			{
				return true;
			}
			break;
		case InteractableType.Lock:
			if (IsLocked == (value == 1))
			{
				return true;
			}
			break;
		case InteractableType.Import:
			if (Importing == value)
			{
				return true;
			}
			break;
		case InteractableType.Import2:
			if (Importing2 == value)
			{
				return true;
			}
			break;
		case InteractableType.Export:
			if (Exporting == value)
			{
				return true;
			}
			break;
		case InteractableType.Activate:
			if (Activate == value)
			{
				return true;
			}
			break;
		case InteractableType.Powered:
			if (Powered == value >= 1)
			{
				return true;
			}
			break;
		case InteractableType.Error:
			if (Error == value)
			{
				return true;
			}
			break;
		case InteractableType.Export2:
			if (Exporting2 == value)
			{
				return true;
			}
			break;
		case InteractableType.Color:
			if (Exporting2 == value)
			{
				return true;
			}
			break;
		case InteractableType.Button1:
			if (Button1 == value)
			{
				return true;
			}
			break;
		case InteractableType.Button2:
			if (Button2 == value)
			{
				return true;
			}
			break;
		case InteractableType.Button3:
			if (Button3 == value)
			{
				return true;
			}
			break;
		}
		return false;
	}

	private void OnEnable()
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		if ((bool)BaseAnimator && HasLockState)
		{
			SetIntegerSafe(Interactable.LockState, _isLocked ? 1 : 0);
		}
		if ((bool)BaseAnimator && HasModeState)
		{
			SetIntegerSafe(Interactable.ModeState, _mode);
		}
		if ((bool)BaseAnimator && HasActivateState)
		{
			SetIntegerSafe(Interactable.ActivateState, _activate);
		}
		if ((bool)BaseAnimator && HasImportState)
		{
			SetIntegerSafe(Interactable.ImportState, _import);
		}
		if ((bool)BaseAnimator && HasImport2State)
		{
			SetIntegerSafe(Interactable.Import2State, _import2);
		}
		if ((bool)BaseAnimator && HasExportState)
		{
			SetIntegerSafe(Interactable.ExportState, _export);
		}
		if ((bool)BaseAnimator && HasOnOffState)
		{
			SetIntegerSafe(Interactable.OnOffState, _onOff ? 1 : 0);
		}
		if ((bool)BaseAnimator && HasOpenState)
		{
			SetIntegerSafe(Interactable.OpenState, _isOpen ? 1 : 0);
		}
		if ((bool)BaseAnimator && HasPowerState)
		{
			SetIntegerSafe(Interactable.PoweredState, _powered);
		}
		if ((bool)BaseAnimator && HasErrorState)
		{
			SetIntegerSafe(Interactable.ErrorState, _error);
		}
		if ((bool)BaseAnimator && HasButton1State)
		{
			SetIntegerSafe(Interactable.Button1State, _button1);
		}
		if ((bool)BaseAnimator && HasButton2State)
		{
			SetIntegerSafe(Interactable.Button2State, _button2);
		}
		if ((bool)BaseAnimator && HasButton3State)
		{
			SetIntegerSafe(Interactable.Button3State, _button3);
		}
		if ((bool)BaseAnimator && HasColorState)
		{
			SetIntegerSafe(Interactable.ColorState, _color);
		}
		if ((bool)BaseAnimator && HasAccessState)
		{
			SetIntegerSafe(Interactable.AccessState, _access);
		}
		foreach (Interactable interactable in Interactables)
		{
			interactable.SetState();
		}
	}

	public void CacheAllAnimatorInteractableVariables()
	{
		if (GameManager.GameState == GameState.Running && _hasBaseAnimator)
		{
			IsLocked = HasLockState && BaseAnimator.GetInteger(Interactable.LockState) == 1;
			Mode = (HasModeState ? BaseAnimator.GetInteger(Interactable.ModeState) : 0);
			Activate = (HasActivateState ? BaseAnimator.GetInteger(Interactable.ActivateState) : 0);
			Importing = (HasImportState ? BaseAnimator.GetInteger(Interactable.ImportState) : 0);
			Importing2 = (HasImport2State ? BaseAnimator.GetInteger(Interactable.Import2State) : 0);
			Exporting = (HasExportState ? BaseAnimator.GetInteger(Interactable.ExportState) : 0);
			OnOff = HasOnOffState && BaseAnimator.GetInteger(Interactable.OnOffState) == 1;
			IsOpen = HasOpenState && BaseAnimator.GetInteger(Interactable.OpenState) == 1;
			PoweredValue = (HasPowerState ? BaseAnimator.GetInteger(Interactable.PoweredState) : 0);
			Error = (HasErrorState ? BaseAnimator.GetInteger(Interactable.ErrorState) : 0);
			if (HasButton1State && InteractButton1.JoinInProgressSync)
			{
				Button1 = BaseAnimator.GetInteger(Interactable.Button1State);
			}
			if (HasButton2State && InteractButton2.JoinInProgressSync)
			{
				Button2 = BaseAnimator.GetInteger(Interactable.Button2State);
			}
			if (HasButton3State && InteractButton3.JoinInProgressSync)
			{
				Button3 = BaseAnimator.GetInteger(Interactable.Button3State);
			}
			Exporting2 = (HasExport2State ? BaseAnimator.GetInteger(Interactable.Export2State) : 0);
			ColorState = (HasColorState ? BaseAnimator.GetInteger(Interactable.ColorState) : 0);
			AccessState = (HasAccessState ? BaseAnimator.GetInteger(Interactable.AccessState) : 0);
		}
	}

	public void CacheAnimatorInteractableVariable(InteractableType interactableAction)
	{
		if (_hasBaseAnimator)
		{
			switch (interactableAction)
			{
			case InteractableType.Lock:
				IsLocked = HasLockState && BaseAnimator.GetInteger(Interactable.LockState) == 1;
				break;
			case InteractableType.Mode:
				Mode = (HasModeState ? BaseAnimator.GetInteger(Interactable.ModeState) : 0);
				break;
			case InteractableType.Activate:
				Activate = (HasActivateState ? BaseAnimator.GetInteger(Interactable.ActivateState) : 0);
				break;
			case InteractableType.Import:
				Importing = (HasImportState ? BaseAnimator.GetInteger(Interactable.ImportState) : 0);
				break;
			case InteractableType.Import2:
				Importing2 = (HasImport2State ? BaseAnimator.GetInteger(Interactable.Import2State) : 0);
				break;
			case InteractableType.Export:
				Exporting = (HasExportState ? BaseAnimator.GetInteger(Interactable.ExportState) : 0);
				break;
			case InteractableType.Export2:
				Exporting2 = (HasExport2State ? BaseAnimator.GetInteger(Interactable.Export2State) : 0);
				break;
			case InteractableType.OnOff:
				OnOff = HasOnOffState && BaseAnimator.GetInteger(Interactable.OnOffState) == 1;
				break;
			case InteractableType.Open:
				IsOpen = HasOpenState && BaseAnimator.GetInteger(Interactable.OpenState) == 1;
				break;
			case InteractableType.Powered:
				PoweredValue = (HasPowerState ? BaseAnimator.GetInteger(Interactable.PoweredState) : 0);
				break;
			case InteractableType.Error:
				Error = (HasErrorState ? BaseAnimator.GetInteger(Interactable.ErrorState) : 0);
				break;
			case InteractableType.Button1:
				Button1 = (HasButton1State ? BaseAnimator.GetInteger(Interactable.Button1State) : 0);
				break;
			case InteractableType.Button2:
				Button2 = (HasButton2State ? BaseAnimator.GetInteger(Interactable.Button2State) : 0);
				break;
			case InteractableType.Button3:
				Button3 = (HasButton3State ? BaseAnimator.GetInteger(Interactable.Button3State) : 0);
				break;
			case InteractableType.Color:
				ColorState = (HasColorState ? BaseAnimator.GetInteger(Interactable.ColorState) : 0);
				break;
			case InteractableType.Access:
				AccessState = (HasAccessState ? BaseAnimator.GetInteger(Interactable.AccessState) : 0);
				break;
			}
		}
	}

	private void SetIntegerSafe(int stateId, int value)
	{
		if ((object)BaseAnimator != null && BaseAnimator.HasParameter(stateId))
		{
			BaseAnimator.SetInteger(stateId, value);
		}
	}

	public virtual void OnAtmosphericsBegin()
	{
	}

	public virtual void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		if ((bool)BaseAnimator)
		{
			SetIntegerSafe(interactable.PropertyId, newState);
		}
	}

	public virtual void OnInteractableUpdated(Interactable interactable)
	{
		CacheAnimatorInteractableVariable(interactable.Action);
		RefreshAnimState(GameManager.GameState != GameState.Running);
		this.OnInteractable?.Invoke();
	}

	public virtual void ValidateOnLoad(int currentSaveVersion)
	{
	}

	public virtual void OnFinishedInteractionSync(Interactable interactable)
	{
		CacheAllAnimatorInteractableVariables();
	}

	public virtual void OnFinishedThingSync()
	{
		SuppressSound = false;
		RefreshAnimState(skipAnimation: true);
		if (this is IRotatable rotatable)
		{
			rotatable.RotatableBehaviour?.OnClientStart();
		}
	}

	public virtual void OnFinishedLoad()
	{
		if (!IsCursor)
		{
			if (InternalAtmosphere == null && GameManager.RunSimulation)
			{
				InitInternalAtmosphere();
			}
			if (!GameManager.RunSimulation)
			{
				RefreshAnimState(skipAnimation: true);
			}
			if (this is IRotatable rotatable)
			{
				rotatable.RotatableBehaviour?.OnClientStart();
			}
			SuppressSound = false;
		}
	}

	public virtual bool IsAuthorized(Thing thing)
	{
		if (!HasAccessState || !thing)
		{
			return true;
		}
		if (AccessState == 0)
		{
			return true;
		}
		if (thing.GetAccess == 0)
		{
			return false;
		}
		return HasAccess(thing.GetAccess);
	}

	public virtual bool HasAccess(int accessFlag)
	{
		if (!HasAccessState)
		{
			return true;
		}
		return (AccessState & accessFlag) != 0;
	}

	public virtual void GiveAccess(int accessFlag)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(InteractAccess, AccessState | accessFlag);
			return;
		}
		NetworkClient.SendToServer(new AccessChangeFromClient
		{
			Access = accessFlag,
			Operation = AccessChange.Add,
			ThingId = netId
		});
	}

	public virtual void RemoveAccess(int accessFlag)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(InteractAccess, AccessState & ~accessFlag);
			return;
		}
		NetworkClient.SendToServer(new AccessChangeFromClient
		{
			Access = accessFlag,
			Operation = AccessChange.Remove,
			ThingId = netId
		});
	}

	private void CacheAnimComponents()
	{
		AnimComponents = GetComponentsInChildren<StateMaterialChanger>(includeInactive: true).ToList();
	}

	public virtual void RefreshOnOffAnimState(bool skipAnimation = false)
	{
		if (!(MaterialChanger != null))
		{
			return;
		}
		if (Error == 1 && OnOff && Powered)
		{
			if (ErrorAnimTask == null || ErrorAnimTask.IsCompleted)
			{
				ErrorAnimTask = ErrorAnim().AsTask();
			}
		}
		else if (OnOff)
		{
			MaterialChanger.ChangeState(Powered ? Defines.Animator.OnPowered : Defines.Animator.On);
		}
		else
		{
			MaterialChanger.ChangeState(Defines.Animator.Off);
		}
	}

	protected virtual void RefreshAnimState(bool skipAnimation = false)
	{
		if (IsCursor)
		{
			return;
		}
		if (MaterialChanger != null)
		{
			RefreshOnOffAnimState(skipAnimation);
		}
		if (SwitchOnOff != null)
		{
			SwitchOnOff.RefreshState(skipAnimation);
		}
		if (ActivateButton != null)
		{
			ActivateButton.RefreshState(skipAnimation);
		}
		if (LeverAnimationComponent != null)
		{
			LeverAnimationComponent.RefreshState(skipAnimation);
		}
		foreach (StateMaterialChanger animComponent in AnimComponents)
		{
			animComponent.RefreshState(skipAnimation);
		}
	}

	protected virtual async UniTask ErrorAnim()
	{
		while (Error == 1 && OnOff && Powered)
		{
			MaterialChanger.ChangeState(Defines.Animator.Error0);
			await UniTask.Delay(250);
			if (Error != 1 || !OnOff || !Powered)
			{
				break;
			}
			MaterialChanger.ChangeState(Defines.Animator.Error1);
			await UniTask.Delay(250);
		}
		ErrorAnimTask = null;
	}

	public virtual void Explode()
	{
	}

	public virtual void Explosion(Vector3 position, float force = 0f)
	{
	}

	public virtual string GetContextualName(Interactable interactable)
	{
		switch (interactable.Action)
		{
		case InteractableType.Open:
			if (!IsOpen)
			{
				return ActionStrings.Open;
			}
			return ActionStrings.Close;
		case InteractableType.OnOff:
			if (!OnOff)
			{
				return ActionStrings.On;
			}
			return ActionStrings.Off;
		case InteractableType.Activate:
			return (Activate == 0) ? GameStrings.Activate : GameStrings.Deactivate;
		case InteractableType.Mode:
		{
			string arg = ((interactable.State == 0) ? ModeStrings[1] : ModeStrings[0]);
			return GameStrings.InteractableAction.AsString(arg);
		}
		default:
			return interactable.DisplayName;
		}
	}

	public virtual void ReactWithTemperature()
	{
	}

	public virtual PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		return new PassiveTooltip(true);
	}

	public virtual PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName);
	}

	public virtual void Start()
	{
		if (!IsCursor)
		{
			TotalThings++;
		}
		LoadingThings();
	}

	public static void LoadingThings(bool forceToJoin = false)
	{
		if (GameManager.GameState == GameState.Joining && !GameManager.RunSimulation && ((TotalThings >= TotalThingsToSpawn && TotalThingsToSpawn > 0) || forceToJoin))
		{
			GameManager.GameState = GameState.Waiting;
		}
	}

	public virtual void OnRegistered(Cell cell)
	{
		Cell = cell;
		Position = ThingTransformPosition;
		Rotation = ThingTransformRotation;
		RegisteredPosition = Position;
		RegisteredRotation = Rotation;
		WorldGrid = new WorldGrid(this);
		RenderChange(setRenderer: true, this).Forget();
		RefreshAnimState(skipAnimation: true);
		if (RoomManager.ContributingPrefabs.Contains(PrefabHash))
		{
			RoomManager.AddRoomContributor(WorldGrid, this);
		}
		_nameHash = Animator.StringToHash(DisplayName);
	}

	public virtual void OnDeregistered()
	{
		if (RoomManager.ContributingPrefabs.Contains(PrefabHash))
		{
			RoomManager.RemoveRoomContributor(WorldGrid, this);
		}
	}

	private Ingot CreateReagent(Ingot prefab, int quantity, Vector3 spawnPosition)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Ingot ingot = Create<Ingot>(prefab, spawnPosition, Quaternion.identity, 0L);
		ingot.ParentSlot = null;
		ingot.Quantity = quantity;
		return ingot;
	}

	private Ore CreateReagent(Ore prefab, int quantity, Vector3 spawnPosition)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Ore ore = Create<Ore>(prefab, spawnPosition, Quaternion.identity, 0L);
		ore.SetQuantity(quantity);
		return ore;
	}

	private Ingot CreateReagent(Ingot prefab, int quantity, Slot slot)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Ingot ingot = Create<Ingot>(prefab, slot.Location);
		ingot.Quantity = quantity;
		OnServer.MoveToSlotOrWorld(ingot, slot);
		return ingot;
	}

	private Ore CreateReagent(Ore prefab, int quantity, Slot slot)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Ore ore = Create<Ore>(prefab, slot.Location);
		ore.SetQuantity(quantity);
		OnServer.MoveToSlotOrWorld(ore, slot);
		return ore;
	}

	public Item DropReagent(bool forceOre = false)
	{
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			ReagentMixture.Clear();
			return null;
		}
		if (_defaultSlag == null)
		{
			_defaultSlag = Prefab.Find<Ore>("ItemReagentMix");
		}
		if (forceOre)
		{
			ReagentMixture ratioMixture = ReagentMixture.GetRatioMixture();
			ReagentMixture reagentMixture = new ReagentMixture(ratioMixture) * Math.Min(ReagentMixture.TotalReagents, _defaultSlag.MaxQuantity);
			int num = (int)reagentMixture.TotalReagents;
			if (num <= 0)
			{
				ReagentMixture.Clear();
				return null;
			}
			ReagentMixture.Subtract(reagentMixture);
			return Ore.CreateOreType(_defaultSlag, CenterPosition + UnityEngine.Random.insideUnitSphere, ratioMixture, num);
		}
		ReagentMixture reagentMixture2 = new ReagentMixture();
		int num2 = ReagentMixture.AddNextReagent(reagentMixture2, 500);
		if (num2 <= 0)
		{
			ReagentMixture.Clear();
			return null;
		}
		Ingot.RecipeComparable.Recipes.TryGetValue(new Recipe(reagentMixture2, null), out var value);
		if (value == null)
		{
			return CreateReagent(_defaultSlag, num2, CenterPosition);
		}
		Ingot ingot = CreateReagent(value, num2, CenterPosition);
		ingot.CreatedReagentMixture = reagentMixture2;
		return ingot;
	}

	public Item DropReagent(Slot slot, bool forceOre = false)
	{
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			ReagentMixture.Clear();
			return null;
		}
		if (_defaultSlag == null)
		{
			_defaultSlag = Prefab.Find<Ore>("ItemReagentMix");
		}
		if (forceOre)
		{
			ReagentMixture ratioMixture = ReagentMixture.GetRatioMixture();
			ReagentMixture reagentMixture = new ReagentMixture(ratioMixture) * Math.Min(ReagentMixture.TotalReagents, _defaultSlag.MaxQuantity);
			int num = (int)reagentMixture.TotalReagents;
			if (num <= 0)
			{
				ReagentMixture.Clear();
				return null;
			}
			ReagentMixture.Subtract(reagentMixture);
			return Ore.CreateOreType(_defaultSlag, slot, ratioMixture, num);
		}
		ReagentMixture reagentMixture2 = new ReagentMixture();
		int num2 = ReagentMixture.AddNextReagent(reagentMixture2, 500);
		if (num2 <= 0)
		{
			ReagentMixture.Clear();
			return null;
		}
		Ingot.RecipeComparable.Recipes.TryGetValue(new Recipe(reagentMixture2, null), out var value);
		if (value == null)
		{
			return CreateReagent(_defaultSlag, num2, slot);
		}
		Ingot ingot = CreateReagent(value, num2, slot);
		ingot.CreatedReagentMixture = reagentMixture2;
		return ingot;
	}

	public Item DropReagent(Reagent reagentType, Slot slot)
	{
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			ReagentMixture.Clear();
			return null;
		}
		Reagent reagent = ReagentMixture.Take(reagentType, 500.0);
		if (reagent == null)
		{
			return null;
		}
		ReagentMixture reagentMixture = new ReagentMixture();
		reagentMixture.Add(reagent);
		int quantity = (int)reagent.Quantity;
		reagentMixture = reagentMixture.Normalize();
		Ingot.RecipeComparable.Recipes.TryGetValue(new Recipe(reagentMixture, null), out var value);
		if (value == null)
		{
			return CreateReagent(_defaultSlag, quantity, slot);
		}
		Ingot ingot = CreateReagent(value, quantity, slot);
		ingot.CreatedReagentMixture = reagentMixture;
		return ingot;
	}

	public virtual void OnStartRender()
	{
		if (this is IThingBatched batchRendered)
		{
			BatchRenderer.Add(batchRendered);
			foreach (ThingRenderer renderer in Renderers)
			{
				if (renderer != null && renderer.HasRenderer())
				{
					renderer.Visible = false;
				}
			}
		}
		else
		{
			foreach (ThingRenderer renderer2 in Renderers)
			{
				if (renderer2 != null && renderer2.HasRenderer())
				{
					renderer2.Visible = !AsDynamicThing || !AsDynamicThing.IsHiddenInSlot;
				}
			}
		}
		foreach (ThingLight light in Lights)
		{
			light.Refresh();
		}
	}

	public virtual void OnStopRender()
	{
		if (this is IBatchRendered batchRendered)
		{
			BatchRenderer.Remove(batchRendered);
		}
		else
		{
			foreach (ThingRenderer renderer in Renderers)
			{
				if (renderer != null && renderer.HasRenderer())
				{
					renderer.Visible = false;
				}
			}
		}
		foreach (ThingLight light in Lights)
		{
			light.Refresh();
		}
	}

	protected virtual float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected virtual float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(SHADOW_MAX_DISTANCE * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	private async UniTaskVoid OverrideShadows()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (IsBeingDestroyed)
		{
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested || BeingDestroyed)
		{
			return;
		}
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.OverrideShadowMode(_shadowMode);
		}
	}

	public static async UniTaskVoid RenderChange(bool setRenderer, Thing thing)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if ((object)thing != null && !thing.IsBeingDestroyed)
		{
			CancellationToken cancelToken = thing.GetCancellationTokenOnDestroy();
			await UniTask.NextFrame(cancelToken);
			if (!cancelToken.IsCancellationRequested && !(thing?.BeingDestroyed ?? true))
			{
				thing.IsOccluded = !setRenderer;
				thing._renderChangeScheduled = false;
			}
		}
	}

	public virtual void SetOcclusion()
	{
		if (_renderChangeScheduled || IsBeingDestroyed || (object)InventoryManager.Parent == null)
		{
			return;
		}
		float renderMaxDistanceSquared = GetRenderMaxDistanceSquared();
		CurrentCameraDistanceSquared = RootParent.Position.DistanceSquared(InventoryManager.WorldPosition);
		bool flag = CurrentCameraDistanceSquared < renderMaxDistanceSquared;
		if (flag)
		{
			CheckShadowOverride();
		}
		if (this is Entity)
		{
			return;
		}
		if (!flag)
		{
			if (!IsOccluded && !IsBeingDestroyed)
			{
				_renderChangeScheduled = true;
				RenderChange(setRenderer: false, this).Forget();
			}
		}
		else if (IsOccluded && !IsBeingDestroyed)
		{
			_renderChangeScheduled = true;
			RenderChange(setRenderer: true, this).Forget();
		}
	}

	private void CheckShadowOverride()
	{
		if (CurrentCameraDistanceSquared < GetShadowMaxDistanceSquared() && GetShadowCastingMode() == ShadowCastingMode.On)
		{
			if (_shadowMode != ShadowCastingMode.On)
			{
				_shadowMode = ShadowCastingMode.On;
				OverrideShadows().Forget();
			}
		}
		else if (_shadowMode != ShadowCastingMode.Off)
		{
			_shadowMode = ShadowCastingMode.Off;
			OverrideShadows().Forget();
		}
	}

	public void UpdateShadowMode()
	{
	}

	public virtual bool CanCacheRenderer(Renderer selectedRenderer)
	{
		return true;
	}

	public virtual void OnRenamed()
	{
	}

	public virtual void OnDamageDestroyed()
	{
		IsBurning = false;
	}

	private bool HasState(string stateName, out Interactable interactable)
	{
		int num = Animator.StringToHash(stateName);
		if (num == Interactable.OnOffState && HasOnOffState)
		{
			interactable = InteractOnOff;
			return true;
		}
		if (num == Interactable.PoweredState && HasPowerState)
		{
			interactable = InteractPowered;
			return true;
		}
		if (num == Interactable.ErrorState && HasErrorState)
		{
			interactable = InteractError;
			return true;
		}
		if (num == Interactable.ActivateState && HasActivateState)
		{
			interactable = InteractActivate;
			return true;
		}
		if (num == Interactable.OpenState && HasOpenState)
		{
			interactable = InteractOpen;
			return true;
		}
		if (num == Interactable.ModeState && HasModeState)
		{
			interactable = InteractMode;
			return true;
		}
		if (num == Interactable.LockState && HasLockState)
		{
			interactable = InteractLock;
			return true;
		}
		if (num == Interactable.Button1State && HasButton1State)
		{
			interactable = InteractButton1;
			return true;
		}
		if (num == Interactable.Button2State && HasButton2State)
		{
			interactable = InteractButton2;
			return true;
		}
		if (num == Interactable.Button3State && HasButton3State)
		{
			interactable = InteractButton1;
			return true;
		}
		if (num == Interactable.AccessState && HasAccessState)
		{
			interactable = InteractAccess;
			return true;
		}
		if (num == Interactable.ColorState && HasColorState)
		{
			interactable = InteractColor;
			return true;
		}
		if (num == Interactable.ExportState && HasExportState)
		{
			interactable = InteractExport;
			return true;
		}
		if (num == Interactable.ImportState && HasImportState)
		{
			interactable = InteractImport;
			return true;
		}
		if (num == Interactable.Import2State && HasImport2State)
		{
			interactable = InteractImport2;
			return true;
		}
		if (num == Interactable.Export2State && HasExport2State)
		{
			interactable = InteractExport2;
			return true;
		}
		interactable = null;
		return false;
	}

	public Interactable GetInteractable(Collider selectedCollider)
	{
		if (_interactableColliderLookup == null)
		{
			return null;
		}
		_interactableColliderLookup.TryGetValue(selectedCollider, out var value);
		return value;
	}

	public Interactable GetInteractable(InteractableType action)
	{
		if (_interactableLookup == null)
		{
			return null;
		}
		_interactableLookup.TryGetValue(action, out var value);
		return value;
	}

	public Slot GetSlot(Collider selectedCollider)
	{
		_slotLookup.TryGetValue(selectedCollider, out var value);
		return value;
	}

	public Slot GetSlot(InteractableType interactableType)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Action == interactableType)
			{
				return slot;
			}
		}
		return null;
	}

	public static void Interact(Thing thing, InteractableType interactableType, int state)
	{
		thing?.Interact(interactableType, state);
	}

	public void Interact(InteractableType interactableType, int state)
	{
		Interact(interactableType switch
		{
			InteractableType.Open => InteractOpen, 
			InteractableType.Button1 => InteractButton1, 
			InteractableType.Button2 => InteractButton2, 
			InteractableType.Button3 => InteractButton3, 
			InteractableType.OnOff => InteractOnOff, 
			InteractableType.Mode => InteractMode, 
			InteractableType.Lock => InteractLock, 
			InteractableType.Import => InteractImport, 
			InteractableType.Import2 => InteractImport2, 
			InteractableType.Export => InteractExport, 
			InteractableType.Activate => InteractActivate, 
			InteractableType.Powered => InteractPowered, 
			InteractableType.Error => InteractError, 
			InteractableType.Export2 => InteractExport2, 
			InteractableType.Color => InteractColor, 
			InteractableType.Access => InteractAccess, 
			_ => null, 
		}, state);
	}

	public static void Interact(Interactable interactable, int state)
	{
		if (interactable != null)
		{
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(interactable, state);
			}
			else if (NetworkManager.IsClient)
			{
				NetworkClient.Interact(interactable, state);
			}
		}
	}

	public static void Merge(IMergeable parent, IMergeable child)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Merge(parent, child);
		}
		else
		{
			NetworkClient.Merge(parent, child);
		}
	}

	public virtual void Awake()
	{
		InitAudio();
		_hasBaseAnimator = BaseAnimator != null;
		SuppressSound = true;
		HasRunOnAtmospherics = GameManager.GameState != GameState.Running;
		if (this is ITransmitable transmitable)
		{
			transmitable.OnTransmitterCreated();
		}
		if (this is IPhysical physicalThing && !IsCursor)
		{
			Register(physicalThing);
		}
		if (this is ILogicTick stack)
		{
			LogicStack.Register(stack);
		}
		if (!IsCursor && this is ISubmergeable handler)
		{
			SubmergedHandler.Register(handler);
		}
		if (!IsCursor && this is IAnimalFood item)
		{
			Plant.AllEdibles.Add(item);
		}
		_interactableColor = Interactables.Find((Interactable i) => i.Action == InteractableType.Color);
		_interactableActivate = Interactables.Find((Interactable i) => i.Action == InteractableType.Activate);
		_interactableOnOff = Interactables.Find((Interactable i) => i.Action == InteractableType.OnOff);
		_interactablePowered = Interactables.Find((Interactable i) => i.Action == InteractableType.Powered);
		_interactableError = Interactables.Find((Interactable i) => i.Action == InteractableType.Error);
		_interactableMode = Interactables.Find((Interactable i) => i.Action == InteractableType.Mode);
		_interactableLock = Interactables.Find((Interactable i) => i.Action == InteractableType.Lock);
		_interactableOpen = Interactables.Find((Interactable i) => i.Action == InteractableType.Open);
		_interactableImport = Interactables.Find((Interactable i) => i.Action == InteractableType.Import);
		_interactableImport2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Import2);
		_interactableExport = Interactables.Find((Interactable i) => i.Action == InteractableType.Export);
		_interactableExport2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Export2);
		_interactableAccess = Interactables.Find((Interactable i) => i.Action == InteractableType.Access);
		_interactableButton1 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button1);
		_interactableButton2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button2);
		_interactableButton3 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button3);
		_interactableButton4 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button4);
		_asDynamicThing = this as DynamicThing;
		_asStructure = this as Structure;
		_asEntity = this as Entity;
		IsEntity = GetType() == typeof(Entity) || GetType().IsSubclassOf(typeof(Entity));
		_customMaterials = new List<CustomColorMapping>();
		int colorIndex = GameManager.GetColorIndex(PaintableMaterial);
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (!CanCacheRenderer(meshRenderer))
			{
				continue;
			}
			ThingRenderer thingRenderer = new ThingRenderer(this, meshRenderer);
			for (int num2 = 0; num2 < meshRenderer.sharedMaterials.Length; num2++)
			{
				Material material = meshRenderer.sharedMaterials[num2];
				if ((object)material != null && (material == PaintableMaterial || material?.mainTexture is Texture2DArray))
				{
					_customMaterials.Add(new CustomColorMapping(thingRenderer, num2, colorIndex));
				}
			}
			if (!thingRenderer.HasRenderer())
			{
				Debug.Log("renderer null");
			}
			Renderers.Add(thingRenderer);
		}
		ConfigureSlots();
		BaseAnimator = GetComponent<Animator>();
		if ((bool)BaseAnimator && BaseAnimator.runtimeAnimatorController == null)
		{
			BaseAnimator = null;
		}
		SetupInteractables();
		_slotLookup.Clear();
		foreach (Slot slot in Slots)
		{
			slot.Initialize();
			if ((bool)slot.Collider && slot.IsInteractable && !_slotLookup.ContainsKey(slot.Collider))
			{
				_slotLookup.Add(slot.Collider, slot);
			}
		}
		foreach (GameAudioEvent audioEvent in AudioEvents)
		{
			if (!_audioEventLookup.ContainsKey(audioEvent.NameHash))
			{
				_audioEventLookup.Add(audioEvent.NameHash, audioEvent);
			}
		}
		foreach (Interactable interactable in Interactables)
		{
			interactable.Initialize();
			interactable.SetState();
			if (_interactableLookup == null)
			{
				_interactableLookup = new Dictionary<InteractableType, Interactable>();
			}
			if (!_interactableLookup.ContainsKey(interactable.Action))
			{
				_interactableLookup.Add(interactable.Action, interactable);
			}
			if (!interactable.Collider)
			{
				continue;
			}
			if (_interactableColliderLookup == null)
			{
				_interactableColliderLookup = new Dictionary<Collider, Interactable>();
			}
			if (_interactableColliderLookup.TryGetValue(interactable.Collider, out var value))
			{
				if (_slotLookup.TryGetValue(interactable.Collider, out var value2) && value2.Action == interactable.Action && value.Action != value2.Action)
				{
					_interactableColliderLookup[interactable.Collider] = interactable;
				}
			}
			else
			{
				_interactableColliderLookup.Add(interactable.Collider, interactable);
			}
		}
		LensFlare[] componentsInChildren2 = GetComponentsInChildren<LensFlare>(includeInactive: true);
		foreach (LensFlare item2 in componentsInChildren2)
		{
			lodFlares.Add(item2);
		}
		InitializeDamageState();
		CacheColliders();
		HandleCollisionWith(this);
		if ((bool)PaintableMaterial)
		{
			CustomColor = GameManager.GetColorSwatch(PaintableMaterial);
		}
		SuppressSound = false;
		IsInstantiated = true;
		if (!IsCursor)
		{
			if (this is IPowered item3)
			{
				ElectricityManager.Register(item3);
			}
			if (this is ILightActivated dynamicThing)
			{
				LightManager.Register(dynamicThing);
			}
		}
	}

	public static Thing Find(Collider collider)
	{
		Thing value = null;
		_colliderLookup.TryGetValue(collider, out value);
		return value;
	}

	public static bool TryFind(Collider collider, out Thing thing)
	{
		return _colliderLookup.TryGetValue(collider, out thing);
	}

	public static T Find<T>(Collider collider) where T : class, IReferencable
	{
		if ((object)collider == null)
		{
			return null;
		}
		_colliderLookup.TryGetValue(collider, out var value);
		if (value is T result)
		{
			return result;
		}
		return null;
	}

	public static bool Exists<T>(Collider collider, out T thing) where T : class, IReferencable
	{
		if ((object)collider == null)
		{
			thing = null;
			return false;
		}
		_colliderLookup.TryGetValue(collider, out var value);
		if (!(value is T val))
		{
			thing = null;
			return false;
		}
		thing = val;
		return true;
	}

	public void CacheColliders()
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
		foreach (Collider collider in componentsInChildren)
		{
			if ((bool)collider.attachedRigidbody)
			{
				_dynamicColliders.Add(collider);
			}
			else
			{
				_staticColliders.Add(collider);
			}
			_colliderLookup[collider] = this;
		}
		componentsInChildren = GetComponents<Collider>();
		foreach (Collider item in componentsInChildren)
		{
			_selfColliders.Add(item);
		}
	}

	public void IgnoreCollisionWith(Collider c, bool ignore)
	{
		foreach (Collider staticCollider in _staticColliders)
		{
			Physics.IgnoreCollision(c, staticCollider, ignore);
		}
		foreach (Collider dynamicCollider in _dynamicColliders)
		{
			Physics.IgnoreCollision(c, dynamicCollider, ignore);
		}
	}

	public void HandleCollisionWith(Thing collidingThing, bool ignore = true)
	{
		foreach (Collider staticCollider in _staticColliders)
		{
			collidingThing.IgnoreCollisionWith(staticCollider, ignore);
		}
		foreach (Collider dynamicCollider in _dynamicColliders)
		{
			collidingThing.IgnoreCollisionWith(dynamicCollider, ignore);
		}
		foreach (Collider staticCollider2 in collidingThing._staticColliders)
		{
			IgnoreCollisionWith(staticCollider2, ignore);
		}
		foreach (Collider dynamicCollider2 in collidingThing._dynamicColliders)
		{
			IgnoreCollisionWith(dynamicCollider2, ignore);
		}
	}

	public virtual bool IsAudible()
	{
		return ((InventoryManager.Parent != null) ? Vector3.SqrMagnitude(InventoryManager.Parent.Position - Position) : float.MaxValue) < AudioDistanceSquared;
	}

	public virtual void InitializeDamageState()
	{
		DamageState = new ThingDamageState(this, ThingHealth);
	}

	public virtual void UpdateEachFrame()
	{
	}

	public virtual void UpdateAudio(float deltaTime)
	{
	}

	public virtual void Update100MS(float deltaTime)
	{
	}

	private bool IsOverrideMethod(string methodName)
	{
		return GetType().GetMethod(methodName)?.DeclaringType != typeof(Thing);
	}

	public virtual void Update1000MS(float deltaTime)
	{
	}

	public static void UpdateLodFlares()
	{
		if (AllLodFlareThings.Count == 0)
		{
			return;
		}
		TerrainCurvature.Refresh();
		Vector3 cameraPosition = CameraController.CameraPosition;
		if (_lodFlareUpdateIndex <= 0 || _lodFlareUpdateIndex >= AllLodFlareThings.Count)
		{
			_lodFlareUpdateIndex = AllLodFlareThings.Count - 1;
		}
		float num = (float)_lodFlareUpdateIndex - (float)AllLodFlareThings.Count * 0.2f;
		if (num < 0f)
		{
			num = 0f;
		}
		int num2 = _lodFlareUpdateIndex;
		while ((float)num2 >= num)
		{
			_lodFlareUpdateIndex = num2;
			Thing thing = AllLodFlareThings[num2];
			if ((object)thing == null)
			{
				AllLodFlareThings.RemoveAt(num2);
			}
			else
			{
				Vector3 vector = ((thing.Position.y < 1000f) ? TerrainCurvature.Curve(thing.Position, cameraPosition) : thing.Position);
				foreach (LensFlare lodFlare in thing.lodFlares)
				{
					lodFlare.brightness = CursorManager.Instance.FlareBrightnessDistanceCurve.Evaluate(Vector3.Distance(cameraPosition, thing.Position));
					lodFlare.transform.position = vector;
				}
			}
			num2--;
		}
	}

	public virtual void OnThreadUpdate()
	{
	}

	public async UniTaskVoid ScheduleGridEventFromThread(GridEvent gridEvent)
	{
		if (GameManager.GameState != GameState.None)
		{
			await UniTask.SwitchToMainThread();
			OnGridEvent(gridEvent);
		}
	}

	public virtual void OnGridEvent(GridEvent gridEvent)
	{
	}

	public virtual CanEnterResult CanEnter(Slot destinationSlot)
	{
		if (destinationSlot == null)
		{
			return CanEnterResult.Succeed;
		}
		if (IsEntity && destinationSlot.Type != Slot.Class.Entity)
		{
			return CanEnterResult.Fail(GameStrings.EntityCantEnterNonEntitySlot);
		}
		if (destinationSlot.Parent.ExistsInHierarchy(this))
		{
			return CanEnterResult.Fail(GameStrings.CantEnterOwnHierarchy);
		}
		if (!CanPickup)
		{
			return CanEnterResult.Fail(GameStrings.CantPickUp);
		}
		if (this == destinationSlot.Parent)
		{
			return CanEnterResult.Fail(GameStrings.CantEnterSelf);
		}
		if (!destinationSlot.TakesSpecificItem(PrefabHash))
		{
			return CanEnterResult.Fail(GameStrings.ThingIsNotType, DisplayName, destinationSlot.DisplayName);
		}
		return CanEnterResult.Succeed;
	}

	protected virtual DelayedActionInstance _HandleSwitchFreeHands(Slot handSlot, Slot selectedSlot, DelayedActionInstance result, bool doAction = true)
	{
		if (selectedSlot.IsNotEmpty())
		{
			DynamicThing dynamicThing = selectedSlot.Get();
			PassiveTooltip passiveTooltip = dynamicThing.GetPassiveTooltip(null);
			result.OverrideTitle = passiveTooltip.Title;
			string text = dynamicThing.GetExtendedText().ToString();
			StringBuilder slotTooltip = dynamicThing.GetSlotTooltip();
			if (slotTooltip.Length > 0)
			{
				result.ExtendedMessage += slotTooltip;
			}
			else if (!string.IsNullOrEmpty(passiveTooltip.Extended))
			{
				result.ExtendedMessage += passiveTooltip.Extended;
			}
			else if (!string.IsNullOrEmpty(text))
			{
				result.ExtendedMessage += text;
			}
			result.ActionMessage = ActionStrings.Take;
			if (!selectedSlot.Occupant.CanEnter(handSlot))
			{
				if (selectedSlot.Occupant.IsEntity)
				{
					if (!doAction)
					{
						result.ClearStateMessage();
						result.AppendStateMessage(GameStrings.ThingMoveToWorld, selectedSlot.Occupant.ToTooltip());
						return result.Succeed();
					}
					Vector3 vector = ((selectedSlot.Parent is IExitable exitable) ? exitable.GetExitPosition(selectedSlot.Occupant as Entity) : selectedSlot.Location.position);
					OnServer.MoveToWorld(selectedSlot.Occupant, vector, selectedSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
					return result.Succeed();
				}
				return result.Fail(GameStrings.ThingCanNotEnterSlot, selectedSlot.Occupant.ToTooltip(), handSlot.ToTooltip());
			}
			if (!doAction)
			{
				return result.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.MoveToSlot(selectedSlot.Occupant, handSlot);
			}
			return result.Succeed();
		}
		return result.Fail();
	}

	protected string GetDamageColor()
	{
		int totalRounded = DamageState.TotalRounded;
		if (totalRounded > 50)
		{
			if (totalRounded > 75)
			{
				return "red";
			}
			return "orange";
		}
		if (totalRounded > 25)
		{
			return "yellow";
		}
		return "green";
	}

	public virtual StringBuilder GetExtendedText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (IsBurning)
		{
			stringBuilder.AppendLine(GameStrings.CurrentlyOnFire.DisplayString);
		}
		if (IsBroken)
		{
			stringBuilder.AppendLine(GameStrings.IsDestroyed.DisplayString);
		}
		else if (DamageState.TotalRounded > 0)
		{
			if (DamageState.DecayRounded == DamageState.TotalRounded)
			{
				stringBuilder.AppendLine(GameStrings.ThingDamageIsAt.AsString(GameStrings.DamageTypeDecay, DamageState.DecayRounded.ToStringPercent(GetDamageColor())));
			}
			else
			{
				AddDamageString(stringBuilder);
			}
		}
		if (!CanPickup)
		{
			stringBuilder.AppendLine(GameStrings.CannotBePickedUpRightNow.DisplayString);
		}
		return stringBuilder;
	}

	public void AddDamageString(StringBuilder sb, bool indent = false)
	{
		if (indent)
		{
			sb.Append(StringManager.Indent);
		}
		sb.AppendLine(GameStrings.ThingDamageIsAt.AsString(GameStrings.DamageTypeDamage, DamageState.TotalRounded.ToStringPercent(GetDamageColor())));
	}

	public void AddNamedDamageString(StringBuilder sb, bool indent = false)
	{
		if (indent)
		{
			sb.Append(StringManager.Indent);
		}
		sb.AppendLine(GameStrings.NamedThingDamageIsAt.AsString(ToTooltip(), GameStrings.DamageTypeDamage, DamageState.TotalRounded.ToStringPercent(GetDamageColor())));
	}

	protected DelayedActionInstance _HandleSwitchMoveToSlot(Slot handSlot, Slot selectedSlot, DelayedActionInstance result, bool doAction = true)
	{
		PassiveTooltip passiveTooltip = handSlot.Occupant.GetPassiveTooltip(null);
		result.OverrideTitle = passiveTooltip.Title;
		if (!string.IsNullOrEmpty(passiveTooltip.Extended))
		{
			result.ExtendedMessage += passiveTooltip.Extended;
		}
		result.ActionMessage = ActionStrings.Insert;
		if (!selectedSlot.Parent.AllowInteraction)
		{
			return result.Fail(GameStrings.ThingCanNotInsertInteractionsDisabled, handSlot.Occupant.ToTooltip(), selectedSlot.Parent.ToTooltip());
		}
		if (!handSlot.Occupant.CanEnter(selectedSlot))
		{
			return result.Fail(GameStrings.ThingCanNotEnterSlot, handSlot.Occupant.ToTooltip(), selectedSlot.ToTooltip());
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.MoveToSlot(handSlot.Occupant, selectedSlot);
		}
		return result.Succeed();
	}

	protected virtual DelayedActionInstance _HandleSwitchAddToStack(Slot handSlot, IMergeable inventoryStack, Slot selectedSlot, IMergeable currentStack, DelayedActionInstance result, bool doAction = true)
	{
		PassiveTooltip passiveTooltip = selectedSlot.Occupant.GetPassiveTooltip(null);
		result.OverrideTitle = passiveTooltip.Title;
		if (!string.IsNullOrEmpty(passiveTooltip.Extended))
		{
			result.ExtendedMessage += passiveTooltip.Extended;
		}
		result.ActionMessage = ActionStrings.Collect;
		if (!selectedSlot.Occupant.CanEnter(handSlot) || !handSlot.Occupant.CanEnter(selectedSlot))
		{
			result.Fail(GameStrings.SlotCanNotUseWith, selectedSlot.ToTooltip(), handSlot.ToTooltip());
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		CmdMoveToStack(inventoryStack, currentStack);
		return result.Succeed();
	}

	public static void CmdMoveToStack(IMergeable child, IMergeable parent)
	{
		if (parent != null && child != null)
		{
			if (GameManager.RunSimulation)
			{
				parent.Merge(child);
				return;
			}
			NetworkClient.SendToServer(new MergeStackablesMessage
			{
				ParentItemId = parent.ReferenceId,
				ChildItemId = child.ReferenceId
			});
		}
	}

	protected virtual DelayedActionInstance _HandleSwitchSwapItems(Interaction interaction, Slot handSlot, Slot selectedSlot, int slot, DelayedActionInstance result, bool doAction = true)
	{
		PassiveTooltip passiveTooltip = selectedSlot.Occupant.GetPassiveTooltip(null);
		result.OverrideTitle = passiveTooltip.Title;
		if (!string.IsNullOrEmpty(passiveTooltip.Extended))
		{
			result.ExtendedMessage += passiveTooltip.Extended;
		}
		result.ActionMessage = ActionStrings.Swap;
		if (!selectedSlot.IsSwappable || !handSlot.IsSwappable || selectedSlot.IsLocked || handSlot.IsLocked)
		{
			return result.Fail(GameStrings.ThingCanNotSwap, selectedSlot.Occupant.ToTooltip(), handSlot.Occupant.ToTooltip());
		}
		if (!selectedSlot.Occupant.CanEnter(handSlot))
		{
			return result.Fail(GameStrings.SlotCanNotUseWith, selectedSlot.ToTooltip(), handSlot.ToTooltip());
		}
		if (handSlot.Occupant.GetType() == selectedSlot.Occupant.GetType() && handSlot.Occupant is Stackable stackable && stackable.Quantity >= stackable.MaxQuantity)
		{
			result.Fail(GameStrings.InventoryStackIsFull, handSlot.Occupant.ToTooltip());
		}
		CanEnterResult canEnterResult = handSlot.Occupant.CanEnter(selectedSlot);
		if (!canEnterResult)
		{
			result.AppendStateMessage(canEnterResult.Reason);
			return result.Fail();
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SwapSlotsMessage
			{
				ThingId1 = netId,
				ThingId2 = interaction.SourceThing.netId,
				SlotId1 = slot,
				SlotId2 = handSlot.SlotIndex
			});
		}
		else
		{
			OnServer.SwapSlots(netId, interaction.SourceThing.netId, slot, handSlot.SlotIndex);
		}
		return result.Fail();
	}

	public virtual DelayedActionInstance HandleSwitch(Interaction interaction, int slot, DelayedActionInstance result, bool doAction = true, bool force = false)
	{
		Slot slot2 = Slots[slot];
		Slot sourceSlot = interaction.SourceSlot;
		if (doAction)
		{
			if (sourceSlot.Parent.HasAuthority && (force || (slot2.IsInteractable && !slot2.IsLocked)))
			{
				if (!sourceSlot.Occupant && (bool)slot2.Occupant)
				{
					sourceSlot.PlaySlotEnterUiSound();
				}
				if (sourceSlot.Occupant != null)
				{
					if (!slot2.Occupant && (bool)sourceSlot.Occupant && slot2.IsAllowedType(sourceSlot.Occupant))
					{
						slot2.PlaySlotEnterUiSound();
					}
					else if ((bool)sourceSlot.Occupant && (bool)slot2.Occupant && slot2.IsAllowedType(sourceSlot.Occupant))
					{
						slot2.PlaySlotEnterUiSound();
					}
					else if (!slot2.IsAllowedType(sourceSlot.Occupant) || !slot2.IsInteractable || ((bool)sourceSlot.Occupant && !sourceSlot.Occupant.CanPickup) || ((bool)slot2.Occupant && !slot2.Occupant.CanPickup))
					{
						UIAudioManager.Play(UIAudioManager.ActionFailHash);
					}
					if (sourceSlot.Contains<IMergeable>(out var occupant) && slot2.Contains<IMergeable>(out var occupant2) && occupant.CanStack(occupant2))
					{
						sourceSlot.PlaySlotEnterUiSound();
					}
				}
			}
			if (!GameManager.RunSimulation)
			{
				return result.Succeed();
			}
		}
		result.ActionMessage = slot2.GetSafeName();
		if (!force && (!slot2.IsInteractable || slot2.IsLocked))
		{
			return result.Fail(GameStrings.ThingCurrentlyNotInteractable, slot2.ToTooltip());
		}
		if ((bool)sourceSlot.Occupant && !sourceSlot.Occupant.CanPickup)
		{
			return result.Fail(GameStrings.ThingCanNotPickUp, sourceSlot.Occupant.ToTooltip());
		}
		if ((bool)slot2.Occupant && !slot2.Occupant.CanPickup)
		{
			return result.Fail(GameStrings.ThingCanNotPickUp, slot2.Occupant.ToTooltip());
		}
		if (!sourceSlot.Occupant)
		{
			return _HandleSwitchFreeHands(sourceSlot, slot2, result, doAction);
		}
		if (!slot2.IsAllowedType(sourceSlot.Get()))
		{
			DelayedActionInstance delayedActionInstance = result.Fail(GameStrings.ThingIsNotType, sourceSlot.Occupant.ToTooltip(), slot2.ToTooltip());
			if ((bool)slot2.Get())
			{
				PassiveTooltip passiveTooltip = slot2.Occupant.GetPassiveTooltip(null);
				if (!string.IsNullOrEmpty(passiveTooltip.Extended))
				{
					delayedActionInstance.ExtendedMessage += passiveTooltip.Extended;
				}
			}
			return delayedActionInstance;
		}
		if (!slot2.Occupant)
		{
			return _HandleSwitchMoveToSlot(sourceSlot, slot2, result, doAction);
		}
		if (sourceSlot.Contains<IMergeable>(out var occupant3) && slot2.Contains<IMergeable>(out var occupant4) && occupant3.CanStack(occupant4))
		{
			if (occupant3.IsStackFull)
			{
				return _HandleSwitchSwapItems(interaction, sourceSlot, slot2, slot, result, doAction);
			}
			return _HandleSwitchAddToStack(sourceSlot, occupant4, slot2, occupant3, result, doAction);
		}
		return _HandleSwitchSwapItems(interaction, sourceSlot, slot2, slot, result, doAction);
	}

	private void DestroyChildren(Transform tran)
	{
		for (int num = tran.childCount - 1; num >= 0; num--)
		{
			GameObject gameObject = tran.GetChild(num).gameObject;
			DestroyChildren(gameObject.transform);
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	public void OnThingCustomNameInvoke()
	{
		this.OnThingCustomName?.Invoke();
	}

	public virtual void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		if (this is IBatchRendered batchRendered)
		{
			BatchRenderer.Remove(batchRendered);
		}
		BeingDestroyed = true;
		StopAllAudio();
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			DestroyToSend.Add(DestroyEvent.Create(this));
		}
		if (this is ITransmitable item)
		{
			Transmitters.AllTransmitters.Remove(item);
		}
		if (this is IPhysical physicalThing)
		{
			Deregister(physicalThing);
		}
		if (this is ISubmergeable handler)
		{
			SubmergedHandler.DeRegister(handler);
		}
		if (this is IAnimalFood item2)
		{
			Plant.AllEdibles.Remove(item2);
		}
		if (this is IPowered item3)
		{
			ElectricityManager.Deregister(item3);
		}
		if (this is ILightActivated dynamicThing)
		{
			LightManager.Deregister(dynamicThing);
		}
		OcclusionManager.Deregister(this);
		if (IsBurning)
		{
			AtmosphericsManager.DeregisterBurningThing(this);
		}
		HelperHintsManager.Deregister(this);
		ReleaseThing();
		OnReleaseReagents();
		foreach (Collider staticCollider in _staticColliders)
		{
			_colliderLookup.Remove(staticCollider);
		}
		foreach (Collider dynamicCollider in _dynamicColliders)
		{
			_colliderLookup.Remove(dynamicCollider);
		}
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.OnParentDestroyed();
		}
		if ((bool)Wireframe)
		{
			UnityEngine.Object.Destroy(Wireframe.gameObject);
			UnityEngine.Object.Destroy(Wireframe);
			Wireframe = null;
		}
		if (!DestroyChildrenOnDead)
		{
			foreach (Slot slot in Slots)
			{
				if ((bool)slot.Occupant)
				{
					if (!GameManager.RunSimulation)
					{
						slot.Occupant.MoveToWorld();
					}
					else
					{
						OnServer.MoveToWorld(slot.Occupant);
					}
				}
			}
		}
		DestroyChildren(base.transform);
		if (this.OnDestroyed != null)
		{
			this.OnDestroyed();
		}
	}

	public void ReleaseThing()
	{
		Referencable.Deregister(this);
		if (GameManager.GameState == GameState.Running && !IsCursor)
		{
			TotalThings--;
		}
		if (GameManager.GameState != GameState.None && InternalAtmosphere != null)
		{
			AtmosphericsManager.DeregisterFromMainThead(InternalAtmosphere);
		}
	}

	public virtual void OnReleaseReagents()
	{
	}

	public virtual void OnAnimationStart()
	{
		if (GameManager.GameState == GameState.Running)
		{
			_disableInteractionFromAnimation = true;
		}
	}

	public virtual void OnAnimationStop()
	{
		_disableInteractionFromAnimation = false;
	}

	public void LocalSetChildrenLock(bool isLocked)
	{
		foreach (Slot item in Slots.Where((Slot slot) => slot.Occupant))
		{
			item.Occupant.CanPickup = !isLocked;
		}
	}

	public void FlagReagentStateChange(Reagent reagent)
	{
		if (ReagentMixture != null && NetworkManager.IsServer)
		{
			NetworkUpdateFlags |= 8;
		}
	}

	public static void OnMinedOreInvoke(Ore oreObject)
	{
		Thing.OnOreMined?.Invoke(oreObject);
	}

	public static void OnMinedOreAmountInvoke(Ore oreObject, float quantity)
	{
		Thing.OnMinedOreAmountEvent?.Invoke(oreObject, quantity);
	}

	public virtual void SetVisibility(bool isVisible, bool hideOnPlayer = false, bool isRecursive = false, bool shouldUpdateLayers = true)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant != null)
			{
				if (slot.OccupantAlwaysVisible)
				{
					slot.Occupant.SetVisibility(isVisible: true);
				}
				else if (slot.HidesOccupant)
				{
					slot.Occupant.SetVisibility(isVisible: false);
				}
				else
				{
					slot.Occupant.SetVisibility(isVisible, hideOnPlayer, isRecursive);
				}
			}
		}
		if (isRecursive && (bool)AsDynamicThing && AsDynamicThing.ParentSlot != null && AsDynamicThing.ParentSlot.HidesOccupant)
		{
			isVisible = false;
		}
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.Visible = isVisible;
			if (shouldUpdateLayers)
			{
				renderer.Parent.transform.gameObject.layer = (hideOnPlayer ? 8 : 0);
			}
		}
		GetShadowCastingMode();
		SetLightVisibility(isVisible, hideOnPlayer);
	}

	public virtual void SetLightVisibility(bool isVisible, bool hideOnPlayer = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (ThingLight light in Lights)
		{
			light.SetVisible(isVisible, isVisible && !hideOnPlayer);
		}
	}

	public virtual ShadowCastingMode GetShadowCastingMode()
	{
		switch (OcclusionManager.ThingShadowMode)
		{
		case ThingShadowMode.High:
		case ThingShadowMode.Medium:
		case ThingShadowMode.Extreme:
			if (!(SurfaceArea < WorldManager.ShadowCullSize))
			{
				return ShadowCastingMode.On;
			}
			return ShadowCastingMode.Off;
		case ThingShadowMode.Low:
			return ShadowCastingMode.Off;
		default:
			return ShadowCastingMode.Off;
		}
	}

	public virtual void OnChildEnterInventory(DynamicThing newChild)
	{
		Item item = newChild as Item;
		if (item != null && !newChild.ParentSlot.RealWorldScale)
		{
			item.ThingTransform.localScale = Vector3.one * item.InventoryScale * newChild.ParentSlot.ScaleMultiplier;
		}
		else
		{
			newChild.ThingTransform.localScale = Vector3.one;
		}
		CheckOnSlot();
		if ((bool)(newChild as Human))
		{
			((Human)newChild).PauseFoleyAudio = true;
		}
		SetSlotOccupantTransformData(newChild);
	}

	public virtual void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ThingTransformLocalPosition = Vector3.zero;
			newChild.ThingTransformLocalRotation = Quaternion.identity;
		}
	}

	public virtual void OnChildExitInventory(DynamicThing previousChild)
	{
		previousChild.ThingTransform.localScale = Vector3.one;
		CheckOnSlot();
		if (previousChild is Human human)
		{
			human.PauseFoleyAudio = false;
		}
	}

	public void CheckOnSlot()
	{
		this.OnSlot?.Invoke();
	}

	public virtual void OnChildDropped(DynamicThing childThing)
	{
	}

	public virtual void OnChildThrown(DynamicThing childThing, float force)
	{
	}

	public virtual object GetModXmlType()
	{
		return new ThingModData();
	}

	public virtual void DeserializModData(ThingModData modData)
	{
		if (modData != null)
		{
			if (!float.IsNaN(modData.SurfaceAreaScale))
			{
				SurfaceAreaScale = modData.SurfaceAreaScale;
			}
			if (!float.IsNaN(modData.ThermodynamicsScale))
			{
				ThermodynamicsScale = modData.ThermodynamicsScale;
			}
			if (!float.IsNaN(modData.SolarHeatingScale))
			{
				SolarHeatingScale = modData.SolarHeatingScale;
			}
			if (!float.IsNaN(modData.ShatterTemperature))
			{
				shatterTemperature = modData.ShatterTemperature;
			}
			if (!float.IsNaN(modData.FlashpointTemperature))
			{
				flashpointTemperature = modData.FlashpointTemperature;
			}
			if (!float.IsNaN(modData.AutoignitionTemperature))
			{
				autoignitionTemperature = modData.AutoignitionTemperature;
			}
			if (!float.IsNaN(modData.BurnTime))
			{
				BurnTime = modData.BurnTime;
			}
			if (!float.IsNaN(modData.EnergyReleasedWhenBurning))
			{
				EnergyReleasedWhenBurned = modData.EnergyReleasedWhenBurning;
			}
		}
	}

	public virtual ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ThingSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public virtual void DeserializeSave(ThingSaveData saveData)
	{
		if (saveData is StructureSaveData structureSaveData)
		{
			Position = structureSaveData.RegisteredWorldPosition;
			ThingTransformPosition = structureSaveData.RegisteredWorldPosition;
			ThingTransform.rotation = structureSaveData.RegisteredWorldRotation;
		}
		else
		{
			ThingTransformPosition = saveData.WorldPosition;
			Position = saveData.WorldPosition;
			ThingTransform.rotation = saveData.WorldRotation;
		}
		base.name = saveData.PrefabName;
		CustomName = saveData.CustomName;
		OwnerClientId = saveData.OwnerSteamId;
		foreach (InteractableState state in saveData.States)
		{
			if (BaseAnimator != null && BaseAnimator.HasParameter(state.StateName))
			{
				BaseAnimator.SetInteger(state.StateName, state.State);
				if (!Prefab.TryFind(base.name, out Structure _))
				{
					continue;
				}
				if (state.StateName == "Export2" || state.StateName == "Export")
				{
					BaseAnimator.SetInteger(state.StateName, 0);
				}
			}
			if (HasState(state.StateName, out var interactable))
			{
				interactable.State = state.State;
			}
		}
		foreach (ReagentSaveData reagent2 in saveData.Reagents)
		{
			Reagent reagent = Reagent.Generate(reagent2.TypeName);
			if (reagent != null)
			{
				reagent.Quantity = reagent2.Quantity;
				if (ReagentMixture == null)
				{
					ReagentMixture = new ReagentMixture(reagent, this);
				}
				else
				{
					ReagentMixture.Add(reagent);
				}
			}
		}
		foreach (Interactable interactable2 in Interactables)
		{
			if (interactable2.JoinInProgressSync)
			{
				OnInteractableUpdated(interactable2);
				interactable2.SetState();
				OnFinishedInteractionSync(interactable2);
			}
		}
		if (saveData.CustomColorIndex >= 0)
		{
			SetCustomColor(saveData.CustomColorIndex);
		}
		Indestructable = saveData.Indestructable;
		DamageState.Copy(saveData.DamageState);
		DamageState.OnDamageUpdated();
		if (saveData.LogicStack != null && this is ILogicStack logicStack)
		{
			logicStack.GetLogicStack()?.Deserialize(saveData.LogicStack);
		}
		RefreshAnimState(skipAnimation: true);
	}

	protected virtual void InitialiseSaveData(ref ThingSaveData savedData)
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		savedData.ReferenceId = ReferenceId;
		savedData.PrefabName = PrefabName;
		savedData.CustomName = CustomName;
		savedData.WorldPosition = ThingTransformPosition;
		savedData.WorldRotation = ThingTransform.rotation;
		savedData.CustomColorIndex = ((CustomColor.Normal == PaintableMaterial) ? (-1) : GameManager.GetColorIndex(CustomColor));
		savedData.OwnerSteamId = OwnerClientId;
		savedData.Indestructable = Indestructable;
		savedData.DamageState = new DamageUpdate(DamageState);
		if (ReagentMixture != null && ReagentMixture.TotalReagents > 0.0)
		{
			savedData.Reagents = ReagentMixture.Serialize();
		}
		List<InteractableType> list = new List<InteractableType>();
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.JoinInProgressSync && !list.Contains(interactable.Action))
			{
				list.Add(interactable.Action);
				savedData.States.Add(new InteractableState
				{
					StateName = interactable.Action.ToString(),
					State = interactable.State
				});
			}
		}
		if (this is ILogicStack logicStack)
		{
			LogicStack logicStack2 = logicStack.GetLogicStack();
			if (logicStack2 != null)
			{
				savedData.LogicStack = logicStack2.Serialize();
			}
		}
	}

	public virtual void AssignInternalAtmosphereOnLoad(VolumeLitres volume, long referenceId = 0L)
	{
		if (IsCursor)
		{
			return;
		}
		if (referenceId > 0 && volume > VolumeLitres.Zero)
		{
			InternalAtmosphere = new Atmosphere(this, volume, referenceId);
			if (InternalAtmosphere.ReferenceId != 0L)
			{
				InternalAtmosphere.Thing = this;
			}
			else
			{
				InternalAtmosphere = null;
			}
		}
		InitInternalAtmosphere();
	}

	public virtual void InitInternalAtmosphere()
	{
	}

	public virtual bool PreventInteraction(out DelayedActionInstance failResult, Interactable interactable, Interaction interaction)
	{
		failResult = null;
		if (IsBroken)
		{
			failResult = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			}.Fail(GameStrings.DeviceBroken);
			return true;
		}
		if (!AllowInteraction)
		{
			failResult = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			}.Fail(GameStrings.ThingInteractionDisabled);
			return true;
		}
		if (!IsAuthorized(interaction.SourceThing))
		{
			failResult = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			}.Fail(GameStrings.AccessCardUnableToInteract);
			return true;
		}
		if (interactable.Action == InteractableType.Lock || interactable.Slot != null)
		{
			return false;
		}
		if (IsLocked)
		{
			failResult = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			}.Fail(GameStrings.DeviceLocked);
			return true;
		}
		return false;
	}

	public virtual DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Slot != null)
		{
			if (interactable.Slot.Type != Slot.Class.None)
			{
				delayedActionInstance.ExtendedMessage = GameStrings.TypeOfSlot.AsString(Localization.GetName(interactable.Slot)) + "\n";
			}
			Slot sourceSlot = interaction.SourceSlot;
			if (sourceSlot != null && sourceSlot.IsNotEmpty() && interaction.SourceSlot.Get().TryInteractWithSlotOccupant(interactable, out var actionInstance, doAction))
			{
				return actionInstance;
			}
			return HandleSwitch(interaction, interactable.Slot.SlotIndex, delayedActionInstance, doAction);
		}
		switch (interactable.Action)
		{
		case InteractableType.Open:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.OnOff:
			if (!doAction)
			{
				if (Error == 1)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.ThingCurrentlyFlashingError);
				}
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (!OnOff) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Lock:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		default:
			return delayedActionInstance.Fail();
		}
	}

	public virtual void OnAtmosphericTick()
	{
	}

	public virtual void OnPreAtmosphere()
	{
	}

	public virtual void OnFireTick()
	{
	}

	public bool OnFireConsume(Atmosphere atmosphere, float heatEnergyReleased)
	{
		if (IsBroken || IsBeingDestroyed)
		{
			return false;
		}
		lock (atmosphere)
		{
			if (atmosphere.GasMixture.Oxygen.Quantity > FireConsumeMoleAmount)
			{
				atmosphere.GasMixture.Oxygen.Remove(FireConsumeMoleAmount);
				atmosphere.GasMixture.CarbonDioxide.Add(FireConsumeMoleAmount, MoleEnergy.Zero);
				atmosphere.GasMixture.AddEnergy(new MoleEnergy(heatEnergyReleased));
				return true;
			}
			if (ShouldIgnite(atmosphere))
			{
				return true;
			}
			return false;
		}
	}

	public virtual void Extinguish()
	{
		IsBurning = false;
		OnFireStop();
	}

	public void ExtinguishSelfAndChildren()
	{
		Extinguish();
		foreach (Slot slot in Slots)
		{
			slot.Get<Thing>()?.ExtinguishSelfAndChildren();
		}
	}

	public virtual bool ShouldIgnite(Atmosphere atmos)
	{
		if (IsBroken || IsBeingDestroyed)
		{
			return false;
		}
		if (IsInRocket())
		{
			return false;
		}
		TemperatureKelvin temperatureKelvin = new TemperatureKelvin(FlashPointTemperature.ToDouble() / (double)atmos.RatioOneAtmosphereClamped());
		bool num = FlashPointTemperature > TemperatureKelvin.Zero && atmos.Temperature > temperatureKelvin && atmos.Inflamed;
		bool flag = AutoignitionTemperature > TemperatureKelvin.Zero && atmos.Temperature > AutoignitionTemperature && atmos.GasMixture.TotalEnergy > MinimumAutoignitionEnergy;
		return num || flag;
	}

	private bool IsInRocket()
	{
		if (this is IRocketInternals { RocketNetwork: { } rocketNetwork })
		{
			return rocketNetwork.Rocket != null;
		}
		return false;
	}

	public void UpdateFlameVisualizer()
	{
		if (!IsBurning)
		{
			AtmosphericsManager.DeregisterBurningThing(this);
		}
		else if (!IsOccluded)
		{
			float num = 0.25f;
			float alpha = Mathf.Lerp(0.25f, 0.75f, num);
			Color color = AtmosphericsManager.Instance.TemperatureGradient.Evaluate(num).SetAlpha(alpha);
			ThingFire.SetFlameParticleValues(this, new FlameParticleData(color, 1f, Bounds.size * 0.8f, 1));
		}
	}

	public virtual void OnFireStart()
	{
		AtmosphericsManager.RegisterBurningThing(this);
	}

	public virtual void OnFireStop()
	{
	}

	public Thing()
	{
		IsInstantiated = false;
	}

	public void AddReferenceIDToContextualMessage(ref DelayedActionInstance contextMessage)
	{
		contextMessage.ExtendedMessage = contextMessage.ExtendedMessage + " Reference ID: " + StringManager.Get(ReferenceId);
	}

	public virtual void AttackWithCompleteLocal(Attack attack)
	{
	}

	public virtual DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (sourceItem is AuthoringTool)
		{
			DelayedActionInstance contextMessage = new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = "Author"
			};
			if (attack.IsCopy)
			{
				contextMessage = new DelayedActionInstance
				{
					Duration = 0.5f,
					ActionMessage = "Copy"
				};
				if (doAction)
				{
					InventoryManager.SpawnPrefab = SourcePrefab as DynamicThing;
				}
			}
			if (attack.IsDestroy)
			{
				Human human = this as Human;
				if ((bool)human && (bool)human.OrganBrain)
				{
					return null;
				}
				contextMessage = new DelayedActionInstance
				{
					Duration = 0.5f,
					ActionMessage = "Delete"
				};
				if (doAction)
				{
					Delete(attack.SourceItem);
				}
			}
			AddReferenceIDToContextualMessage(ref contextMessage);
			return contextMessage;
		}
		if (IsPaintable && sourceItem is ISprayer sprayer && !HasColorState)
		{
			return ISprayer.DoSpray(this, sprayer, doAction);
		}
		FireExtinguisher fireExtinguisher = attack.SourceItem as FireExtinguisher;
		if ((bool)fireExtinguisher)
		{
			DelayedActionInstance result = new DelayedActionInstance
			{
				Duration = 0.2f,
				ActionMessage = GameStrings.ActionExtinguish.DisplayString
			};
			if (!doAction)
			{
				return result;
			}
			if (fireExtinguisher.IsSuppressingFire)
			{
				ExtinguishSelfAndChildren();
			}
			return null;
		}
		Tablet tablet = attack.SourceItem as Tablet;
		if ((bool)tablet && (bool)tablet.Cartridge && !string.IsNullOrEmpty(tablet.Cartridge.ScanActionText))
		{
			DelayedActionInstance result2 = new DelayedActionInstance
			{
				Duration = tablet.ActionTime,
				ActionMessage = tablet.Cartridge.ScanActionText
			};
			if (!doAction)
			{
				return result2;
			}
			tablet.Scan(this);
		}
		return null;
	}

	public virtual void Delete(Thing sourceItem)
	{
		if (sourceItem == null)
		{
			return;
		}
		if (Slots.Count > 0)
		{
			foreach (Slot slot in Slots)
			{
				if ((bool)slot.Occupant && GameManager.RunSimulation)
				{
					slot.Occupant.Delete(sourceItem);
				}
			}
		}
		if (GameManager.RunSimulation)
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
		else
		{
			NetworkServer.SendToClients(new DestroyThingRequest
			{
				ThingId = netId,
				SourceItemId = sourceItem.NetworkId
			}, NetworkChannel.GeneralTraffic, -1L);
		}
	}

	public virtual async UniTaskVoid DestroyFromThread()
	{
		await UniTask.SwitchToMainThread();
		OnServer.Destroy(this);
	}

	public void CacheStates(bool cacheInteractables = false)
	{
		HasLockState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Lock);
		HasErrorState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Error);
		HasPowerState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Powered);
		HasOnOffState = Interactables.Exists((Interactable i) => i.Action == InteractableType.OnOff);
		HasModeState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Mode);
		HasOpenState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Open);
		HasActivateState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Activate);
		HasExportState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Export);
		HasImportState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Import);
		HasImport2State = Interactables.Exists((Interactable i) => i.Action == InteractableType.Import2);
		HasExport2State = Interactables.Exists((Interactable i) => i.Action == InteractableType.Export2);
		HasButton1State = Interactables.Exists((Interactable i) => i.Action == InteractableType.Button1);
		HasButton2State = Interactables.Exists((Interactable i) => i.Action == InteractableType.Button2);
		HasButton3State = Interactables.Exists((Interactable i) => i.Action == InteractableType.Button3);
		HasColorState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Color);
		HasAccessState = Interactables.Exists((Interactable i) => i.Action == InteractableType.Access);
		if (cacheInteractables)
		{
			_interactableColor = Interactables.Find((Interactable i) => i.Action == InteractableType.Color);
			_interactableActivate = Interactables.Find((Interactable i) => i.Action == InteractableType.Activate);
			_interactableOnOff = Interactables.Find((Interactable i) => i.Action == InteractableType.OnOff);
			_interactablePowered = Interactables.Find((Interactable i) => i.Action == InteractableType.Powered);
			_interactableError = Interactables.Find((Interactable i) => i.Action == InteractableType.Error);
			_interactableMode = Interactables.Find((Interactable i) => i.Action == InteractableType.Mode);
			_interactableLock = Interactables.Find((Interactable i) => i.Action == InteractableType.Lock);
			_interactableOpen = Interactables.Find((Interactable i) => i.Action == InteractableType.Open);
			_interactableImport = Interactables.Find((Interactable i) => i.Action == InteractableType.Import);
			_interactableImport2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Import2);
			_interactableExport = Interactables.Find((Interactable i) => i.Action == InteractableType.Export);
			_interactableExport2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Export2);
			_interactableAccess = Interactables.Find((Interactable i) => i.Action == InteractableType.Access);
			_interactableButton1 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button1);
			_interactableButton2 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button2);
			_interactableButton3 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button3);
			_interactableButton4 = Interactables.Find((Interactable i) => i.Action == InteractableType.Button4);
		}
	}

	private void SetupInteractables()
	{
		foreach (Interactable interactable in Interactables)
		{
			interactable.OriginalBounds = interactable.Bounds;
		}
	}

	public virtual void CachePrefabBounds()
	{
		ThingTransform = base.transform;
		Quaternion rotation = ThingTransform.rotation;
		Vector3 vector = ThingTransform.position;
		ThingTransform.rotation = Quaternion.identity;
		ThingTransformPosition = Vector3.zero;
		_ = Bounds;
		Bounds.center = Vector3.zero;
		Bounds.extents = Vector3.zero;
		if ((bool)(this as StaticDecal))
		{
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if (!collider.CompareTag("UIHelper"))
				{
					Bounds.Encapsulate(collider.bounds);
				}
			}
		}
		else
		{
			Renderer[] componentsInChildren2 = GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren2)
			{
				if (!renderer.CompareTag("UIHelper") && !renderer.CompareTag("ExcludeFromBounds"))
				{
					Bounds.Encapsulate(renderer.bounds);
				}
			}
		}
		ThingTransform.SetPositionAndRotation(vector, rotation);
		SurfaceArea = 2f * (Bounds.size.x * Bounds.size.y + Bounds.size.y * Bounds.size.z + Bounds.size.z * Bounds.size.x) * SurfaceAreaScale;
	}

	private void ConfigureSlots()
	{
		foreach (Slot slot in Slots)
		{
			slot.Parent = this;
			slot.Action = (InteractableType)Enum.Parse(typeof(InteractableType), "Slot" + (slot.SlotIndex + 1));
		}
	}

	public Slot GetFreeSlot(Slot.Class slotType)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == null && (slotType == slot.Type || slot.Type == Slot.Class.None))
			{
				return slot;
			}
		}
		return null;
	}

	public List<Slot> GetFreeSlots(Slot.Class slotType)
	{
		List<Slot> list = new List<Slot>(Slots.Count);
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == null && (slotType == slot.Type || slot.Type == Slot.Class.None))
			{
				list.Add(slot);
			}
		}
		return list;
	}

	public int GetFreeSlotCount()
	{
		int num = 0;
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == null)
			{
				num++;
			}
		}
		return num;
	}

	public Slot GetFreeSlot(Slot.Class slotType, List<InteractableType> excludeTypes)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == null && !excludeTypes.Contains(slot.Action) && (slotType == slot.Type || slot.Type == Slot.Class.None))
			{
				return slot;
			}
		}
		return null;
	}

	public Transform FindTransform(string name)
	{
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (!(base.transform == transform) && transform.name.ToLower().Contains(name.ToLower()))
			{
				return transform;
			}
		}
		return null;
	}

	public virtual void OnReagentUpdate()
	{
		if (this.OnReagentChanged != null)
		{
			this.OnReagentChanged();
		}
	}

	public virtual void OnNetworkCollision(Vector3 contactPoint, Vector3 relativeVelocity, Thing otherThing, float impulseMagnitude)
	{
	}

	public virtual void SetCustomColor(ColorSwatch colorSwatch)
	{
		SetCustomColor(colorSwatch.Index);
	}

	public virtual Material SelectColorSwatchMaterial(bool emissive)
	{
		if (!emissive)
		{
			return CustomColor.Normal;
		}
		return CustomColor.Emissive;
	}

	public virtual void SetCustomColor(int index, bool emissive = false)
	{
		if (!GameManager.IsValidColor(index))
		{
			return;
		}
		CustomColor = GameManager.GetColorSwatch(index);
		if (GameManager.RunSimulation)
		{
			NetworkUpdateFlags |= 32;
		}
		if (CustomColor == null)
		{
			return;
		}
		if (_customMaterials != null)
		{
			Material material = SelectColorSwatchMaterial(emissive);
			foreach (CustomColorMapping customMaterial in _customMaterials)
			{
				if (!customMaterial.ThingRenderer.GetRendererGameObject().CompareTag("NotPaintable"))
				{
					if (emissive)
					{
						customMaterial.SetEmissive(material);
					}
					else
					{
						customMaterial.SetColor(material, index);
					}
				}
			}
		}
		foreach (LensFlare lodFlare in lodFlares)
		{
			if ((bool)lodFlare)
			{
				lodFlare.color = CustomColor.Light;
			}
		}
		if (this.OnColorChange != null)
		{
			this.OnColorChange(CustomColor);
		}
		DiffuseIndex = index;
		EmissionColor = Color.white * (emissive ? 1f : 0f);
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.SetShaderVectorProperty(EMISSION_COLOR, EmissionColor);
			renderer.SetShaderFloatProperty(DiffuseIndexPropertyID, DiffuseIndex);
			renderer.SetShaderFloatProperty(SmoothnessIndexPropertyID, SmoothnessIndex);
		}
	}

	public virtual void SetCustomColor(bool emissive)
	{
		if ((object)CustomColor.Normal == null)
		{
			return;
		}
		foreach (CustomColorMapping customMaterial in _customMaterials)
		{
			if (!customMaterial.ThingRenderer.GetRendererGameObject().CompareTag("NotPaintable"))
			{
				if (emissive)
				{
					customMaterial.SetEmissive(CustomColor.Emissive);
				}
				else
				{
					customMaterial.SetColor(CustomColor.Normal, CustomColor.Index);
				}
			}
		}
	}

	public bool BoundsIntersectWith(Thing otherThing)
	{
		Vector3[] array = new Vector3[8];
		Vector3[] array2 = new Vector3[8];
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector = new Vector3((float)((i % 2 != 0) ? 1 : (-1)) * Bounds.extents.x, (float)((i / 2 % 2 != 0) ? 1 : (-1)) * Bounds.extents.y, (float)((i / 4 % 2 != 0) ? 1 : (-1)) * Bounds.extents.z);
			vector += Bounds.center;
			vector = Rotation * vector;
			vector += Position;
			array[num++] = vector;
		}
		Vector3 center = Bounds.center;
		center = Rotation * center;
		center += Position;
		num = 0;
		for (int j = 0; j < 8; j++)
		{
			Vector3 vector2 = new Vector3((float)((j % 2 != 0) ? 1 : (-1)) * otherThing.Bounds.extents.x, (float)((j / 2 % 2 != 0) ? 1 : (-1)) * otherThing.Bounds.extents.y, (float)((j / 4 % 2 != 0) ? 1 : (-1)) * otherThing.Bounds.extents.z);
			vector2 += otherThing.Bounds.center;
			vector2 = otherThing.Rotation * vector2;
			vector2 += otherThing.Position;
			array2[num++] = vector2;
		}
		Vector3 center2 = otherThing.Bounds.center;
		center2 = otherThing.Rotation * center2;
		center2 += otherThing.Position;
		if (_AreBoundingSpheresSeparate(center, array, center2, array2))
		{
			return false;
		}
		if (_Separated(array, array2, ThingTransform.right) || _Separated(array, array2, ThingTransform.up) || _Separated(array, array2, ThingTransform.forward) || _Separated(array, array2, otherThing.ThingTransform.right) || _Separated(array, array2, otherThing.ThingTransform.up) || _Separated(array, array2, otherThing.ThingTransform.forward) || _Separated(array, array2, Vector3.Cross(ThingTransform.right, otherThing.ThingTransform.right)) || _Separated(array, array2, Vector3.Cross(ThingTransform.right, otherThing.ThingTransform.up)) || _Separated(array, array2, Vector3.Cross(ThingTransform.right, otherThing.ThingTransform.forward)) || _Separated(array, array2, Vector3.Cross(ThingTransform.up, otherThing.ThingTransform.right)) || _Separated(array, array2, Vector3.Cross(ThingTransform.up, otherThing.ThingTransform.up)) || _Separated(array, array2, Vector3.Cross(ThingTransform.up, otherThing.ThingTransform.forward)) || _Separated(array, array2, Vector3.Cross(ThingTransform.forward, otherThing.ThingTransform.right)) || _Separated(array, array2, Vector3.Cross(ThingTransform.forward, otherThing.ThingTransform.up)) || _Separated(array, array2, Vector3.Cross(ThingTransform.forward, otherThing.ThingTransform.forward)))
		{
			return false;
		}
		return true;
	}

	private static bool _AreBoundingSpheresSeparate(Vector3 centerA, Vector3[] vertsA, Vector3 centerB, Vector3[] vertsB)
	{
		float num = float.MinValue;
		float num2 = float.MinValue;
		for (int i = 0; i < vertsA.Length; i++)
		{
			float sqrMagnitude = (vertsA[i] - centerA).sqrMagnitude;
			if (sqrMagnitude > num)
			{
				num = sqrMagnitude;
			}
		}
		for (int j = 0; j < vertsB.Length; j++)
		{
			float sqrMagnitude2 = (vertsB[j] - centerB).sqrMagnitude;
			if (sqrMagnitude2 > num2)
			{
				num2 = sqrMagnitude2;
			}
		}
		return (centerB - centerA).magnitude > Mathf.Sqrt(num) + Mathf.Sqrt(num2);
	}

	private static bool _Separated(Vector3[] vertsA, Vector3[] vertsB, Vector3 axis)
	{
		if (axis == Vector3.zero)
		{
			return false;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < vertsA.Length; i++)
		{
			float num5 = Vector3.Dot(vertsA[i], axis);
			num = ((num5 < num) ? num5 : num);
			num2 = ((num5 > num2) ? num5 : num2);
		}
		for (int j = 0; j < vertsB.Length; j++)
		{
			float num6 = Vector3.Dot(vertsB[j], axis);
			num3 = ((num6 < num3) ? num6 : num3);
			num4 = ((num6 > num4) ? num6 : num4);
		}
		float num7 = Mathf.Max(num2, num4) - Mathf.Min(num, num3);
		float num8 = num2 - num + num4 - num3;
		return num7 >= num8;
	}

	protected virtual void WriteTransform(RocketBinaryWriter writer)
	{
		writer.WriteVector3(ThingTransform ? ThingTransformPosition : Position);
		writer.WriteQuaternion(ThingTransform ? ThingTransform.rotation : Rotation);
	}

	public virtual void SerializeOnJoin(RocketBinaryWriter writer)
	{
		NetworkUpdateFlags |= ushort.MaxValue;
		DamageState.UpdateType |= DamageUpdateType.All;
		int num = ((CustomColor?.Normal == PaintableMaterial) ? (-1) : GameManager.GetColorIndex(CustomColor));
		Network.WritePackedId(writer, this);
		writer.WriteInt32(PrefabHash);
		WriteTransform(writer);
		writer.WriteString(CustomName);
		writer.WriteSByte((sbyte)num);
		SerializeInteractableOnJoin(writer);
		bool flag = ReagentMixture != null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			ReagentMixture.Write(writer);
		}
		bool indestructable = DamageState.Indestructable;
		writer.WriteBoolean(indestructable);
		if (indestructable)
		{
			IndestructableDamageState.Write(writer, DamageState);
		}
		writer.WriteSingle(EnergyConvected);
		writer.WriteSingle(EnergyRadiated);
	}

	private void SerializeInteractableOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		for (int i = 0; i < Interactables.Count; i++)
		{
			Interactable interactable = Interactables[i];
			if (interactable.IsValidToSend())
			{
				writer.WriteByte((byte)i);
				WriteInteractableState(writer, interactable);
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public virtual void DeserializeOnJoin(RocketBinaryReader reader)
	{
		string text = reader.ReadString();
		sbyte b = reader.ReadSByte();
		if (!string.IsNullOrEmpty(text))
		{
			RenameThing(text);
		}
		if (b >= 0)
		{
			SetCustomColor(b);
		}
		DeserializeInteractableOnJoin(reader);
		if (reader.ReadBoolean())
		{
			if (ReagentMixture == null)
			{
				ReagentMixture = new ReagentMixture();
			}
			ReagentMixture.Read(reader);
		}
		if (reader.ReadBoolean())
		{
			IndestructableDamageState.Read(reader, DamageState);
		}
		EnergyConvected = reader.ReadSingle();
		EnergyRadiated = reader.ReadSingle();
	}

	protected virtual int ReadInteractableState(RocketBinaryReader reader, Interactable interactable)
	{
		if (interactable.Action == InteractableType.Access)
		{
			return reader.ReadInt16();
		}
		return reader.ReadSByte();
	}

	protected virtual void SetInteractableStateOnJoin(Interactable interactable, int state)
	{
		interactable.State = state;
		interactable.SetState();
	}

	private void DeserializeInteractableOnJoin(RocketBinaryReader reader)
	{
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			byte index = reader.ReadByte();
			Interactable interactable = Interactables[index];
			int state = ReadInteractableState(reader, interactable);
			SetInteractableStateOnJoin(interactable, state);
			OnFinishedInteractionSync(interactable);
		}
	}

	public static void ClearAll()
	{
		foreach (Thing item in OcclusionManager.AllThings.ToList())
		{
			UnityEngine.Object.Destroy(item.GameObject);
		}
		Device.AllDevices.Clear();
		TotalThingsToSpawn = 0;
		TotalThings = 0;
		OcclusionManager.ClearAll();
		LogicStack.ClearAll();
		PhysicalPoolActive.Clear();
		WaypointManager.Clear();
		AllILights.Clear();
		AllIWearableLights.Clear();
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		if (ReferenceId == 0L)
		{
			throw new Exception("Tried to send ThingData to client with a referenceId of 0!");
		}
		SerializeOnJoin(writer);
	}

	public static void DeserializeDestroy(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Thing thing = Find(referenceId);
		if ((object)thing != null)
		{
			thing.BeingDestroyed = true;
			UnityEngine.Object.Destroy(thing.GameObject);
		}
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		int prefabHash = reader.ReadInt32();
		Vector3 transformPosition = reader.ReadVector3();
		Quaternion quaternion = reader.ReadQuaternion();
		Thing thing = Create<Thing>(prefabHash, transformPosition, quaternion, referenceId);
		thing.DeserializeOnJoin(reader);
		thing.SnapTransform(transformPosition, quaternion);
	}

	public virtual void SnapTransform(Vector3 transformPosition, Quaternion transformRotation)
	{
		Transform.SetPositionAndRotation(transformPosition, transformRotation);
		ThingTransformPosition = transformPosition;
		ThingTransformRotation = transformRotation;
		WorldGrid = new WorldGrid(CenterPosition);
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Thing current = enumerator.Current;
			if ((bool)current && current.ReferenceId != 0L && !current.IsBeingDestroyed && current.IsNetworkUpdate())
			{
				ushort networkUpdateType = current.NetworkUpdateFlags;
				VerifyUpdateType(current, ref networkUpdateType);
				Network.WritePackedId(writer, current);
				writer.WriteNetworkUpdateType(networkUpdateType);
				current.BuildUpdate(writer, networkUpdateType);
				current.NetworkUpdateFlags = 0;
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		string arg = "None";
		Network.ReadIndex<uint>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			ushort networkUpdateType = reader.ReadNetworkUpdateType();
			Thing thing = Referencable.Find<Thing>(referenceId);
			if ((object)thing != null)
			{
				arg = thing.DisplayName;
			}
			if ((object)thing == null)
			{
				throw new NullReferenceException($"Error: thing number: {i} is null during network update. ReferenceId: {referenceId}. Last valid thing was {arg}");
			}
			thing.ProcessUpdate(reader, networkUpdateType);
		}
	}

	public static void VerifyUpdateType(Thing thing, ref ushort networkUpdateType)
	{
		if (IsNetworkUpdateRequired(8u, networkUpdateType) && thing.ReagentMixture == null)
		{
			networkUpdateType ^= 8;
		}
	}

	public static void TestByteArrayProcessing(Thing thing)
	{
	}

	public virtual bool IsNetworkUpdate()
	{
		return NetworkUpdateFlags != 0;
	}

	public virtual void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		if (IsNetworkUpdateRequired(1u, networkUpdateType))
		{
			BuildUpdateTransform(writer);
		}
		BuildInteractableUpdate(writer, networkUpdateType);
		if (IsNetworkUpdateRequired(4u, networkUpdateType))
		{
			IndestructableDamageState.Write(writer, DamageState);
		}
		if (IsNetworkUpdateRequired(8u, networkUpdateType))
		{
			ReagentMixture.Write(writer);
		}
		if (IsNetworkUpdateRequired(16u, networkUpdateType))
		{
			writer.WriteBoolean(IsBurning);
		}
		if (IsNetworkUpdateRequired(32u, networkUpdateType))
		{
			int colorIndex = GameManager.GetColorIndex(CustomColor);
			writer.WriteInt32(colorIndex);
			writer.WriteString(CustomName);
		}
		if (IsNetworkUpdateRequired(128u, networkUpdateType))
		{
			writer.WriteSingle(EnergyConvected);
			writer.WriteSingle(EnergyRadiated);
		}
	}

	protected virtual void WriteInteractableState(RocketBinaryWriter writer, Interactable interactable)
	{
		if (interactable.Action == InteractableType.Access)
		{
			writer.WriteInt16((short)interactable.State);
		}
		else
		{
			writer.WriteSByte((sbyte)interactable.State);
		}
	}

	private void BuildInteractableUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		if (!IsNetworkUpdateRequired(2u, networkUpdateType))
		{
			return;
		}
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		for (byte b = 0; b < Interactables.Count; b++)
		{
			Interactable interactable = Interactables[b];
			if (interactable.IsDirty)
			{
				writer.WriteByte(b);
				WriteInteractableState(writer, interactable);
				interactable.IsDirty = false;
				if (count >= byte.MaxValue)
				{
					throw new Exception($"InteractableCount exceeds: {byte.MaxValue}");
				}
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static bool IsNetworkUpdateRequired(uint toCheck, ushort networkUpdateType)
	{
		return (toCheck & networkUpdateType) != 0;
	}

	public virtual void BuildOwnerUpdate(RocketBinaryWriter writer)
	{
	}

	public virtual void ProcessOwnerUpdate(RocketBinaryReader reader)
	{
	}

	public virtual void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		if (IsNetworkUpdateRequired(1u, networkUpdateType))
		{
			ProcessUpdateTransform(reader);
		}
		ProcessInteractableUpdate(reader, networkUpdateType);
		if (IsNetworkUpdateRequired(4u, networkUpdateType))
		{
			IndestructableDamageState.Read(reader, DamageState);
		}
		if (IsNetworkUpdateRequired(8u, networkUpdateType))
		{
			if (ReagentMixture == null)
			{
				ReagentMixture = new ReagentMixture();
			}
			ReagentMixture.Read(reader);
		}
		if (IsNetworkUpdateRequired(16u, networkUpdateType))
		{
			IsBurning = reader.ReadBoolean();
		}
		if (IsNetworkUpdateRequired(32u, networkUpdateType))
		{
			int index = reader.ReadInt32();
			SetCustomColor(index);
			string newName = reader.ReadString();
			RenameThing(newName);
		}
		if (IsNetworkUpdateRequired(128u, networkUpdateType))
		{
			EnergyConvected = reader.ReadSingle();
			EnergyRadiated = reader.ReadSingle();
		}
	}

	private void ProcessInteractableUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		if (IsNetworkUpdateRequired(2u, networkUpdateType))
		{
			byte b = reader.ReadByte();
			for (int i = 0; i < b; i++)
			{
				byte index = reader.ReadByte();
				Interactable interactable = Interactables[index];
				int state = ReadInteractableState(reader, interactable);
				interactable.Interact(state, skipAnimation: false);
			}
		}
	}

	protected virtual void BuildUpdateTransform(RocketBinaryWriter writer)
	{
		writer.WriteVector3(Position);
		writer.WriteQuaternion(Rotation);
	}

	protected virtual void ProcessUpdateTransform(RocketBinaryReader reader)
	{
		Vector3 vector = reader.ReadVector3();
		Quaternion rotation = reader.ReadQuaternion();
		if (!HasAuthority)
		{
			ThingTransform.SetPositionAndRotation(vector, rotation);
		}
	}

	public virtual void OnFinishJoin()
	{
	}

	public void Add(PooledAudioSource gameAudioSource)
	{
		PooledAudioSources.Add(gameAudioSource);
	}

	public void Remove(PooledAudioSource gameAudioSource)
	{
		PooledAudioSources.Remove(gameAudioSource);
	}

	public static void SerializeAllOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Thing current = enumerator.Current;
			if (!current || current.IsBeingDestroyed)
			{
				continue;
			}
			if (current.ReferenceId == 0L)
			{
				ConsoleWindow.PrintError("error " + current.DisplayName + " has no reference id, skipping and continuing");
			}
			else if (!(current.RootParent != current))
			{
				try
				{
					NetworkServer.Serialize(writer, current, ref count);
				}
				catch (Exception ex)
				{
					ConsoleWindow.PrintError($"fatal error serializing '{current.DisplayName}' #{current.ReferenceId} at index {count} for join package");
					ConsoleWindow.Print(ex.ToString());
				}
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
		ConsoleWindow.Print($"serialized {count} things for join package");
	}

	public Slot GetNextFreeSlot(Thing prefab)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.IsEmpty() && slot.IsAllowedType(prefab as DynamicThing))
			{
				return slot;
			}
		}
		return null;
	}

	public Slot GetNextFreeSlot()
	{
		foreach (Slot slot in Slots)
		{
			if (slot.IsEmpty())
			{
				return slot;
			}
		}
		return null;
	}

	public virtual Slot GetSlot(int slotIndex)
	{
		return Slots[slotIndex];
	}

	public Slot GetNextFreeSlot(string slotKey)
	{
		int num = Animator.StringToHash(slotKey);
		foreach (Slot slot in Slots)
		{
			if (!slot.IsNotEmpty() && slot.StringHash == num)
			{
				return slot;
			}
		}
		return null;
	}

	public Slot GetNextFreeSlot(Slot.Class slotKey)
	{
		foreach (Slot slot in Slots)
		{
			if (!slot.IsNotEmpty() && slot.Type == slotKey)
			{
				return slot;
			}
		}
		return null;
	}

	public virtual void CachePositionOnSpawn()
	{
		Position = base.gameObject.transform.position;
		Rotation = base.gameObject.transform.rotation;
	}
}
