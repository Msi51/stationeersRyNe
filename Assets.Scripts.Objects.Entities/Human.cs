using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Emotes;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Objects.Rockets;
using Reagents;
using TerrainSystem.Lods;
using Trading;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using Util;

namespace Assets.Scripts.Objects.Entities;

public sealed class Human : Entity, ITradableInventory, IReferencable, IEvaluable, IGenerateMinables, ILightActivated, IDensePoolable
{
	public delegate void OnHumanCreatedEvent(Entity entity);

	public const float RENDER_DISTANCE = 100f;

	public const float SHADOW_DISTANCE = 10f;

	public HumanHandsBehaviour HumanHandsBehaviour;

	public ThrowItemBehaviour ThrowItemBehaviour;

	public static readonly List<Human> AllHumans = new List<Human>();

	[NonSerialized]
	public Slot StomachSlot;

	[ReadOnly]
	public Stomach OrganStomach;

	private static float PowerDrainedPerTick = 100f;

	[NonSerialized]
	public Slot SuitSlot;

	[NonSerialized]
	public Slot HelmetSlot;

	[NonSerialized]
	public Slot GlassesSlot;

	[NonSerialized]
	public Slot BackpackSlot;

	[NonSerialized]
	public Slot LeftHandSlot;

	[NonSerialized]
	public Slot RightHandSlot;

	[NonSerialized]
	public Slot UniformSlot;

	[NonSerialized]
	public Slot ToolbeltSlot;

	[Header("Human")]
	public static readonly float GroundedRadiusScale = 0.99f;

	private readonly int _velocityHash = Animator.StringToHash("Velocity");

	private readonly int _jetPackHash = Animator.StringToHash("JetPack");

	private readonly int _groundedHash = Animator.StringToHash("Grounded");

	private readonly int _jumpHash = Animator.StringToHash("Jump");

	private readonly int _jumpLandHash = Animator.StringToHash("JumpLand");

	private readonly int _robotJumpHash = Animator.StringToHash("RobotJump");

	private readonly int _robotJumpLandHash = Animator.StringToHash("RobotJumpLand");

	private readonly int _footStepLeftHash = Animator.StringToHash("FootStepLeft");

	private readonly int _footStepRightHash = Animator.StringToHash("FootStepRight");

	private readonly int _vHash = Animator.StringToHash("V");

	private readonly int _hHash = Animator.StringToHash("H");

	private readonly int _strafeNotify = 1;

	private readonly float _range = 1f;

	private readonly float _threshold = 0.01f;

	private readonly float _velocityMax = 4f;

	private bool _jump;

	private bool _grounded;

	private static readonly float FootStepSoundCoolDown = 0.25f;

	private float _lastFootStepTime;

	private readonly int _armLHash = Animator.StringToHash("ArmL");

	private readonly int _armRHash = Animator.StringToHash("ArmR");

	private readonly int _legLHash = Animator.StringToHash("LegL");

	private readonly int _legRHash = Animator.StringToHash("LegR");

	private readonly int _toolBeltHash = Animator.StringToHash("ToolBelt");

	private readonly int _legLRobotHash = Animator.StringToHash("LegLRobot");

	private readonly int _legRRobotHash = Animator.StringToHash("LegRRobot");

	private readonly int _spineRobotHash = Animator.StringToHash("SpineRobot");

	public AnimationCurve ArmHumanVolumeCurve;

	public AnimationCurve LegHumanVolumeCurve;

	public AnimationCurve LegRobotVolumeCurve;

	public AnimationCurve ToolBeltCurve;

	public AnimationCurve SpineCurve;

	private Vector3 _armLPos;

	private Vector3 _armRPos;

	private Vector3 _legLPos;

	private Vector3 _legRPos;

	private Vector3 _hipsPos;

	private Vector3 _hipsVel;

	private float _armLVol;

	private float _armRVol;

	private float _legLVol;

	private float _legRVol;

	private float _hipsVol;

	private float _spineVol;

	private float _foleyLerpSpeed = 2f;

	private float RobotLerpSpeed = 6f;

	private BreathingState PreviousBreathingState;

	private BreathingState BreathingState;

	private float _stress;

	public float Exertion;

	private static readonly float ReductionRate = 0.1f;

	private static readonly float StressedThreshold = 0.1f;

	private static readonly float ExertionThreshold = 0.7f;

	private static readonly float RelaxedThreshold = 0.2f;

	private static readonly PressurekPa NoBreathingThreshold = new PressurekPa(0.5);

	private static readonly float ExertionRate = 0.1f;

	private static readonly float MovementThreshold = 0.35f;

	private static readonly float BreathlessnessReductionRate = 0.3f;

	private static readonly float OxygenQualityThreshold = 0.5f;

	private static readonly float LungHealthThreshold = 0.3f;

	private float _breathlessness;

	private bool _breathIn;

	private GameAudioSource _breathAudioSource;

	private static readonly int BreathInRelaxedHash = Animator.StringToHash("BreathIn_Relaxed");

	private static readonly int BreathOutRelaxedHash = Animator.StringToHash("BreathOut_Relaxed");

	private static readonly int BreathInToExertionHash = Animator.StringToHash("BreathIn_ToExertion");

	private static readonly int BreathOutToExertionHash = Animator.StringToHash("BreathOut_ToExertion");

	private static readonly int BreathInExertionHash = Animator.StringToHash("BreathIn_Exertion");

	private static readonly int BreathOutExertionHash = Animator.StringToHash("BreathOut_Exertion");

	private static readonly int BreathInToRelaxedHash = Animator.StringToHash("BreathIn_ToRelaxed");

	private static readonly int BreathOutToRelaxedHash = Animator.StringToHash("BreathOut_ToRelaxed");

	private static readonly int BreathInStressedHash = Animator.StringToHash("BreathIn_Stressed");

	private static readonly int BreathOutStressedHash = Animator.StringToHash("BreathOut_Stressed");

	private static readonly int BreathInLowPressureHash = Animator.StringToHash("BreathIn_LowPressure");

	private static readonly int BreathOutLowPressureHash = Animator.StringToHash("BreathOut_LowPressure");

	private static readonly int BreathInFirstBreathHash = Animator.StringToHash("BreathIn_1stBreath");

	private static readonly int BreathOutFirstBreathHash = Animator.StringToHash("BreathOut_1stBreath");

	private static readonly int BreathInToxinHash = Animator.StringToHash("BreathIn_Toxin");

	private static readonly int BreathOutToxinHash = Animator.StringToHash("BreathOut_Toxin");

	private static readonly int BreathInJumpHash = Animator.StringToHash("BreathIn_Jump");

	private static readonly int BreathOutJumpHash = Animator.StringToHash("BreathOut_Jump");

	private static readonly int BreathInJumpLandHash = Animator.StringToHash("BreathIn_JumpLand");

	private static readonly int BreathOutJumpLandHash = Animator.StringToHash("BreathOut_JumpLand");

	private static readonly int FemaleBreathInRelaxedHash = Animator.StringToHash("FemaleBreathIn_Relaxed");

	private static readonly int FemaleBreathOutRelaxedHash = Animator.StringToHash("FemaleBreathOut_Relaxed");

	private static readonly int FemaleBreathInToExertionHash = Animator.StringToHash("FemaleBreathIn_ToExertion");

	private static readonly int FemaleBreathOutToExertionHash = Animator.StringToHash("FemaleBreathOut_ToExertion");

	private static readonly int FemaleBreathInExertionHash = Animator.StringToHash("FemaleBreathIn_Exertion");

	private static readonly int FemaleBreathOutExertionHash = Animator.StringToHash("FemaleBreathOut_Exertion");

	private static readonly int FemaleBreathInToRelaxedHash = Animator.StringToHash("FemaleBreathIn_ToRelaxed");

	private static readonly int FemaleBreathOutToRelaxedHash = Animator.StringToHash("FemaleBreathOut_ToRelaxed");

	private static readonly int FemaleBreathInStressedHash = Animator.StringToHash("FemaleBreathIn_Stressed");

	private static readonly int FemaleBreathOutStressedHash = Animator.StringToHash("FemaleBreathOut_Stressed");

	private static readonly int FemaleBreathInLowPressureHash = Animator.StringToHash("FemaleBreathIn_LowPressure");

	private static readonly int FemaleBreathOutLowPressureHash = Animator.StringToHash("FemaleBreathOut_LowPressure");

	private static readonly int FemaleBreathInFirstBreathHash = Animator.StringToHash("FemaleBreathIn_1stBreath");

	private static readonly int FemaleBreathOutFirstBreathHash = Animator.StringToHash("FemaleBreathOut_1stBreath");

	private static readonly int FemaleBreathInJumpHash = Animator.StringToHash("FemaleBreathIn_Jump");

	private static readonly int FemaleBreathOutJumpHash = Animator.StringToHash("FemaleBreathOut_Jump");

	private static readonly int FemaleBreathInJumpLandHash = Animator.StringToHash("FemaleBreathIn_JumpLand");

	private static readonly int FemaleBreathOutJumpLandHash = Animator.StringToHash("FemaleBreathOut_JumpLand");

	private static readonly int FemaleBreathInToxinHash = Animator.StringToHash("FemaleBreathIn_Toxin");

	private static readonly int FemaleBreathOutToxinHash = Animator.StringToHash("FemaleBreathOut_Toxin");

	private bool _isBreathingAudio;

	private float _lungsLastDamageEfficiency;

	private bool _pauseFoleyAudio;

	private bool _isPlayingArmL;

	private bool _isPlayingArmR;

	private bool _isPlayingLegL;

	private bool _isPlayingLegR;

	private bool _isPlayingToolBelt;

	private bool _isPlayingSpine;

	public LayerMask AllButPlayerImmune;

	public LayerMask AllButPlayer;

	public Material BaseMaterial;

	public Transform AimIk;

	public MeshFilter HelmetShadowMesh;

	public DynamicSkeleton DynamicSkeleton;

	public ChatCanvas ChatPanel;

	public bool UnlimitedPower;

	public bool UnlimitedGas;

	public AudioReverbZone ReverbZone;

	public EmoteController EmoteController;

	[ReadOnly]
	[Tooltip("Current skin head shadow")]
	public HeadShadowInfo HeadShadow;

	[ReadOnly]
	[Tooltip("Current hair shadow")]
	public HeadShadowInfo HairShadow;

	[ReadOnly]
	[Tooltip("Current back shadow")]
	public HeadShadowInfo BackShadow;

	private Transform _leftEye;

	private Transform _rightEye;

	public Animator HeadAnimator;

	public static readonly int UnconsciousHash = Animator.StringToHash("Unconscious");

	public bool LockedToSeat;

	private RaycastHit _ikHit;

	[ReadOnly]
	[SerializeField]
	public List<LeakReference> LeakReferences = new List<LeakReference>();

	public PlayerCosmetics CosmeticData = new PlayerCosmetics();

	public PlayerCosmeticsBehaviour CosmeticsBehaviour;

	private float _gForce;

	private bool _isAllowedToSetCosmetics;

	private bool _wearableScheduled;

	private static readonly float _hydrationLossPerTick = 0.75f * (5f / GameManager.TicksPerThirtyMinutes);

	private static readonly int scaleRate = 100;

	private const float MOOD_MODIFIER_THRESHOLD = 0.25f;

	private const float MOOD_REDUCTION_BASE = 0.00013888889f;

	private const float MOOD_RECOVERY_BASE = 0.0016666667f;

	private const float ROOM_MOOD_RECOVERY = 0.00083333335f;

	private const float HYGIENE_REDUCTION_BASE = 4.6296296E-05f;

	private const float HYGIENE_RECOVERY_BASE = 0.00041666668f;

	private const float SOILED_HYGIENE_REDUCTION = 0.05f;

	private const float MOOD_NUTRITION_MULTIPLIER = 1.1f;

	private static int DiggingHash = Animator.StringToHash("Digging");

	private readonly int _expressionDeadIdHash = Animator.StringToHash("ExpressionDead".ToLower());

	private readonly int _expressionNeutralIdHash = Animator.StringToHash("ExpressionNeutral".ToLower());

	[Tooltip("Velocity absorbed by the body (excluding suit)")]
	public float VelocityAbsorbed = 12f;

	[ReadOnly]
	public float Invulnerability;

	private int _damageSuitHash = Animator.StringToHash("DamageSuit");

	private int _damageHelmetHash = Animator.StringToHash("DamageHelmet");

	public NightVisionGoggles CurrentNightVision;

	public static bool CurrentlyUsingNightVision;

	public static readonly int RESPAWN_PROMPT_DISPLAY_DELAY = 5000;

	public static readonly string UnconsciousPromptTitle = "UnconsciousPromptTitle";

	public static readonly string UnconsciousPromptMessage = "UnconsciousPromptMessage";

	public static readonly string UnconsciousPromptOptionWaitForHelp = "UnconsciousPromptOptionWaitForHelp";

	public static readonly string UnconsciousPromptAskForHelp = "UnconsciousPromptAskForHelp";

	public static readonly string UnconsciousPromptOptionGiveUp = "UnconsciousPromptOptionGiveUp";

	public static readonly string UnconsciousGiveUpPromptMessage = "UnconsciousGiveUpPromptMessage";

	public static readonly string DeadPromptTitle = "DeadPromptTitle";

	public static readonly string DeadPromptMessage = "DeadPromptMessage";

	public static readonly string DeadPromptOptionWaitForHelp = "DeadPromptOptionWaitForHelp";

	public static readonly string DeadPromptAskForHelp = "DeadPromptAskForHelp";

	public static readonly string DeadPromptOptionGiveUp = "DeadPromptOptionGiveUp";

	public static readonly string DeadGiveUpPromptMessage = "DeadGiveUpPromptMessage";

	public static readonly string GiveUpPromptTitle = "GiveUpPromptTitle";

	public static readonly string GiveUpPromptOptionGiveUp = "GiveUpPromptOptionGiveUp";

	public static readonly string GiveUpPromptOptionCancel = "GiveUpPromptOptionCancel";

	public Vector3 AimIkTarget;

	public static CancellationTokenWrapper ClientRequestCharacter = new CancellationTokenWrapper();

	private bool _createStomachOnFinishedLoad;

	public List<MedicalEffectBase> MedicalEffects = new List<MedicalEffectBase>(5);

	private Vector3 _controlPosition;

	public const float N2O_PARTIAL_PRESSURE_HUMAN = 5f;

	public const float N2O_PARTIAL_PRESSURE_ZRILIAN = 16f;

	public static PressurekPa SafeN2OPartialPressureHuman = new PressurekPa(5.0);

	public static PressurekPa SafeN2OPartialPressureZrilian = new PressurekPa(16.0);

	public const float GFORCE_HIGH = 1.5f;

	public const float GFORCE_MAX = 4f;

	public const float GFORCE_STUN_SCALE = 3f;

	public const float GFORCE_OVERMAX_STUN = 2f;

	private static int BodyBagHash = Animator.StringToHash("DynamicBodyBag");

	private static int BoxHash = Animator.StringToHash("CardboardBoxLarge");

	private static readonly float MovementAnimationThreshold = 0.5f;

	private bool _updatedLastAnimation;

	private float _animateRate = 25f;

	private int _gridX = -10000;

	private int _gridY = -10000;

	private int _gridZ = -10000;

	private const float MIN_LERP_RATE = 0.25f;

	private bool _returnToPlayableAreaMessageOnCooldown;

	private static readonly Bounds _humanBounds = new Bounds(Vector3.zero, new Vector3(0.5f, 0.5f, 1.1f));

	[Tooltip("Head Bone used For IK HeadLook")]
	public Transform HeadBone;

	[Tooltip("Spine Bone used For IK SpineBend")]
	public List<Transform> SpineBones;

	public Transform[] HandBones;

	private static readonly Vector3 IkAxis = -Vector3.right + Vector3.up * 1.5f;

	private static readonly Vector3 HandAxis = new Vector3(-1f, 0.5f, 0f);

	private float _headWeight = 0.3f;

	private float _bodyWeight = 0.5f;

	private float _handWeight = 1f;

	private float _ikPositionWeight = 0.7f;

	public const float _seatedHeadWeight = 0.9f;

	private float _timeUntilClamp = 1f;

	private float _lastKinematicUpdateTime;

	private BatteryCell _robotBattery;

	private int _daysOfPotatoOnly;

	private Recipe _potatoOnly = new Recipe(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0);

	public Vector3Int MinablesGenerationRange
	{
		get
		{
			if (!(InventoryManager.ParentHuman == this))
			{
				return GameConstants.MINABLES_GENERATION_RANGE_CLIENT;
			}
			return GameConstants.MINABLES_GENERATION_RANGE;
		}
	}

	public Vector3 GeneratePosition => base.Position;

	public Vector3 PreviousMinableRequestPosition { get; set; }

	public bool ShouldGenerate => true;

	public override bool ShouldRender => OrganBrain?.LocalControl ?? false;

	public override LodInfo LodInfo
	{
		get
		{
			if (!ShouldRender)
			{
				return LodManager.ThingLodInfo;
			}
			return LodManager.PlayerLodInfo;
		}
	}

	public static Human LocalHuman => InventoryManager.ParentHuman;

	public static int LayerDefault => LayerMask.NameToLayer("Default");

	public static int LayerPlayer => LayerMask.NameToLayer("Player");

	public bool ShowUniform => !SuitSlot.Contains<IFullBody>();

	public bool Jump
	{
		get
		{
			return _jump;
		}
		set
		{
			if (value && !_jump)
			{
				PlaySound((SpeciesClass == SpeciesClass.Robot) ? _robotJumpHash : _jumpHash);
				PlayJumpBreathAudio(jump: true);
			}
			else if (!value && _jump && _grounded)
			{
				PlaySound((SpeciesClass == SpeciesClass.Robot) ? _robotJumpLandHash : _jumpLandHash);
				PlayJumpBreathAudio(jump: false);
			}
			_jump = value;
		}
	}

	public bool Grounded
	{
		get
		{
			return _grounded;
		}
		set
		{
			if (value && !_grounded)
			{
				PlaySound((SpeciesClass == SpeciesClass.Robot) ? _robotJumpLandHash : _jumpLandHash);
				PlayJumpBreathAudio(jump: false);
			}
			_grounded = value;
		}
	}

	public float Stress
	{
		get
		{
			return _stress;
		}
		set
		{
			_stress = value;
			if (_stress > 0f)
			{
				Exertion = 1f;
			}
		}
	}

	public float Breathlessness
	{
		get
		{
			return _breathlessness;
		}
		set
		{
			_breathlessness = value;
			if (_breathlessness > 0f)
			{
				Exertion = 1f;
			}
		}
	}

	public bool PauseFoleyAudio
	{
		get
		{
			return _pauseFoleyAudio;
		}
		set
		{
			if (value == _pauseFoleyAudio)
			{
				return;
			}
			if (value)
			{
				TriggerFoleyAudio(HumanBodyBones.LeftUpperArm, 0f, 0.5f);
				TriggerFoleyAudio(HumanBodyBones.RightUpperArm, 0f, 0.5f);
				TriggerFoleyAudio(HumanBodyBones.LeftUpperLeg, 0f, 0.5f);
				TriggerFoleyAudio(HumanBodyBones.RightUpperLeg, 0f, 0.5f);
				TriggerFoleyAudio(HumanBodyBones.Hips, 0f, 0.5f);
				if (HasAuthority && SpeciesClass == SpeciesClass.Robot)
				{
					TriggerFoleyAudio(HumanBodyBones.Spine, 0f, 0.65f);
				}
			}
			_pauseFoleyAudio = value;
		}
	}

	public override int GetAccess
	{
		get
		{
			int num = base.GetAccess;
			if ((bool)Uniform)
			{
				num |= Uniform.GetAccess;
			}
			if ((bool)LeftHandSlot.Occupant)
			{
				num |= LeftHandSlot.Occupant.GetAccess;
			}
			if ((bool)RightHandSlot.Occupant)
			{
				num |= RightHandSlot.Occupant.GetAccess;
			}
			return num;
		}
	}

	public bool IsMale => Gender == Gender.Male;

	public override Human RootParentHuman
	{
		get
		{
			if (!base.IsChild)
			{
				return this;
			}
			return base.RootParentHuman;
		}
	}

	public bool InternalsLocked => (bool)HelmetSlot.Occupant & HelmetSlot.Occupant.IsLocked;

	public override float MaxMovementSpeed
	{
		get
		{
			if (!BackpackSlot.Contains<Jetpack>(out var occupant) || occupant.Activate != 1)
			{
				return 20f;
			}
			return occupant.JetPackSpeed;
		}
	}

	public bool ShadowRenderersEnabled
	{
		set
		{
			if (HeadShadow != null && HeadShadow.Renderer != null)
			{
				HeadShadow.Renderer.enabled = value;
			}
			if (HairShadow != null && HairShadow.Renderer != null)
			{
				HairShadow.Renderer.enabled = value;
			}
			if (BackShadow != null && BackShadow.Renderer != null)
			{
				BackShadow.Renderer.enabled = value;
			}
		}
	}

	public SpeciesClass SpeciesClass => CosmeticData.SpeciesClass;

	private Gender Gender => CosmeticData.Gender;

	public ISuit Suit { get; private set; }

	public NightVisionGoggles GlassesAsNightVision => GlassesSlot.Occupant as NightVisionGoggles;

	public GasMask HeadAsSpaceHelmet { get; private set; }

	public Hat HeadAsHat { get; private set; }

	public Uniform Uniform { get; private set; }

	public BatteryCell RobotBattery
	{
		get
		{
			if ((object)_robotBattery == null || _robotBattery.IsEmpty)
			{
				if (LeftHandSlot.Contains<BatteryCell>(out var occupant) && !occupant.IsEmpty)
				{
					return occupant;
				}
				if (RightHandSlot.Contains<BatteryCell>(out var occupant2) && !occupant2.IsEmpty)
				{
					return occupant2;
				}
			}
			return _robotBattery;
		}
		private set
		{
			_robotBattery = value;
		}
	}

	public override bool IsArtificial => SpeciesClass == SpeciesClass.Robot;

	public override float BreathingEfficiency
	{
		get
		{
			if (IsArtificial)
			{
				return 1f;
			}
			if (!(OrganLungs != null))
			{
				return 0f;
			}
			return OrganLungs.AtmosphericEfficiency * OrganLungs.DamageEfficiency;
		}
	}

	public static MoleQuantity MolesPerMinute => new MoleQuantity(0.0048f / AtmosphericsManager.Instance.TickSpeedSeconds * 60f);

	public override Atmosphere BreathingAtmosphere
	{
		get
		{
			if (!HasInternals || !InternalsOn)
			{
				return base.BreathingAtmosphere;
			}
			return HelmetSlot.Occupant.InternalAtmosphere;
		}
		protected set
		{
			if (HasInternals && InternalsOn && HelmetSlot.Contains<IInternalAtmosphere>(out var occupant))
			{
				occupant.InternalAtmosphere = value;
			}
			else
			{
				base.WorldAtmosphere = value;
			}
		}
	}

	public float GForce => _gForce;

	public override Atmosphere LungAtmosphere => OrganLungs?.InternalAtmosphere;

	protected override Atmosphere SoilingAtmosphere
	{
		get
		{
			ISuit suit = Suit;
			if (!(suit?.AsThing) || suit.InternalAtmosphere == null)
			{
				return base.SoilingAtmosphere;
			}
			return suit.InternalAtmosphere;
		}
	}

	public bool InternalsOn
	{
		get
		{
			GasMask headAsSpaceHelmet = HeadAsSpaceHelmet;
			if ((object)headAsSpaceHelmet == null)
			{
				return false;
			}
			return !headAsSpaceHelmet.IsOpen;
		}
		private set
		{
			if (HeadAsSpaceHelmet != null)
			{
				Thing.Interact(HelmetSlot.Occupant, InteractableType.Open, (!value) ? 1 : 0);
			}
		}
	}

	public bool HasInternals
	{
		get
		{
			if (HeadAsSpaceHelmet != null)
			{
				return Suit != null;
			}
			return false;
		}
	}

	public override bool IsBurnable => false;

	public override float BaseNutritionStorage => 50f;

	public bool HygieneLow => base.Hygiene <= 0f;

	public bool HygieneOk => base.Hygiene > 0.25f;

	public float SuitLossVelocityRatio
	{
		get
		{
			if (!(Suit?.AsThing))
			{
				return 1f;
			}
			return Suit.SuitVelocityLeakRatio;
		}
	}

	public float MinSuitDamageVelocity
	{
		get
		{
			if (!(Suit?.AsThing))
			{
				return VelocityAbsorbed;
			}
			return Suit.SuitVelocityAbsorbed;
		}
	}

	public float SuitDamageVelocityScale
	{
		get
		{
			if (!(Suit?.AsThing))
			{
				return 1f;
			}
			return Suit.SuitVelocityScale;
		}
	}

	protected override float LerpRate => 10f;

	protected override float RotationLerpRate => 10f;

	public StartLocationData StartLocation { get; private set; }

	public override bool AttackWithAllowIncomplete => true;

	public Transform Spine_03
	{
		get
		{
			List<Transform> spineBones = SpineBones;
			if (spineBones == null || spineBones.Count <= 1)
			{
				return ThingTransform;
			}
			return SpineBones[1];
		}
	}

	public PlayableAreaRule PlayableAreaState { get; private set; }

	public Vector2 LastValidPlayablePosition { get; set; }

	public static event OnHumanCreatedEvent OnHumanCreated;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void ApplyLavaDamage()
	{
		if (Suit != null && (object)HeadAsSpaceHelmet != null)
		{
			Suit.AsThing.DamageState.Damage(ChangeDamageType.Increment, Suit.AsThing.AsDynamicThing.LavaDamage, DamageUpdateType.Burn);
			HeadAsSpaceHelmet.DamageState.Damage(ChangeDamageType.Increment, HeadAsSpaceHelmet.LavaDamage, DamageUpdateType.Burn);
			if (LeftHandSlot.Contains<DynamicThing>(out var occupant))
			{
				occupant.ApplyLavaDamage();
			}
			if (RightHandSlot.Contains<DynamicThing>(out var occupant2))
			{
				occupant2.ApplyLavaDamage();
			}
		}
		else
		{
			base.ApplyLavaDamage();
		}
	}

	public static Human GetNearest(Vector3 worldPosition)
	{
		Human result = null;
		float num = float.MaxValue;
		int count = AllHumans.Count;
		while (count-- > 0)
		{
			Human human = AllHumans[count];
			if (!(human == null))
			{
				float num2 = RocketMath.DistanceSquared(human.ThingTransformPosition, worldPosition);
				if (RocketMath.DistanceSquared(human.ThingTransformPosition, worldPosition) < num)
				{
					num = num2;
					result = human;
				}
			}
		}
		return result;
	}

	public void FootStep(AnimationEvent animationEvent)
	{
		if (Animator.GetFloat(_jetPackHash) >= _threshold || !Animator.GetBool(_groundedHash) || Animator.GetBool(_jumpHash))
		{
			return;
		}
		float num = Animator.GetFloat(_velocityHash);
		if (num < _threshold || animationEvent.animatorClipInfo.weight < _threshold)
		{
			return;
		}
		float num2 = Animator.GetFloat(_vHash);
		float num3 = Animator.GetFloat(_hHash);
		if (animationEvent.intParameter == _strafeNotify && (num2 < 0f - _range || num2 > _range || (num3 > 0f - _range && num3 < _range)))
		{
			return;
		}
		float num4 = Mathf.Clamp01(num / _velocityMax);
		float pitchMultiplier = Mathf.Clamp(num / _velocityMax, 0.5f, 1f);
		string text = null;
		Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid);
		if ((atmosphere != null && !atmosphere.IsGlobalAtmosphere && atmosphere.TotalVolumeLiquids > LiquidSolver.DEEP_THRESHOLD) || GlobalAtmosphereLiquid.IsUnderGlobalLiquid(base.Position))
		{
			text = animationEvent.stringParameter + "DeepWater";
		}
		else
		{
			text = ((SpeciesClass == SpeciesClass.Robot) ? ("Robot" + animationEvent.stringParameter) : animationEvent.stringParameter);
			if (atmosphere != null && !atmosphere.IsGlobalAtmosphere && atmosphere.TotalVolumeLiquids > LiquidSolver.RenderThreshold(atmosphere))
			{
				GetAudioEvent(Animator.StringToHash(animationEvent.stringParameter + "Water")).Trigger(num4, pitchMultiplier);
				num4 *= 0.7f;
			}
		}
		GameAudioEvent audioEvent = GetAudioEvent(Animator.StringToHash(text));
		if (audioEvent != null)
		{
			float time = Time.time;
			if (!(_lastFootStepTime + FootStepSoundCoolDown > time))
			{
				audioEvent.Trigger(num4, pitchMultiplier);
				_lastFootStepTime = time;
			}
		}
	}

	public async UniTaskVoid WaitStartBreathAudio()
	{
		await UniTask.WaitUntil(() => InventoryManager.ParentHuman != null && InventoryManager.ParentHuman.LungAtmosphere != null);
		if (!(this != InventoryManager.ParentHuman) && SpeciesClass != SpeciesClass.Robot)
		{
			_breathAudioSource = GetAudioSource(GetAudioEvent(BreathInRelaxedHash).Channel);
			_breathIn = false;
			_isBreathingAudio = true;
			Stress = 0f;
			Exertion = 0f;
			Breathlessness = 0f;
			BreathingState = BreathingState.NotBreathing;
		}
	}

	private void UpdateBreathingAudio()
	{
		if (!_isBreathingAudio || BreathingAtmosphere == null || base.State == EntityState.Dead || OrganLungs == null)
		{
			Stress = 0f;
			Exertion = 0f;
			Breathlessness = 0f;
			BreathingState = BreathingState.NotBreathing;
			return;
		}
		if (Stress > 0f)
		{
			Stress -= ReductionRate * Time.deltaTime;
		}
		if ((_legLVol + _legRVol + _hipsVol) / 3f >= MovementThreshold)
		{
			Exertion += ExertionRate * Time.deltaTime;
		}
		else
		{
			Exertion -= ReductionRate * Time.deltaTime;
		}
		if (Breathlessness > 0f && BreathingState != BreathingState.NoAir)
		{
			Breathlessness -= BreathlessnessReductionRate * Time.deltaTime;
		}
		if (CameraController.AlreadyLavaCam)
		{
			Stress = 1f;
		}
		if (OrganLungs.DamageEfficiency < _lungsLastDamageEfficiency || DamageState.Stun > 0f || base.Nutrition <= 0f || base.Hydration <= 0f)
		{
			Stress = 1f;
		}
		_lungsLastDamageEfficiency = OrganLungs.DamageEfficiency;
		Stress = Mathf.Clamp01(Stress);
		Exertion = Mathf.Clamp01(Exertion);
		if (!_breathAudioSource.isPlaying)
		{
			UpdateBreathingState();
			PlayBreathAudio();
		}
	}

	private void UpdateBreathingState()
	{
		if (BreathingAtmosphere == null)
		{
			return;
		}
		BreathingState breathingState = BreathingState.Relaxed;
		if (Exertion >= ExertionThreshold)
		{
			breathingState = BreathingState.Exertion;
		}
		else if (Exertion <= RelaxedThreshold)
		{
			breathingState = BreathingState.Relaxed;
		}
		else if (PreviousBreathingState == BreathingState.Relaxed && BreathingState == BreathingState.ToExertion)
		{
			breathingState = BreathingState.ToExertion;
		}
		else if (PreviousBreathingState == BreathingState.Exertion && BreathingState == BreathingState.ToRelaxed)
		{
			breathingState = BreathingState.ToRelaxed;
		}
		if (Stress > StressedThreshold || BreathingAtmosphere.PressureGassesAndLiquids > Chemistry.Limits.PressureMaximumSafe || BreathingAtmosphere.Temperature < Chemistry.Temperature.ZeroDegrees - new TemperatureKelvin(10.0) || BreathingAtmosphere.Temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(80.0))
		{
			breathingState = BreathingState.Stressed;
		}
		if (base.OxygenQuality < OxygenQualityThreshold || BreathingAtmosphere.PressureGassesAndLiquids < Chemistry.Limits.PressureMinimumSafe || OrganLungs.DamageState.TotalRatioClampedUndamaged < LungHealthThreshold)
		{
			breathingState = BreathingState.LowPressure;
			Exertion = 1f;
		}
		if (SpeciesClass == SpeciesClass.Human)
		{
			if (BreathingAtmosphere.PartialPressureHumanToxins > Entity.ToxicPartialPressureForWarning)
			{
				breathingState = BreathingState.Toxin;
			}
		}
		else if (SpeciesClass == SpeciesClass.Zrilian && BreathingAtmosphere.PartialPressureZrillianToxins > Entity.ToxicPartialPressureForWarning)
		{
			breathingState = BreathingState.Toxin;
		}
		if (Breathlessness > 0f)
		{
			breathingState = BreathingState.FirstBreath;
		}
		if (base.State == EntityState.Unconscious)
		{
			breathingState = BreathingState.Relaxed;
		}
		if (Breathlessness > 0f)
		{
			breathingState = BreathingState.FirstBreath;
		}
		if (BreathingAtmosphere.PressureGassesAndLiquids < NoBreathingThreshold || (CameraController.IsUnderWater && (HeadAsSpaceHelmet == null || HeadAsSpaceHelmet.IsOpen)))
		{
			breathingState = BreathingState.NoAir;
			Exertion = 1f;
		}
		if (PreviousBreathingState == BreathingState.NoAir && breathingState != BreathingState.NoAir && BreathingState != BreathingState.FirstBreath)
		{
			breathingState = BreathingState.FirstBreath;
			Breathlessness = 1f;
		}
		if (breathingState != BreathingState)
		{
			PreviousBreathingState = BreathingState;
			BreathingState = breathingState;
		}
	}

	private void PlayBreathAudio()
	{
		if (!_isBreathingAudio)
		{
			return;
		}
		if (BreathingState == BreathingState.NoAir || BreathingState == BreathingState.NotBreathing)
		{
			_breathIn = false;
		}
		else
		{
			_breathIn = !_breathIn;
		}
		float volumeMultiplier = 1f;
		if (!ListenerEffectManager.HelmetClosed)
		{
			volumeMultiplier = 0.5f;
		}
		bool flag = Gender == Gender.Male;
		float pitchMultiplier = ((BreathingAtmosphere.PartialPressureHelium > PressurekPa.One) ? 1.5f : 1f);
		switch (BreathingState)
		{
		case BreathingState.Relaxed:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInRelaxedHash : BreathOutRelaxedHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInRelaxedHash : FemaleBreathOutRelaxedHash, volumeMultiplier, pitchMultiplier);
			}
			break;
		case BreathingState.ToExertion:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInToExertionHash : BreathOutToExertionHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInToExertionHash : FemaleBreathOutToExertionHash, volumeMultiplier, pitchMultiplier);
			}
			break;
		case BreathingState.Exertion:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInExertionHash : BreathOutExertionHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInExertionHash : FemaleBreathOutExertionHash, volumeMultiplier, pitchMultiplier);
			}
			break;
		case BreathingState.ToRelaxed:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInToRelaxedHash : BreathOutToRelaxedHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInToRelaxedHash : FemaleBreathOutToRelaxedHash, volumeMultiplier, pitchMultiplier);
			}
			break;
		case BreathingState.Stressed:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInStressedHash : BreathOutStressedHash, 1f, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInStressedHash : FemaleBreathOutStressedHash, 1f, pitchMultiplier);
			}
			break;
		case BreathingState.LowPressure:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInLowPressureHash : BreathOutLowPressureHash, 1f, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInLowPressureHash : FemaleBreathOutLowPressureHash, 1f, pitchMultiplier);
			}
			break;
		case BreathingState.NoAir:
			break;
		case BreathingState.FirstBreath:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInFirstBreathHash : BreathOutFirstBreathHash, 1f, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInFirstBreathHash : FemaleBreathOutFirstBreathHash, 1f, pitchMultiplier);
			}
			break;
		case BreathingState.Toxin:
			if (flag)
			{
				PlaySound(_breathIn ? BreathInToxinHash : BreathOutToxinHash, 1f, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? FemaleBreathInToxinHash : FemaleBreathOutToxinHash, 1f, pitchMultiplier);
			}
			break;
		}
	}

	private void PlayJumpBreathAudio(bool jump)
	{
		if (!_isBreathingAudio)
		{
			return;
		}
		if (BreathingState == BreathingState.NoAir)
		{
			_breathIn = false;
			return;
		}
		float volumeMultiplier = 1f;
		if (!ListenerEffectManager.HelmetClosed)
		{
			volumeMultiplier = 0.5f;
		}
		float pitchMultiplier = ((BreathingAtmosphere.PartialPressureHelium > PressurekPa.One) ? 1.5f : 1f);
		bool num = Gender == Gender.Male;
		Exertion = Mathf.Min(Exertion + 0.5f, 1f);
		_breathIn = !_breathIn;
		if (num)
		{
			if (jump)
			{
				PlaySound(_breathIn ? BreathInJumpHash : BreathOutJumpHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				PlaySound(_breathIn ? BreathInJumpLandHash : BreathOutJumpLandHash, volumeMultiplier, pitchMultiplier);
			}
		}
		else if (jump)
		{
			PlaySound(_breathIn ? FemaleBreathInJumpHash : FemaleBreathOutJumpHash, volumeMultiplier, pitchMultiplier);
		}
		else
		{
			PlaySound(_breathIn ? FemaleBreathInJumpLandHash : FemaleBreathOutJumpLandHash, volumeMultiplier, pitchMultiplier);
		}
	}

	private void UpdateFoleyAudio(float deltaTime)
	{
		deltaTime = ((deltaTime > 0f) ? deltaTime : 0.034f);
		float num = deltaTime * _foleyLerpSpeed;
		_foleyLerpSpeed = 6f;
		Vector3 eulerAngles = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).transform.eulerAngles;
		Vector3 eulerAngles2 = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).transform.eulerAngles;
		Vector3 eulerAngles3 = Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).transform.position;
		Vector3 eulerAngles4 = Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).transform.position;
		Vector3 vector = Animator.GetBoneTransform(HumanBodyBones.Hips).transform.position;
		Vector3 vector2 = vector - _hipsPos;
		float num2 = Mathf.Clamp((vector2 - _hipsVel).magnitude / deltaTime, float.Epsilon, 10f);
		float num3 = ((Jump || !Grounded) ? 0.3f : 1f);
		float num4 = 1f;
		if (!Grounded || (BackpackSlot.Contains<Jetpack>(out var occupant) && occupant.Activate == 1 && occupant.HasPropellent) || !base.GravityEnabled)
		{
			num3 = float.Epsilon;
			num4 = 0.3f;
		}
		if (SpeciesClass == SpeciesClass.Robot)
		{
			eulerAngles3 = Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).transform.localRotation.eulerAngles;
			eulerAngles4 = Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).transform.localRotation.eulerAngles;
			num3 = 1f;
			_foleyLerpSpeed = RobotLerpSpeed;
		}
		_armLVol = Mathf.Lerp(_armLVol, ArmHumanVolumeCurve.Evaluate(CalculateVelocity(_armLPos, eulerAngles)), num);
		_armRVol = Mathf.Lerp(_armRVol, ArmHumanVolumeCurve.Evaluate(CalculateVelocity(_armRPos, eulerAngles2)), num);
		AnimationCurve animationCurve = ((SpeciesClass == SpeciesClass.Robot) ? LegRobotVolumeCurve : LegHumanVolumeCurve);
		_legLVol = Mathf.Lerp(_legLVol, animationCurve.Evaluate(CalculateVelocity(_legLPos, eulerAngles3) * num3), num);
		_legRVol = Mathf.Lerp(_legRVol, animationCurve.Evaluate(CalculateVelocity(_legRPos, eulerAngles4) * num3), num);
		if (HasAuthority && SpeciesClass == SpeciesClass.Robot)
		{
			float num5 = SpineCurve.Evaluate(Mathf.Clamp(CameraController.RotationalVelocitySmoothed, 0f, 40f));
			if (num5 < _spineVol)
			{
				num *= 1.4f;
			}
			_spineVol = Mathf.Lerp(_spineVol, num5, num);
			TriggerFoleyAudio(HumanBodyBones.Spine, _spineVol, Mathf.Lerp(0.65f, 1f, _spineVol));
		}
		float num6 = ToolBeltCurve.Evaluate(num2 * num4);
		_hipsVol = Mathf.Lerp(t: (!(num6 >= _hipsVol)) ? (deltaTime * 2f) : (deltaTime * 10f), a: _hipsVol, b: num6);
		TriggerFoleyAudio(HumanBodyBones.LeftUpperArm, _armLVol, Mathf.Lerp(0.5f, 1f, _armLVol));
		TriggerFoleyAudio(HumanBodyBones.RightUpperArm, _armRVol, Mathf.Lerp(0.5f, 1f, _armRVol));
		TriggerFoleyAudio(HumanBodyBones.LeftUpperLeg, _legLVol, Mathf.Lerp(0.5f, 1f, _legLVol));
		TriggerFoleyAudio(HumanBodyBones.RightUpperLeg, _legRVol, Mathf.Lerp(0.5f, 1f, _legRVol));
		_armLPos = eulerAngles;
		_armRPos = eulerAngles2;
		_legLPos = eulerAngles3;
		_legRPos = eulerAngles4;
		_hipsPos = vector;
		_hipsVel = vector2;
	}

	public float CalculateVelocity(Vector3 lastPosition, Vector3 currentPosition)
	{
		float num = ((Time.deltaTime > 0f) ? Time.deltaTime : 0.034f);
		return Mathf.Clamp((currentPosition - lastPosition).magnitude / num, float.Epsilon, 10000f);
	}

	public void TriggerFoleyAudio(HumanBodyBones bone, float volumeMultiplier, float pitchMultiplier)
	{
		bool flag = volumeMultiplier > 0.001f;
		switch (bone)
		{
		case HumanBodyBones.LeftUpperLeg:
			if (SpeciesClass == SpeciesClass.Robot)
			{
				PlayFoleyAudio(_legLRobotHash, flag, _isPlayingLegL, volumeMultiplier, pitchMultiplier);
				_isPlayingLegL = flag;
			}
			else
			{
				flag = flag && (SuitSlot.Occupant != null || UniformSlot.Occupant != null);
				PlayFoleyAudio(_legLHash, flag, _isPlayingLegL, volumeMultiplier, pitchMultiplier);
				_isPlayingLegL = flag;
			}
			break;
		case HumanBodyBones.RightUpperLeg:
			if (SpeciesClass == SpeciesClass.Robot)
			{
				PlayFoleyAudio(_legRRobotHash, flag, _isPlayingLegR, volumeMultiplier, pitchMultiplier);
				_isPlayingLegR = flag;
			}
			else
			{
				flag = flag && (SuitSlot.Occupant != null || UniformSlot.Occupant != null);
				PlayFoleyAudio(_legRHash, flag, _isPlayingLegR, volumeMultiplier, pitchMultiplier);
				_isPlayingLegR = flag;
			}
			break;
		case HumanBodyBones.LeftUpperArm:
			flag = flag && SuitSlot.Occupant != null;
			PlayFoleyAudio(_armLHash, flag, _isPlayingArmL, volumeMultiplier, pitchMultiplier);
			_isPlayingArmL = flag;
			break;
		case HumanBodyBones.RightUpperArm:
			flag = flag && SuitSlot.Occupant != null;
			PlayFoleyAudio(_armRHash, flag, _isPlayingArmR, volumeMultiplier, pitchMultiplier);
			_isPlayingArmR = flag;
			break;
		case HumanBodyBones.Hips:
			flag = flag && ToolbeltSlot.Occupant != null;
			PlayFoleyAudio(_toolBeltHash, flag, _isPlayingToolBelt, volumeMultiplier, pitchMultiplier);
			_isPlayingToolBelt = flag;
			break;
		case HumanBodyBones.Spine:
			if (SpeciesClass == SpeciesClass.Robot)
			{
				PlayFoleyAudio(_spineRobotHash, flag, _isPlayingSpine, volumeMultiplier, pitchMultiplier);
				_isPlayingSpine = flag;
			}
			break;
		}
	}

	private void PlayFoleyAudio(int nameHash, bool playState, bool isPlaying, float volumeMultiplier, float pitchMultiplier)
	{
		if (playState != isPlaying)
		{
			if (playState)
			{
				PlaySound(nameHash, volumeMultiplier, pitchMultiplier);
			}
			else
			{
				StopSound(nameHash);
			}
		}
		SetVolumeAndPitch(nameHash, pitchMultiplier, volumeMultiplier);
	}

	private void EnterLeftHandEquipSound()
	{
		if (!GameManager.IsBatchMode && HasAuthority && InventoryManager.Instance.ActiveHand.Slot == LeftHandSlot)
		{
			PlayEquipSound(LeftHandSlot.Occupant);
		}
	}

	private void EnterRightHandEquipSound()
	{
		if (!GameManager.IsBatchMode && HasAuthority && InventoryManager.Instance.ActiveHand.Slot == RightHandSlot)
		{
			PlayEquipSound(RightHandSlot.Occupant);
		}
	}

	public void PlayEquipSound(DynamicThing occupant)
	{
		if (HasAuthority && GameManager.GameState == GameState.Running && occupant != null && occupant.EquipSoundHash != -1 && occupant.GripType != HandPosition.SideGrip)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(occupant, occupant.EquipSoundHash, Vector3.zero);
		}
	}

	public void PlayUnEquipSound(DynamicThing occupant)
	{
		if (HasAuthority && GameManager.GameState == GameState.Running && occupant != null && occupant.UnEquipSoundHash != -1 && occupant.GripType != HandPosition.SideGrip)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(occupant, occupant.UnEquipSoundHash, Vector3.zero);
		}
	}

	public override void RefreshClothing()
	{
		base.RefreshClothing();
		CosmeticsBehaviour.RefreshSetGameObjectLayer();
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant is IWearable wearable)
			{
				wearable.RefreshVisibility();
			}
		}
	}

	public override bool DragInSlot(Slot destinationSlot, Vector3 offset)
	{
		if (InventoryManager.ParentHuman == destinationSlot.Parent)
		{
			base.GameObject.layer = Layers.IgnoreRaycast;
		}
		return base.DragInSlot(destinationSlot, offset);
	}

	public override void Explosion(Vector3 position, float force = 0f)
	{
		base.ActiveRigidbody.AddExplosionForce(force * 2.2f, position, 5f);
	}

	private void CacheGForce()
	{
		if (base.Room != null)
		{
			_gForce = 1f;
			return;
		}
		if (base.ParentSlot?.Parent is IRocketInternals rocketInternals)
		{
			Rocket rocket = rocketInternals.RocketNetwork?.Rocket;
			if (rocket != null)
			{
				RocketState rocketState = rocket.RocketState;
				if (rocketState == RocketState.Launching || rocketState == RocketState.Landing)
				{
					_gForce = Mathf.Abs(rocket.EngineAcceleration / 9.8f);
					return;
				}
			}
		}
		_gForce = (WorldManager.HasGravityAtHeight(base.Position.y) ? Mathf.Abs(WorldManager.WorldGravity / 9.8f) : 0f);
	}

	public override void Awake()
	{
		base.Awake();
		CosmeticsBehaviour.OnLocalChanged = OnLocalCosmeticsChanged;
		RigidBody.maxDepenetrationVelocity = 10f;
		foreach (Slot slot in Slots)
		{
			switch (slot.Type)
			{
			case Slot.Class.Helmet:
				HelmetSlot = slot;
				continue;
			case Slot.Class.Glasses:
				GlassesSlot = slot;
				continue;
			case Slot.Class.Suit:
				SuitSlot = slot;
				continue;
			case Slot.Class.Back:
				BackpackSlot = slot;
				continue;
			case Slot.Class.Belt:
				ToolbeltSlot = slot;
				continue;
			}
			if (slot.StringHash == Slot.StomachHash)
			{
				StomachSlot = slot;
			}
			else if (slot.StringHash == Slot.UniformHash)
			{
				UniformSlot = slot;
			}
			else if (slot.StringHash == Slot.LeftHandHash)
			{
				LeftHandSlot = slot;
			}
			else if (slot.StringHash == Slot.RightHandHash)
			{
				RightHandSlot = slot;
			}
		}
		SuitSlot.OnOccupantChange += OnSuitOccupantChanged;
		HelmetSlot.OnOccupantChange += OnHelmetOccupantChanged;
		UniformSlot.OnOccupantChange += OnUniformOccupantChanged;
		LeftHandSlot.OnEnter += EnterLeftHandEquipSound;
		RightHandSlot.OnEnter += EnterRightHandEquipSound;
		Animator.SetBool(MovementController.IdleHash, value: true);
		Animator.SetBool(MovementController.GroundedHash, value: true);
		AimIkTarget = AimIk.position;
		if (!AllHumans.Contains(this))
		{
			AllHumans.Add(this);
		}
	}

	public void UpdateCosmeticIdentity()
	{
		CosmeticsBehaviour.UpdateIdentity(CosmeticData);
		OnLocalCosmeticsChanged(CosmeticData);
		SetSpeciesSpecificSlots();
	}

	private void SetSpeciesSpecificSlots()
	{
		switch (SpeciesClass)
		{
		case SpeciesClass.Robot:
			UniformSlot.Type = Slot.Class.Battery;
			UniformSlot.StringKey = "Battery";
			UniformSlot.StringHash = Animator.StringToHash(UniformSlot.StringKey);
			UniformSlot.RefreshSlotDisplay();
			UniformSlot.Initialize();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case SpeciesClass.None:
		case SpeciesClass.Human:
		case SpeciesClass.Zrilian:
			break;
		}
	}

	private void OnLocalCosmeticsChanged(PlayerCosmetics newCosmetics)
	{
		CosmeticData = newCosmetics;
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 2048;
		}
		else if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new HumanIdentityMessage
			{
				HumanId = base.ReferenceId,
				Cosmetics = newCosmetics
			});
		}
	}

	private void OnHelmetOccupantChanged()
	{
		DynamicThing occupant = HelmetSlot.Occupant;
		if (!(occupant is GasMask headAsSpaceHelmet))
		{
			if (occupant is Hat headAsHat)
			{
				HeadAsHat = headAsHat;
				HeadAsSpaceHelmet = null;
				CosmeticsBehaviour.SetHairMode(HairMode.Hat);
			}
			else
			{
				HeadAsSpaceHelmet = null;
				HeadAsHat = null;
				CosmeticsBehaviour.SetHairMode(HairMode.Normal);
			}
		}
		else
		{
			HeadAsSpaceHelmet = headAsSpaceHelmet;
			HeadAsHat = null;
			CosmeticsBehaviour.SetHairMode(HairMode.Helmet);
		}
	}

	private void OnSuitOccupantChanged()
	{
		Suit = SuitSlot.Get<ISuit>();
		SetWearable().Forget();
	}

	private void OnUniformOccupantChanged()
	{
		Uniform = UniformSlot.Get<Uniform>();
		RobotBattery = UniformSlot.Get<BatteryCell>();
		SetWearable().Forget();
	}

	public async UniTaskVoid SetWearable()
	{
		if (_wearableScheduled)
		{
			return;
		}
		_wearableScheduled = true;
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		while (GameManager.GameState != GameState.Running || IsOccluded)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		IWearable occupant2;
		if (SuitSlot.Contains<IWearable>(out var occupant))
		{
			occupant.SetWearableVisibility(clothingOn: true);
			if (!SuitSlot.Contains<IBodyArmor>())
			{
				CosmeticsBehaviour.ApplyArmor(null);
			}
		}
		else if (UniformSlot.Contains<IWearable>(out occupant2))
		{
			occupant2.SetWearableVisibility(clothingOn: true);
			if (!SuitSlot.Contains<IBodyArmor>())
			{
				CosmeticsBehaviour.ApplyArmor(null);
			}
		}
		else
		{
			CosmeticsBehaviour.ApplyClothing(null);
			CosmeticsBehaviour.ApplyArmor(null);
		}
		_wearableScheduled = false;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AllHumans.Remove(this);
		if (GameManager.RunSimulation)
		{
			DestroyOrgans();
		}
		GameManager.OnGameStateChange -= UpdateNightVision;
	}

	protected override void LifeDehydrate()
	{
		ISuit occupant;
		GasMask occupant2;
		TemperatureKelvin temperatureKelvin = ((SuitSlot.Contains<ISuit>(out occupant) && occupant.InternalAtmosphere != null && HelmetSlot.Contains<GasMask>(out occupant2) && !occupant2.IsOpen) ? occupant.InternalAtmosphere.Temperature : GridController.World.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid).Temperature);
		temperatureKelvin -= Chemistry.Temperature.ZeroDegrees;
		float num = Mathf.Max(0.1f, (temperatureKelvin / scaleRate).ToFloat());
		float num2 = _hydrationLossPerTick + _hydrationLossPerTick * num;
		FloatReference hydrationRate = DifficultySetting.Current.HydrationRate;
		float num3 = (((object)OrganBrain != null && OrganBrain.IsOnline) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
		float num4 = num2 * (float)hydrationRate * num3;
		if (base.IsSleeping)
		{
			num4 *= 0.5f;
		}
		base.Hydration -= num4;
		base.LifeDehydrate();
	}

	public bool RoomStateOk()
	{
		if (RoomController.World.GetRoom(base.Position) != null)
		{
			return base.OxygenQuality > WarningOxygen;
		}
		return false;
	}

	public bool SuitOrHelmetOn()
	{
		ISuit suit = SuitSlot.Get<ISuit>();
		GasMask gasMask = HelmetSlot.Get<GasMask>();
		if (suit == null)
		{
			return gasMask;
		}
		return true;
	}

	private StringBuilder AppendMoodTooltip(StringBuilder sb)
	{
		string arg = PlayerStatsPanel.StatDeltaString(CalculateMoodChange());
		sb.AppendLine(GameStrings.PlayerStatsMoodDeltaState.AsString(arg));
		if (HygieneLow)
		{
			sb.AppendLine(StringManager.NegativeStatEffector(GameStrings.PlayerStatsLowHygiene.DisplayString));
		}
		else if (HygieneOk)
		{
			sb.AppendLine(StringManager.PositiveStatEffector(GameStrings.PlayerStatsHygieneOk.DisplayString));
		}
		if (RoomStateOk())
		{
			sb.AppendLine(StringManager.PositiveStatEffector(GameStrings.PlayerStatsRoomStateOk.DisplayString));
		}
		return sb;
	}

	private StringBuilder AppendFoodQualityToolTip(StringBuilder sb)
	{
		float foodQuality = base.FoodQuality;
		FoodQuality foodQuality2 = ((foodQuality < 0.7f) ? ((foodQuality < 0.45f) ? Assets.Scripts.Objects.Items.FoodQuality.Raw : Assets.Scripts.Objects.Items.FoodQuality.Cooked) : ((!(foodQuality < 0.9f)) ? Assets.Scripts.Objects.Items.FoodQuality.Complex : Assets.Scripts.Objects.Items.FoodQuality.Canned));
		FoodQuality foodQuality3 = foodQuality2;
		sb.AppendLine(GameStrings.PlayerStatsFoodQuality.AsString(Food.GetFoodQualityDescription(foodQuality3)));
		return sb;
	}

	private StringBuilder AppendHygieneTooltip(StringBuilder sb)
	{
		string arg = PlayerStatsPanel.StatDeltaString(CalculateHygieneChange());
		sb.AppendLine(GameStrings.PlayerStatsHygieneDeltaState.AsString(arg));
		if (SuitOrHelmetOn())
		{
			sb.AppendLine(StringManager.NegativeStatEffector(GameStrings.PlayerStatsSuitOrHelmetOn.DisplayString));
		}
		else
		{
			sb.AppendLine(StringManager.PositiveStatEffector(GameStrings.PlayerStatsNoSuitOrHelmet.DisplayString));
		}
		if (base.IsSoiled)
		{
			sb.AppendLine(StringManager.NegativeStatEffector(GameStrings.SoiledHeading.DisplayString));
		}
		return sb;
	}

	public string GetHygieneTooltip()
	{
		return AppendHygieneTooltip(new StringBuilder()).ToString();
	}

	public string GetMoodTooltip()
	{
		return AppendMoodTooltip(new StringBuilder()).ToString();
	}

	public string GetStatsTooltip()
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendFoodQualityToolTip(stringBuilder);
		AppendMoodTooltip(stringBuilder);
		AppendHygieneTooltip(stringBuilder);
		return stringBuilder.ToString();
	}

	public float CalculateMoodChange()
	{
		float num = 0f;
		if (HygieneLow)
		{
			float num2 = ((OrganBrain != null && OrganBrain.IsOnline) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
			num -= 0.00013888889f * (float)DifficultySetting.Current.MoodRate * num2;
		}
		else if (HygieneOk)
		{
			num += 0.0016666667f;
		}
		if (RoomStateOk())
		{
			float num3 = 0.00083333335f * Mathf.Lerp(0.1f, 1f, base.Hygiene);
			num += num3;
		}
		return num;
	}

	protected override void AssessMood()
	{
		base.Mood += CalculateMoodChange();
		base.AssessMood();
	}

	protected override void AssessHygiene()
	{
		float num = CalculateHygieneChange();
		if (num < 0f)
		{
			base.Hygiene += num;
		}
		else if (base.Hygiene < 1f)
		{
			base.Hygiene = Mathf.Min(base.Hygiene + num, 1f);
		}
		base.AssessHygiene();
	}

	public float CalculateHygieneChange()
	{
		float num = 0f;
		ISuit suit = SuitSlot.Get<ISuit>();
		GasMask gasMask = HelmetSlot.Get<GasMask>();
		if (SuitOrHelmetOn())
		{
			float num2 = ((OrganBrain != null && OrganBrain.IsOnline) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
			float num3 = suit?.HygieneReductionMultiplier ?? 1f;
			float num4 = (((object)gasMask == null || gasMask.IsOpen) ? 0.5f : 1f);
			num = -4.6296296E-05f * (float)DifficultySetting.Current.HygieneRate * num2 * num3 * num4;
		}
		else
		{
			num = 0.00041666668f;
		}
		if (base.IsSoiled)
		{
			num -= 0.05f;
		}
		return num;
	}

	protected override void LifeNutrition()
	{
		float num = ((base.Mood < 0.25f) ? 1.1f : 1f);
		float num2 = BaseNutritionStorage / (GameManager.TicksPerThirtyMinutes * 2f) * num * (float)DifficultySetting.Current.HungerRate * ((OrganBrain != null && OrganBrain.IsOnline) ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
		if (base.IsSleeping)
		{
			num2 *= 0.5f;
		}
		base.Nutrition -= num2;
		base.LifeNutrition();
	}

	private void MakePropsInvisible()
	{
		if (Suit == null)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in Suit.SkinnedMeshes)
		{
			skinnedMesh.Renderer.gameObject.layer = Layers.PlayerImmune;
			skinnedMesh.Renderer.updateWhenOffscreen = true;
		}
	}

	private void Digging()
	{
		Pickaxe pickaxe = LeftHandSlot.Get<Pickaxe>() ?? RightHandSlot.Get<Pickaxe>();
		if ((object)pickaxe != null && pickaxe.ParentSlot == InventoryManager.ActiveHandSlot)
		{
			pickaxe.PlaySound(DiggingHash);
			EffectManager.CreateSparkEffect(pickaxe.ThingTransform.GetChild(0), InventoryManager.Parent == this);
		}
	}

	public void CreateStomach()
	{
		if (!IsArtificial && StomachSlot != null)
		{
			OnServer.Create<Organ>(Prefab.Organ.Stomach, StomachSlot);
		}
	}

	public void CreateLungs()
	{
		if (!IsArtificial)
		{
			switch (SpeciesClass)
			{
			case SpeciesClass.Human:
			{
				Organ organ2 = OnServer.Create<Organ>(Prefab.Organ.LungsHuman, LungsSlot);
				MoleQuantity quantity2 = IdealGas.Quantity(Chemistry.OneAtmosphere, organ2.InternalAtmosphere.Volume, Chemistry.Temperature.TwentyDegrees);
				MoleEnergy energy2 = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, Mole.SpecificHeat(Chemistry.GasType.Oxygen), quantity2);
				AtmosphericEventInstance.CreateAdd(gasMixture: new GasMixture(new Mole(Chemistry.GasType.Oxygen, quantity2, energy2)), atmosphere: organ2.InternalAtmosphere);
				break;
			}
			case SpeciesClass.Zrilian:
			{
				Organ organ = OnServer.Create<Organ>(Prefab.Organ.LungsZirilian, LungsSlot);
				MoleQuantity quantity = IdealGas.Quantity(Chemistry.OneAtmosphere, organ.InternalAtmosphere.Volume, Chemistry.Temperature.TwentyDegrees);
				MoleEnergy energy = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, Mole.SpecificHeat(Chemistry.GasType.Methane), quantity);
				AtmosphericEventInstance.CreateAdd(gasMixture: new GasMixture(new Mole(Chemistry.GasType.Methane, quantity, energy)), atmosphere: organ.InternalAtmosphere);
				break;
			}
			}
		}
	}

	public void DropHeldItems()
	{
		Slot leftHandSlot = LeftHandSlot;
		if (leftHandSlot != null && leftHandSlot.IsNotEmpty())
		{
			OnServer.MoveToWorld(LeftHandSlot.Occupant);
		}
		Slot rightHandSlot = RightHandSlot;
		if (rightHandSlot != null && rightHandSlot.IsNotEmpty())
		{
			OnServer.MoveToWorld(RightHandSlot.Occupant);
		}
	}

	public void ToggleGodMode()
	{
		bool indestructable = OrganLungs.Indestructable;
		OrganLungs.Indestructable = !indestructable;
		base.Indestructable = !indestructable;
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.Indestructable = !indestructable;
			}
		}
	}

	public void SetEyesClosed(bool eyesClosed)
	{
		if (!(HeadAnimator == null))
		{
			HeadAnimator.SetBool(UnconsciousHash, eyesClosed);
		}
	}

	protected override void OnEntityUnconscious()
	{
		SetEyesClosed(eyesClosed: true);
		if (GameManager.RunSimulation)
		{
			DropHeldItems();
		}
		base.OnEntityUnconscious();
		if (HasAuthority && !base.IsSleeping)
		{
			InventoryManager.Instance.CancelPlacement();
			InventoryManager.Instance.ClearCursor();
			ShowHumanRespawnPrompt(immediate: false);
		}
		if (PlayerCosmeticsBehaviour.FacialExpressions.TryGet(_expressionNeutralIdHash, out var expression))
		{
			CosmeticsBehaviour.SetFacialExpression(expression);
		}
	}

	protected override void OnEntityConscious()
	{
		Achievements.AssessGoodMorning(this);
		SetEyesClosed(eyesClosed: false);
		base.OnEntityConscious();
	}

	public override void OnCollisionEnter(Collision collision)
	{
		if (GameManager.GameState != GameState.Running || !RootParent.HasAuthority || Invulnerability > 0f)
		{
			return;
		}
		float num = collision.impulse.magnitude / Time.deltaTime / 60f;
		if (!(num < MinSuitDamageVelocity))
		{
			Thing thing = Thing.Find(collision.collider);
			Vector3 contactPoint = collision.contacts[0].point + collision.contacts[0].normal * 0.2f;
			if (GameManager.RunSimulation)
			{
				OnNetworkCollision(contactPoint, collision.relativeVelocity, thing, num);
				return;
			}
			long otherThingId = ((thing == null) ? NetworkThing.Invalid : thing.netId);
			NetworkClient.SendToServer(new NetworkMessages.CollisionMessage
			{
				ContactPoint = contactPoint,
				RelativeVelocity = collision.relativeVelocity,
				ThingId = base.netId,
				OtherThingId = otherThingId,
				ImpulseMagnitude = num
			});
		}
	}

	public override void OnNetworkCollision(Vector3 contactPoint, Vector3 relativeVelocity, Thing otherThing, float impulseMagnitude)
	{
		DynamicThing dynamicThing = otherThing as DynamicThing;
		if (!(otherThing == null) && !(otherThing.RootParent != this) && (!(dynamicThing != null) || !(dynamicThing.ParentSlot.Parent != this)))
		{
			return;
		}
		float num = impulseMagnitude * SuitDamageVelocityScale;
		if ((bool)Suit?.AsThing && !Suit.Indestructable)
		{
			Suit.LeakRatio = Math.Min(Suit.LeakRatio + num * SuitLossVelocityRatio, 1f);
			Suit.AsThing.DamageState.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Brute);
			Suit.AsThing.PlaySound(_damageSuitHash);
			Stress = 1f;
			if (HasAuthority)
			{
				UpdateBreathingState();
				PlayBreathAudio();
			}
			if (Suit is SuitBase suitBase)
			{
				float value = suitBase.BruteDamagePassthroughAsStun * num;
				DamageState.Damage(ChangeDamageType.Increment, value, DamageUpdateType.Stun);
			}
		}
		if ((bool)HeadAsSpaceHelmet && !HeadAsSpaceHelmet.Indestructable)
		{
			HeadAsSpaceHelmet.PlaySound(_damageHelmetHash);
			HeadAsSpaceHelmet.LeakRatio = Math.Min(HeadAsSpaceHelmet.LeakRatio + impulseMagnitude * SuitLossVelocityRatio, 1f);
			HeadAsSpaceHelmet.DamageState.Damage(ChangeDamageType.Increment, impulseMagnitude * SuitDamageVelocityScale, DamageUpdateType.Brute);
			Stress = 1f;
			if (HasAuthority)
			{
				UpdateBreathingState();
				PlayBreathAudio();
			}
		}
		if ((bool)Suit?.AsThing && (bool)HeadAsSpaceHelmet)
		{
			return;
		}
		if ((bool)Uniform)
		{
			if (!Uniform.Indestructable)
			{
				Uniform.DamageState.Damage(ChangeDamageType.Increment, impulseMagnitude, DamageUpdateType.Brute);
			}
		}
		else if (!base.Indestructable)
		{
			DamageState.Damage(ChangeDamageType.Increment, impulseMagnitude, DamageUpdateType.Brute);
			Stress = 1f;
			if (HasAuthority)
			{
				UpdateBreathingState();
				PlayBreathAudio();
			}
		}
	}

	private void OnApplicationQuit()
	{
		if (HasAuthority)
		{
			NetworkClient.Disconnect().Forget();
		}
	}

	public void UpdateHeadShadow()
	{
		if (!HelmetSlot.Occupant || HelmetSlot.Occupant.Renderers.Count != 0)
		{
			HelmetShadowMesh.mesh = (HelmetSlot.Occupant ? HelmetSlot.Occupant.Renderers[0].MeshFilter.sharedMesh : null);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		UpdateHeadShadow();
		if (SpeciesClass != SpeciesClass.Robot)
		{
			UpdateNightVision();
		}
		if (newChild.ParentSlot == SuitSlot)
		{
			ReParentSuitBackOccupant();
			RefreshBackSlots();
			EjectBackSlotOccupant();
		}
		if (newChild is Stomach stomach && stomach.ParentSlot == StomachSlot)
		{
			OrganStomach = stomach;
			Organs.Add(stomach);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (SpeciesClass != SpeciesClass.Robot)
		{
			UpdateNightVision();
		}
		if (previousChild is GasMask gasMask)
		{
			gasMask.FlushMask();
		}
		if (previousChild is DraggableThing)
		{
			Invulnerability += 1f;
		}
		if (previousChild is Stomach stomach && OrganStomach == stomach)
		{
			OrganStomach = null;
			Organs.Remove(stomach);
		}
		UpdateHeadShadow();
		RefreshBackSlots();
		RestoreSuitBackOccupantParent(previousChild);
	}

	private void EjectBackSlotOccupant()
	{
		if (GameManager.RunSimulation && SuitSlot.Contains<SuitBase>() && BackpackSlot.Occupant != null)
		{
			OnServer.MoveToWorld(BackpackSlot.Occupant);
		}
	}

	private void RestoreSuitBackOccupantParent(DynamicThing previousChild)
	{
		if (previousChild is SuitBase suitBase && !(suitBase.BackSlot?.Occupant == null))
		{
			suitBase.BackSlot.Occupant.Transform.SetParent(suitBase.BackSlot.Location, worldPositionStays: false);
		}
	}

	private void ReParentSuitBackOccupant()
	{
		if (SuitSlot.Occupant is SuitBase suitBase && !(suitBase.BackSlot?.Occupant == null))
		{
			Transform location = BackpackSlot.Location;
			suitBase.BackSlot.Occupant.Transform.SetParent(location, worldPositionStays: false);
			suitBase.BackSlot.Occupant.SetVisibility(isVisible: true);
		}
	}

	public void RefreshBackSlots()
	{
		if (!HasAuthority)
		{
			return;
		}
		EntityState state = base.State;
		if (state != EntityState.Decay && state != EntityState.Dead)
		{
			DynamicThing occupant = SuitSlot.Occupant;
			if (occupant != null && occupant is SuitBase { BackSlot: not null } suitBase)
			{
				ShowSuitBackSlot(suitBase);
			}
			else
			{
				ShowHumanBackSlot();
			}
		}
	}

	private void ShowSuitBackSlot(SuitBase suitBase)
	{
		SlotDisplayMirror suitBackSlotDisplay = InventoryManager.Instance.SuitBackSlotDisplay;
		SlotDisplayButtonMirror suitBackSlotDisplayButton = InventoryManager.Instance.SuitBackSlotDisplayButton;
		SlotDisplay display = BackpackSlot.Display;
		if (display != null)
		{
			suitBackSlotDisplay?.SlotDisplayButton.SetActive(active: true);
			display?.SlotDisplayButton.SetActive(active: false);
			suitBackSlotDisplay?.HotkeyGrid.HideAll();
			suitBackSlotDisplay?.Link(suitBase.BackSlot.Display);
			suitBackSlotDisplayButton?.Link(suitBase.BackSlot.Display?.SlotDisplayButton);
			suitBackSlotDisplay?.RefreshDisplay();
		}
	}

	private void ShowHumanBackSlot()
	{
		SlotDisplayMirror suitBackSlotDisplay = InventoryManager.Instance.SuitBackSlotDisplay;
		SlotDisplayButtonMirror suitBackSlotDisplayButton = InventoryManager.Instance.SuitBackSlotDisplayButton;
		SlotDisplay display = BackpackSlot.Display;
		if (display != null)
		{
			suitBackSlotDisplay.SlotDisplayButton.SetActive(active: false);
			display.SlotDisplayButton.SetActive(active: true);
			suitBackSlotDisplay.Unlink();
			suitBackSlotDisplayButton.Unlink();
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			Slot parentSlot = newChild.ParentSlot;
			if (parentSlot != null && parentSlot.IsHandSlot)
			{
				SetHandPositionRotation(newChild);
			}
			else
			{
				base.SetSlotOccupantTransformData(newChild);
			}
		}
	}

	public void SetHandPositionRotation(DynamicThing newChild)
	{
		if (newChild is Item item)
		{
			bool flag = newChild.ParentSlot == LeftHandSlot;
			if (newChild.ParentSlot == RightHandSlot || flag)
			{
				item.SetHandPosition(flag);
			}
		}
	}

	private void UpdateNightVision()
	{
		if (!OrganBrain || !HasAuthority || CameraController.AlreadyLavaCam)
		{
			return;
		}
		NightVisionGoggles glassesAsNightVision = GlassesAsNightVision;
		CameraController.SetNightVision(glassesAsNightVision != null && glassesAsNightVision.IsOperable && glassesAsNightVision.OnOff && glassesAsNightVision.Powered);
		if (CurrentNightVision != null && CurrentNightVision != glassesAsNightVision)
		{
			CurrentNightVision.OnInteractable -= UpdateNightVision;
		}
		if (glassesAsNightVision != null)
		{
			if (CurrentNightVision != glassesAsNightVision)
			{
				glassesAsNightVision.OnInteractable += UpdateNightVision;
				CurrentNightVision = glassesAsNightVision;
			}
		}
		else
		{
			CurrentNightVision = null;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		switch (base.State)
		{
		case EntityState.Dead:
		case EntityState.Decay:
			result.State = GameStrings.EntityIsDead.AsString(ToTooltip());
			break;
		case EntityState.Unconscious:
			result.State = GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), EnumCollections.EntityStates.GetName(base.State));
			break;
		}
		if (Suit != null && Suit.AsThing.DamageState.Total > 0f)
		{
			result.RepairString = ISuitReparier.Tooltip;
		}
		result.Extended = GetExtendedText().ToString();
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (base.Hydration < WarningHydration)
		{
			extendedText.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), GameStrings.EntityDehydrated));
		}
		if (base.OxygenQuality < WarningOxygen)
		{
			extendedText.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), GameStrings.EntityWheezing));
		}
		if (base.Nutrition < WarningNutrition)
		{
			extendedText.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), GameStrings.EntityHungry));
		}
		if (DamageState.TotalRatio > WarningHealth)
		{
			extendedText.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), GameStrings.EntityHurt));
		}
		if (base.RespawnStressTime > 0f)
		{
			extendedText.AppendLine(GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), GameStrings.EntityRespawnStress));
		}
		return extendedText;
	}

	private async UniTaskVoid SetupReferencesWhenReady()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		InventoryManager.Instance.Initialize(this);
		KeyManager.ResetBindingsOnRespawn();
		await UniTask.Delay(100);
		StatusUpdates.Parent = this;
		if (base.ParentSlot == null)
		{
			base.ThingTransformPosition = _controlPosition;
		}
	}

	public void SetPhysicsOnControl()
	{
		RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		if ((bool)MovementController && (bool)OrganBrain && OrganBrain.HasAuthority && base.State == EntityState.Alive)
		{
			MovementController.enabled = true;
		}
		if (base.IsChild)
		{
			foreach (Slot slot in Slots)
			{
				if ((bool)slot.Occupant && slot.IsHiddenInSeat)
				{
					slot.Occupant.SetVisibility(isVisible: false, hideOnPlayer: true, isRecursive: true);
				}
			}
			return;
		}
		SetPhysics(on: true);
	}

	public void ShowHumanRespawnPrompt(bool immediate = true)
	{
		if (GameManager.IsNewTutorial)
		{
			ShowDeadInTutorialPrompt();
			return;
		}
		int delay = ((!immediate) ? RESPAWN_PROMPT_DISPLAY_DELAY : 0);
		switch (base.State)
		{
		case EntityState.Unconscious:
			WaitAndDisplayUnconsciousPrompt(delay).Forget();
			break;
		case EntityState.Dead:
		case EntityState.Decay:
			WaitAndDisplayDeadPrompt(delay).Forget();
			break;
		}
	}

	public async UniTaskVoid WaitAndDisplayUnconsciousPrompt(int delay = 3000)
	{
		await UniTask.Delay(delay);
		if (base.IsLocalPlayer && base.State == EntityState.Unconscious)
		{
			DisplayUnconsciousPrompt();
		}
	}

	public async UniTaskVoid WaitAndDisplayDeadPrompt(int delay = 3000)
	{
		await UniTask.Delay(delay);
		if (base.IsLocalPlayer && base.State == EntityState.Dead)
		{
			DisplayDeadPrompt();
		}
	}

	public void DisplayDeadPrompt()
	{
		Singleton<ConfirmationPanel>.Instance.Show(DeadPromptTitle, DeadPromptMessage, DeadPromptOptionWaitForHelp, null, DeadPromptOptionGiveUp, DeadShowRespawnPrompt);
	}

	public static void DisplayDecayPrompt()
	{
		Singleton<ConfirmationPanel>.Instance.Show(DeadPromptTitle, DeadPromptMessage, DeadPromptOptionWaitForHelp, null, DeadPromptOptionGiveUp, RespawnFromNoParent);
	}

	public void DisplayUnconsciousPrompt()
	{
		Singleton<ConfirmationPanel>.Instance.Show(UnconsciousPromptTitle, UnconsciousPromptMessage, UnconsciousPromptOptionWaitForHelp, null, UnconsciousPromptOptionGiveUp, UnconsciousShowRespawnPrompt);
	}

	public void DeadShowRespawnPrompt()
	{
		if (Singleton<ConfirmationPanel>.Instance.isActiveAndEnabled)
		{
			Singleton<ConfirmationPanel>.Instance.CloseCurrentPanel();
		}
		Singleton<ConfirmationPanel>.Instance.Show(GiveUpPromptTitle, DeadGiveUpPromptMessage, GiveUpPromptOptionCancel, null, GiveUpPromptOptionGiveUp, Respawn);
	}

	public void UnconsciousShowRespawnPrompt()
	{
		if (Singleton<ConfirmationPanel>.Instance.isActiveAndEnabled)
		{
			Singleton<ConfirmationPanel>.Instance.CloseCurrentPanel();
		}
		Singleton<ConfirmationPanel>.Instance.Show(GiveUpPromptTitle, DeadGiveUpPromptMessage, GiveUpPromptOptionCancel, null, GiveUpPromptOptionGiveUp, UnconsciousToRespawn);
	}

	private void SendAskForHelpChatMessage()
	{
		if (base.State switch
		{
			EntityState.Unconscious => GameStrings.PlayerRequestHelpUnconscious, 
			EntityState.Dead => GameStrings.PlayerRequestHelpDead, 
			_ => null, 
		} == null)
		{
			ConsoleWindow.PrintError($"Ask for help message is null for state {base.State}. Can only be states Unconscious or Dead", suppressStacktrace: true);
			return;
		}
		ChatMessage chatMessage = new ChatMessage
		{
			ChatText = GameStrings.PlayerRequestHelpUnconscious.AsString(DisplayName),
			DisplayName = "",
			HumanId = (GameManager.IsBatchMode ? (-1) : LocalHuman.ReferenceId)
		};
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(chatMessage);
		}
		else if (NetworkManager.IsServer)
		{
			chatMessage.PrintToConsole();
			NetworkServer.SendToClients(chatMessage, NetworkChannel.GeneralTraffic, -1L);
		}
		else
		{
			ConsoleWindow.PrintError("Error unable to send message");
		}
	}

	private void ShowDeadInTutorialPrompt()
	{
		PromptPanel.Instance.ShowPrompt(GameStrings.TutorialYouDied, GameStrings.TutorialExitToMainMenu, GameStrings.OkayConfirmation, QuitToMainMenu, isEscapable: false, hideCancelButton: true);
	}

	private void QuitToMainMenu()
	{
		InventoryManager.Instance.GameMenuPanel.SetActive(value: false);
		GameManager.LeaveGame();
	}

	protected override void PrepareUpdateMessage(ref DynamicThingPosition currentPosition)
	{
		base.PrepareUpdateMessage(ref currentPosition);
		currentPosition.ForceUpdate = false;
		currentPosition.DynamicThingId = base.netId;
		currentPosition.WorldPosition = base.ThingTransformPosition;
		currentPosition.WorldRotation = EntityRotation;
		currentPosition.ChildPosition = AimIk.position;
	}

	public override void ProcessPhysicsUpdate(DynamicThingPosition updateData)
	{
		base.ProcessPhysicsUpdate(updateData);
		if (!HasAuthority)
		{
			if (!IsOccluded)
			{
				AimIkTarget = updateData.ChildPosition;
			}
			else
			{
				AimIk.position = updateData.ChildPosition;
			}
		}
	}

	private void UnconsciousToRespawn()
	{
		OnServer.SetEntityState(base.netId, EntityState.Dead);
		Respawn();
	}

	private void SetDefaultLayerForSlotted()
	{
		foreach (Slot slot in Slots)
		{
			if (!(slot.Occupant == null))
			{
				slot.Occupant.SetRenderersToDefaultLayer();
			}
		}
	}

	private void Respawn()
	{
		CosmeticsBehaviour.SetGameObjectsAsDefault();
		CosmeticsBehaviour.UnregisterEvents();
		InventoryManager.ParentBrain.RelinquishControl();
		SetDefaultLayerForSlotted();
		LodManager.EnqueueRequesterToRemove(this);
		if (NetworkManager.IsClient)
		{
			if (ClientRequestCharacter.Initialized)
			{
				return;
			}
			ClientRequestCharacter.Initialize();
			NetworkClient.RequestCharacterAsync(isRespawn: true, ClientRequestCharacter.Token).Forget();
		}
		else
		{
			SerializedClientInfo clientInfo = GameManager.GetClientInfo(base.OwnerClientId);
			bool flag = clientInfo != null;
			StartLocationData startLocation = (flag ? DataCollection.Get<StartLocationData>(clientInfo.StartLocationHash) : null);
			ISpawnPoint spawnPoint = (flag ? Referencable.Find<ISpawnPoint>(clientInfo.SpawnPointReference) : null);
			Human newHuman = CreateCharacter(base.OwnerClientId, DisplayName, CosmeticData, flag, startLocation, spawnPoint);
			AwaitTerrainTakeControl(newHuman).Forget();
		}
		OnEntityDecay();
	}

	private async UniTaskVoid AwaitTerrainTakeControl(Human newHuman)
	{
		await UniTask.Yield();
		await UniTask.WaitUntil(() => !LodManager.Instance.AwaitingTerrainGeneration);
		newHuman?.OrganBrain?.TakeControl();
	}

	public static void RespawnFromNoParent()
	{
		InventoryManager.ParentBrain.RelinquishControl();
		if (NetworkManager.IsClient)
		{
			if (!ClientRequestCharacter.Initialized)
			{
				ClientRequestCharacter.Initialize();
				NetworkClient.RequestCharacterAsync(isRespawn: true, ClientRequestCharacter.Token).Forget();
			}
			return;
		}
		ulong localClientId = NetworkManager.LocalClientId;
		string username = NetworkManager.Username;
		PlayerCosmetics cosmetics = PlayerCosmetics.Load(Singleton<GameManager>.Instance.CustomCosmeticsSlot) ?? new PlayerCosmetics();
		SerializedClientInfo clientInfo = GameManager.GetClientInfo(localClientId);
		bool flag = clientInfo != null;
		StartLocationData startLocation = (flag ? DataCollection.Get<StartLocationData>(clientInfo.StartLocationHash) : null);
		ISpawnPoint spawnPoint = (flag ? Referencable.Find<ISpawnPoint>(clientInfo.SpawnPointReference) : null);
		CreateCharacter(localClientId, username, cosmetics, flag, startLocation, spawnPoint).OrganBrain.TakeControl();
	}

	public void SetBasicsData(ulong clientId, string clientUserName)
	{
		OnServer.PublishCustomName(this, clientUserName);
		base.OwnerClientId = clientId;
		base.name = "Character_" + clientUserName + "_" + clientId + "_" + clientUserName;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt64(base.OwnerClientId);
		writer.WriteString(CustomName);
		writer.WriteUInt16((ushort)_daysOfPotatoOnly);
		writer.WriteInt16((short)LastValidPlayablePosition.x);
		writer.WriteInt16((short)LastValidPlayablePosition.y);
		CosmeticData.Write(writer);
		writer.WriteInt32(StartLocation.IdHash);
		WriteMedicalEffects(writer);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ulong clientId = reader.ReadUInt64();
		string clientUserName = reader.ReadString();
		ushort daysOfPotatoOnly = reader.ReadUInt16();
		short num = reader.ReadInt16();
		short num2 = reader.ReadInt16();
		LastValidPlayablePosition = new Vector2(num, num2);
		CosmeticData.Read(reader);
		SetBasicsData(clientId, clientUserName);
		UpdateCosmeticIdentity();
		RefreshClothing();
		_daysOfPotatoOnly = daysOfPotatoOnly;
		DataCollection.TryGet<StartLocationData>(reader.ReadInt32(), out var data);
		StartLocation = data;
		ReadMedicalEffects(reader);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		ResetSlotPositions().Forget();
		ThingTransform.rotation = Quaternion.identity;
		if (_createStomachOnFinishedLoad)
		{
			_createStomachOnFinishedLoad = false;
			CreateStomach();
		}
	}

	public override void ValidateOnLoad(int currentSaveVersion)
	{
		base.ValidateOnLoad(currentSaveVersion);
		if (currentSaveVersion <= 27309)
		{
			_createStomachOnFinishedLoad = true;
		}
	}

	private async UniTaskVoid ResetSlotPositions()
	{
		await UniTask.WaitUntil(() => InventoryManager.ParentHuman != null || GameManager.GameState == GameState.None);
		await UniTask.Delay(2000);
		if (GameManager.GameState != GameState.None)
		{
			SetSlotOccupantTransformData(SuitSlot.Occupant);
			SetSlotOccupantTransformData(HelmetSlot.Occupant);
			SetSlotOccupantTransformData(GlassesSlot.Occupant);
			SetSlotOccupantTransformData(BackpackSlot.Occupant);
			SetSlotOccupantTransformData(LeftHandSlot.Occupant);
			SetSlotOccupantTransformData(RightHandSlot.Occupant);
			SetSlotOccupantTransformData(UniformSlot.Occupant);
			SetSlotOccupantTransformData(ToolbeltSlot.Occupant);
		}
	}

	public static Human Find(string name)
	{
		foreach (Human allHuman in AllHumans)
		{
			if (allHuman.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return allHuman;
			}
		}
		return null;
	}

	public static Human Find(ulong clientId)
	{
		foreach (Human allHuman in AllHumans)
		{
			if (allHuman.OwnerClientId == clientId)
			{
				return allHuman;
			}
		}
		return null;
	}

	public static Human CreateEmptyHuman(Slot slot, StartLocationData startLocation)
	{
		if (!GameManager.RunSimulation)
		{
			return null;
		}
		Human human = OnServer.Create<Human>(Prefab.Character, slot);
		human.WorldCenterOfMass = human.ActiveRigidbody.worldCenterOfMass;
		human.StartLocation = startLocation;
		human.Nutrition = human.BaseNutritionStorage;
		human.Hydration = 5f;
		human.Mood = 1f;
		human.Hygiene = 1f;
		human.FoodQuality = 0.75f;
		human.Oxygenation = 0.024f;
		human.SetPhysics(on: false);
		return human;
	}

	public static Human CreateCharacter(ulong clientId, string steamName, PlayerCosmetics cosmetics = null, bool isRespawn = false, StartLocationData startLocation = null, ISpawnPoint spawnPoint = null)
	{
		if (GameManager.IsNewTutorial && (cosmetics == null || cosmetics.SpeciesClass != SpeciesClass.Human))
		{
			cosmetics = null;
		}
		if (startLocation == null)
		{
			startLocation = WorldSetting.Current.StartLocationData.Get();
		}
		Vector3 safePositionInRadius = startLocation.GetSafePositionInRadius();
		Quaternion rotation = Quaternion.identity;
		if (spawnPoint != null)
		{
			safePositionInRadius = spawnPoint.GetSpawnPointTransform().position;
			rotation = spawnPoint.GetSpawnPointTransform().rotation;
		}
		Human human = OnServer.Create<Human>(Prefab.Character, safePositionInRadius, rotation);
		human.WorldCenterOfMass = human.ActiveRigidbody.worldCenterOfMass;
		human.StartLocation = startLocation;
		human.SetBasicsData(clientId, steamName);
		if (cosmetics != null)
		{
			human.CosmeticData = cosmetics;
		}
		human.UpdateCosmeticIdentity();
		OnServer.SetCustomName(human, steamName);
		if (isRespawn)
		{
			if (spawnPoint == null)
			{
				WorldSetting.OnRespawnPlayer(human);
			}
			WorldSetting.OnRespawnPlayerKit(human);
			human.RespawnStressTime = DifficultySetting.Current.RespawnStressTime;
		}
		else
		{
			if (spawnPoint == null)
			{
				WorldSetting.OnNewPlayer(human);
			}
			WorldSetting.OnNewPlayerKit(human);
		}
		human.OnLifeCreated(isRespawn);
		human.OrganBrain.RegisterBrain(clientId);
		NetworkServer.ClientIsAwaitingCharacter(clientId);
		GameManager.AddClientInfo(clientId, new SerializedClientInfo(clientId, startLocation.IdHash, spawnPoint?.ReferenceId ?? 0));
		human.SetPhysics(on: false);
		return human;
	}

	public void Apply(MedicalEffectBase medicalEffectBase)
	{
		if (GameManager.RunSimulation)
		{
			medicalEffectBase.Human = this;
			MedicalEffects.Add(medicalEffectBase);
			medicalEffectBase.OnApplied();
			base.NetworkUpdateFlags |= 4096;
		}
	}

	public void RemoveEffect(MedicalEffectBase medicalEffectBase)
	{
		if (MedicalEffects.Remove(medicalEffectBase))
		{
			base.NetworkUpdateFlags |= 4096;
		}
	}

	private void WriteMedicalEffects(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)MedicalEffects.Count);
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			writer.WriteByte((byte)medicalEffect.TypeId);
			writer.WriteSingle(medicalEffect.Remaining);
		}
	}

	private void ReadMedicalEffects(RocketBinaryReader reader)
	{
		MedicalEffects.Clear();
		byte b = reader.ReadByte();
		for (int i = 0; i < b; i++)
		{
			byte type = reader.ReadByte();
			float remaining = reader.ReadSingle();
			MedicalEffectBase medicalEffectBase = MedicalEffectBase.Create((MedicalEffectType)type, remaining);
			if (medicalEffectBase != null)
			{
				medicalEffectBase.Human = this;
				MedicalEffects.Add(medicalEffectBase);
			}
		}
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		UpdateSanitationRatio();
		CacheGForce();
		if (!GameManager.RunSimulation)
		{
			TickMedicalEffectsClient(deltaTime);
		}
	}

	private void TickMedicalEffectsClient(float deltaTime)
	{
		for (int i = 0; i < MedicalEffects.Count; i++)
		{
			MedicalEffectBase medicalEffectBase = MedicalEffects[i];
			if (medicalEffectBase.Remaining > 0f)
			{
				medicalEffectBase.Remaining = Mathf.Max(0f, medicalEffectBase.Remaining - deltaTime);
			}
		}
	}

	public void UpdateSanitationRatio()
	{
		base.SanitationRatio = OrganStomach?.GetWasteRatio() ?? 0f;
	}

	public void ForceSetPosition(Vector3 position)
	{
		if (GameManager.RunSimulation)
		{
			base.Position = position;
			base.ThingTransformPosition = position;
			Transform.position = position;
			base.ActiveRigidbody.MovePosition(position);
			if (!base.ActiveRigidbody.isKinematic)
			{
				base.ActiveRigidbody.velocity = Vector3.zero;
				base.ActiveRigidbody.angularVelocity = Vector3.zero;
			}
			_controlPosition = position;
			ResetInterpolation();
		}
	}

	public override void TakeControl(bool setPhysics)
	{
		base.TakeControl(setPhysics);
		_controlPosition = base.ThingTransformPosition;
		CameraController.ClearCameraShake();
		CameraController.Instance.SetCullingMask();
		SetupReferencesWhenReady().Forget();
		MakePropsInvisible();
		RefreshClothing();
		if (BackShadow.Visualizer == null)
		{
			BackShadow = new HeadShadowInfo
			{
				Visualizer = new GameObject("Back Shadow")
			};
			BackShadow.Renderer = BackShadow.Visualizer.AddComponent<MeshRenderer>();
			BackShadow.Renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
			BackShadow.Renderer.material = BaseMaterial;
			BackShadow.MeshFilter = BackShadow.Visualizer.AddComponent<MeshFilter>();
			BackShadow.Visualizer.transform.parent = BackpackSlot.Location;
			BackShadow.Visualizer.transform.localPosition = Vector3.zero;
			BackShadow.Visualizer.transform.localEulerAngles = Vector3.zero;
		}
		if (HasAuthority)
		{
			InventoryManager.EnablePlayerKeys = true;
			FirstPersonHelmetOverlay.Instance.RemoveHelmet();
			if (BaseAnimator != null)
			{
				BaseAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			}
			InventoryManager.RefreshSlotWearVisibility();
			WaitStartBreathAudio().Forget();
		}
		OnParentLocalityChange();
		UpdateNightVision();
		if (setPhysics)
		{
			SetPhysicsOnControl();
		}
		GameManager.OnGameStateChange += UpdateNightVision;
		LodManager.EnqueueRequesterToUpdate(this);
		ClientRequestCharacter.Cancel();
	}

	private void OnLifeCreated(bool isRespawn)
	{
		base.OnLifeCreated();
		base.Oxygenation = 0.024f;
		CreateLungs();
		CreateStomach();
		Human.OnHumanCreated?.Invoke(this);
	}

	protected override void TakeBreath()
	{
		MoleQuantity moleQuantity = new MoleQuantity(0.0048f * BreathingEfficiency * (float)DifficultySetting.Current.BreathingRate);
		MoleQuantity moleQuantity2 = MoleQuantity.Zero;
		switch (SpeciesClass)
		{
		case SpeciesClass.Human:
			moleQuantity2 = OrganLungs?.TakeBreath(BreathingAtmosphere, moleQuantity) ?? MoleQuantity.Zero;
			base.OxygenQuality = (moleQuantity2 / 0.004800000227987766 / (float)DifficultySetting.Current.BreathingRate).ToFloat();
			break;
		case SpeciesClass.Zrilian:
			moleQuantity2 = RocketMath.Min(moleQuantity, LungAtmosphere.GasMixture.Methane.Quantity * BreathingEfficiency);
			base.OxygenQuality = (moleQuantity2 / 0.004800000227987766 / (float)DifficultySetting.Current.BreathingRate).ToFloat();
			BreathingAtmosphere.GasMixture.NitrousOxide.AddAtTemperature(Entity.BodyTemperature, LungAtmosphere.GasMixture.Methane.Remove(moleQuantity2).Quantity * 0.032);
			LungAtmosphere.GasMixture.AddEnergy(Entity.EnergyReleasedPerTick);
			break;
		case SpeciesClass.Robot:
			moleQuantity2 = moleQuantity;
			base.OxygenQuality = (moleQuantity2 / 0.004800000227987766 / (float)DifficultySetting.Current.BreathingRate).ToFloat();
			break;
		}
		base.Oxygenation += (moleQuantity2 / (float)DifficultySetting.Current.BreathingRate).ToFloat();
	}

	public PressurekPa GetSafeN2OPartialPressure()
	{
		return SpeciesClass switch
		{
			SpeciesClass.Human => SafeN2OPartialPressureHuman, 
			SpeciesClass.Zrilian => SafeN2OPartialPressureZrilian, 
			_ => PressurekPa.MaxValue, 
		};
	}

	protected override bool N2OExceedsSafeLimit(out float stunDamage)
	{
		switch (SpeciesClass)
		{
		case SpeciesClass.Human:
			stunDamage = LungAtmosphere.PartialPressureNitrousOxide.ToFloat() / 5f;
			return stunDamage > 1f;
		case SpeciesClass.Zrilian:
			stunDamage = LungAtmosphere.PartialPressureNitrousOxide.ToFloat() / 16f;
			return stunDamage > 1f;
		default:
			stunDamage = 0f;
			return false;
		}
	}

	public void OnRevive()
	{
		if (!GameManager.IsBatchMode)
		{
			PromptPanel.Instance.DisablePromptPanel();
			InventoryManager.EnablePlayerKeys = true;
		}
		ResetCameraEffects(CameraController.Instance);
	}

	private static void SetPowerDrain(float newAmount)
	{
		PowerDrainedPerTick = newAmount;
	}

	private static void ResetPowerDrain()
	{
		PowerDrainedPerTick = 100f;
	}

	public override bool CanEat()
	{
		if ((bool)DifficultySetting.Current.EatWhileHelmetClosed || HelmetSlot.IsEmpty())
		{
			return true;
		}
		if (HelmetSlot.Contains<IObstructsEating>(out var occupant))
		{
			return occupant.IsOpen;
		}
		return true;
	}

	public override bool CanDrink()
	{
		if ((bool)DifficultySetting.Current.DrinkWhileHelmetClosed || HelmetSlot.IsEmpty())
		{
			return true;
		}
		if (HelmetSlot.Contains<IObstructsDrinking>(out var occupant))
		{
			return occupant.IsOpen;
		}
		return true;
	}

	public override bool CanDefecate()
	{
		if ((bool)DifficultySetting.Current.EatWhileHelmetClosed || SuitSlot.IsEmpty())
		{
			return true;
		}
		return !SuitSlot.Contains<IFullBody>();
	}

	public override void LifeEffects()
	{
		base.LifeEffects();
		if (IsArtificial)
		{
			return;
		}
		float num = (float)GameManager.GameTickSpeedMs / 1000f;
		if (GForce > 1f)
		{
			float num2 = GForce * 3f;
			if (GForce > 4f)
			{
				float num3 = GForce - 4f;
				num2 += 2f * num3;
			}
			DamageState.Damage(ChangeDamageType.Increment, num2 * num, DamageUpdateType.Stun);
		}
		for (int num4 = MedicalEffects.Count - 1; num4 >= 0; num4--)
		{
			MedicalEffects[num4].Update(num);
		}
	}

	public override bool OnLifeTick()
	{
		if (!base.OnLifeTick())
		{
			return false;
		}
		if (IsArtificial)
		{
			if (RootParent is ILifeSuspender { IsSuspendingLife: not false })
			{
				return true;
			}
			if ((object)RobotBattery != null)
			{
				float num = (OrganBrain.IsOnline ? 1f : ((float)DifficultySetting.Current.OfflineMetabolism));
				RobotBattery.PowerStored -= num * PowerDrainedPerTick * (float)DifficultySetting.Current.RobotBatteryRate;
			}
		}
		switch (base.State)
		{
		case EntityState.Alive:
		{
			if ((object)OrganBrain == null)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(delegate
				{
					Debug.Log("State dead. " + base.name, this);
				});
				break;
			}
			float stun = OrganBrain.DamageState.Stun;
			if (stun >= 90f && (RootParent is ILifeSuspender { IsSuspendingLife: not false } || stun >= 100f))
			{
				base.State = EntityState.Unconscious;
			}
			break;
		}
		case EntityState.Unconscious:
			if ((object)OrganBrain != null && OrganBrain.DamageState.Stun < 50f)
			{
				base.State = EntityState.Alive;
			}
			break;
		}
		return true;
	}

	protected override void OnStateChanged()
	{
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 1024;
		}
		switch (base.State)
		{
		case EntityState.Alive:
			Animator.enabled = true;
			if (HasAuthority)
			{
				MovementController.enabled = true;
				MovementController.UnclampTime = 1f;
				if (_oldState == EntityState.Dead)
				{
					OnRevive();
					ConsoleWindow.PrintAction(DisplayName + " revived");
				}
			}
			if (_oldState == EntityState.Unconscious)
			{
				OnEntityConscious();
			}
			SetEyesClosed(eyesClosed: false);
			CosmeticsBehaviour?.OnHumanConscious();
			break;
		case EntityState.Dead:
		{
			Animator.enabled = false;
			EntityDeath();
			StateChangedToNotAlive(this);
			if (PlayerCosmeticsBehaviour.FacialExpressions.TryGet(_expressionDeadIdHash, out var expression))
			{
				CosmeticsBehaviour.SetFacialExpression(expression);
			}
			break;
		}
		case EntityState.Unconscious:
			OnEntityUnconscious();
			StateChangedToNotAlive(this);
			break;
		case EntityState.Decay:
			OnEntityDecay();
			CosmeticsBehaviour.OnHumanUnconscious();
			break;
		}
	}

	private static void StateChangedToNotAlive(Human human)
	{
		if (!human)
		{
			return;
		}
		if (human.HasAuthority)
		{
			CameraController.SetUnderLava(show: false);
			CameraController.Instance.IsSensorLensesFxActive = false;
			CameraController.Instance.IsSolarStormEffectActive = false;
			if (!human.IsSleeping)
			{
				human.ShowHumanRespawnPrompt(immediate: false);
			}
		}
		if ((bool)human.MovementController)
		{
			human.MovementController.enabled = false;
		}
		human.SetEyesClosed(eyesClosed: true);
		human.CosmeticsBehaviour.OnHumanUnconscious();
	}

	public override void Explode()
	{
	}

	public void DestroyOrgans()
	{
		OnServer.Destroy(BrainSlot.Occupant);
		OnServer.Destroy(LungsSlot.Occupant);
		OnServer.Destroy(StomachSlot.Occupant);
	}

	public override void OnEntityDecay()
	{
		Entity.AllEntities.Remove(this);
		SetRagdoll(active: false);
		RigidBody.isKinematic = true;
		MovementController.enabled = false;
		Collider.enabled = false;
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.Enabled = false;
		}
		SkinnedMeshRendererInstance[] skinnedMeshes = SkinnedMeshes;
		foreach (SkinnedMeshRendererInstance skinnedMeshRendererInstance in skinnedMeshes)
		{
			if (!(skinnedMeshRendererInstance.Renderer == null))
			{
				skinnedMeshRendererInstance.Renderer.gameObject.SetActive(value: false);
			}
		}
		ShadowRenderersEnabled = false;
		if (HasAuthority)
		{
			InventoryManager.Instance.CancelPlacement();
			InventoryManager.Instance.ClearCursor();
		}
		if (GameManager.RunSimulation)
		{
			MovePlayerStuffToBox();
			CreateBodyBag();
		}
	}

	private void CreateBodyBag()
	{
		DynamicBodyBag dynamicBodyBag = Thing.Create<DynamicBodyBag>(BodyBagHash, Transform.position, Quaternion.identity, 0L);
		dynamicBodyBag.PlayersDisplayName = DisplayName;
		dynamicBodyBag.CustomName = GameStrings.BodyBagName.AsString(DisplayName);
		dynamicBodyBag.BodyDamageState = new EntityDamageState(dynamicBodyBag);
		dynamicBodyBag.BodyDamageState.Copy(DamageState);
		dynamicBodyBag.BodyCosmeticData = new PlayerCosmetics();
		dynamicBodyBag.BodyCosmeticData.Copy(CosmeticData);
		if (OrganBrain.ClientId != 0L)
		{
			OnServer.MoveToSlot(OrganBrain, dynamicBodyBag.BrainSlot);
		}
		if ((bool)OrganLungs)
		{
			OnServer.MoveToSlot(OrganLungs, dynamicBodyBag.LungsSlot);
		}
		if ((bool)OrganStomach)
		{
			OnServer.MoveToSlot(OrganStomach, dynamicBodyBag.StomachSlot);
		}
		OnServer.Destroy(this);
	}

	private void MovePlayerStuffToBox()
	{
		CardboardBox cardboardBox = Thing.Create<CardboardBox>(BoxHash, Transform.position + Vector3.up * 0.5f, Quaternion.identity, 0L);
		cardboardBox.CustomName = CreateOwnershipName(GameStrings.PlayerBelongings.DisplayString);
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && !slot.Contains<Organ>())
			{
				Slot nextFreeSlot = cardboardBox.GetNextFreeSlot();
				if (nextFreeSlot != null && (bool)slot.Occupant.CanEnter(nextFreeSlot))
				{
					OnServer.MoveToSlot(slot.Occupant, nextFreeSlot);
				}
				else
				{
					OnServer.MoveToWorld(slot.Occupant);
				}
			}
		}
	}

	private string CreateOwnershipName(string thing)
	{
		string displayName = DisplayName;
		string value = ((displayName[displayName.Length - 1] == 's') ? "'" : "'s");
		StringManager.ReusableStringBuilder.Clear();
		StringManager.ReusableStringBuilder.Append(DisplayName);
		StringManager.ReusableStringBuilder.Append(value);
		StringManager.ReusableStringBuilder.Append(" ");
		StringManager.ReusableStringBuilder.Append(thing);
		string result = StringManager.ReusableStringBuilder.ToString();
		StringManager.ReusableStringBuilder.Clear();
		return result;
	}

	protected override void EntityDeath()
	{
		base.EntityDeath();
		if (GameManager.RunSimulation)
		{
			DropHeldItems();
		}
		DamageState.Damage(ChangeDamageType.Set, DamageState.MaxDamage, DamageUpdateType.Stun);
		if (base.ParentSlot == null)
		{
			SetRagdoll(active: true);
		}
		ChatPanel.HideAllPopups();
		if (HasAuthority)
		{
			InventoryManager.Instance.CancelPlacement();
			InventoryManager.Instance.ClearCursor();
			if (WorldManager.HasLava)
			{
				CursorManager.Instance.LavaAudio.Stop();
			}
			CursorManager.Instance.LavaLight.enabled = false;
			CursorManager.Instance.LavaPlane.SetActive(value: false);
			CursorManager.Instance.LavaParticles.Stop();
			_isBreathingAudio = false;
			Achievements.AssessWellThankWasQuick(this);
		}
		if ((bool)HelmetSlot.Occupant)
		{
			HelmetSlot.Occupant.gameObject.layer = Layers.Player;
		}
		if ((bool)BackpackSlot.Occupant)
		{
			BackpackSlot.Occupant.gameObject.layer = Layers.Player;
		}
	}

	public bool IsGrounded(LayerMask mask, float distance = 0.02f)
	{
		float radius = MasterCollider.radius * GroundedRadiusScale;
		Vector3 vector = MasterCollider.transform.position + MasterCollider.radius.ToVector3Y();
		Vector3 end = vector - distance.ToVector3Y();
		return Physics.CheckCapsule(vector, end, radius, mask, QueryTriggerInteraction.Ignore);
	}

	public Vector3 GetIkTarget()
	{
		if (Physics.Raycast(CameraController.CameraOrigin, CameraController.CurrentCamera.transform.forward, out _ikHit, 10f, AllButPlayerImmune))
		{
			Vector3 point = _ikHit.point;
			if (RocketMath.DistanceSquared(point, base.ThingTransformPosition) > 5f)
			{
				return point;
			}
		}
		return CameraController.CameraOrigin + CameraController.CurrentCamera.transform.forward * 10f;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		PrepareAnimationMessage(writer);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			CosmeticData.Write(writer);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			WriteMedicalEffects(writer);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		AnimationUpdateMessage packet = default(AnimationUpdateMessage);
		packet.Read(reader);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			CosmeticData.Read(reader);
			CosmeticsBehaviour.UpdateIdentity(CosmeticData);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			ReadMedicalEffects(reader);
		}
		ProcessEntityAnimationUpdate(packet);
	}

	public override void BuildOwnerUpdate(RocketBinaryWriter writer)
	{
		base.BuildOwnerUpdate(writer);
		PrepareAnimationMessage(writer);
	}

	public override void ProcessOwnerUpdate(RocketBinaryReader reader)
	{
		base.ProcessOwnerUpdate(reader);
		AnimationUpdateMessage packet = default(AnimationUpdateMessage);
		packet.Read(reader);
		ProcessEntityAnimationUpdate(packet);
	}

	private void PrepareAnimationMessage(RocketBinaryWriter writer)
	{
		AnimationUpdateMessage animationUpdateMessage = new AnimationUpdateMessage
		{
			EntityId = base.ReferenceId,
			VFloat = ((Mathf.Abs(BaseAnimator.GetFloat(MovementController.VHash)) > MovementAnimationThreshold) ? BaseAnimator.GetFloat(MovementController.VHash) : 0f),
			HFloat = ((Mathf.Abs(BaseAnimator.GetFloat(MovementController.HHash)) > MovementAnimationThreshold) ? BaseAnimator.GetFloat(MovementController.HHash) : 0f),
			VelocityFloat = BaseAnimator.GetFloat(MovementController.VelocityHash),
			JetPackFloatCompressed = (byte)(BaseAnimator.GetFloat(MovementController.JetpackHash) * 255f),
			HasItemBool = BaseAnimator.GetBool(MovementController.HasItemHash),
			IdleBool = BaseAnimator.GetBool(MovementController.IdleHash),
			FlyUpBool = BaseAnimator.GetBool(MovementController.FlyUpHash),
			FlyDownBool = BaseAnimator.GetBool(MovementController.FlyDownHash),
			GroundedBool = BaseAnimator.GetBool(MovementController.GroundedHash),
			JumpBool = BaseAnimator.GetBool(MovementController.JumpHash),
			CastingBool = BaseAnimator.GetBool(MovementController.CastingHash),
			ActiveHand = (byte)BaseAnimator.GetInteger(MovementController.ActiveHandHash),
			ControlMode = (byte)BaseAnimator.GetInteger(MovementController.ControlModeHash),
			VerticalClimb = BaseAnimator.GetBool(MovementController.VerticalClimbHash),
			HandGrip = (byte)BaseAnimator.GetInteger(MovementController.HandGripHash),
			CastingAnimation = (byte)BaseAnimator.GetFloat(MovementController.CastingAnimationHash),
			Throwing = BaseAnimator.GetBool(MovementController.ThrowingHash)
		};
		SetEyesClosed(base.State != EntityState.Alive);
		animationUpdateMessage.Write(writer);
	}

	private void ProcessEntityAnimationUpdate(AnimationUpdateMessage packet)
	{
		_updatedLastAnimation = true;
		LastAnimationUpdate.Update(packet);
		if (!IsOccluded && !HasAuthority)
		{
			BaseAnimator.SetInteger(MovementController.ActiveHandHash, LastAnimationUpdate.ActiveHandInt);
			BaseAnimator.SetInteger(MovementController.ControlModeHash, LastAnimationUpdate.ControlModeInt);
			BaseAnimator.SetBool(MovementController.HasItemHash, LastAnimationUpdate.HasItemBool);
			BaseAnimator.SetBool(MovementController.IdleHash, LastAnimationUpdate.IdleBool);
			BaseAnimator.SetBool(MovementController.FlyUpHash, LastAnimationUpdate.FlyUpBool);
			BaseAnimator.SetBool(MovementController.FlyDownHash, LastAnimationUpdate.FlyDownBool);
			BaseAnimator.SetBool(MovementController.GroundedHash, LastAnimationUpdate.GroundedBool);
			BaseAnimator.SetBool(MovementController.CastingHash, LastAnimationUpdate.CastingBool);
			BaseAnimator.SetBool(MovementController.JumpHash, LastAnimationUpdate.JumpBool);
			BaseAnimator.SetBool(MovementController.VerticalClimbHash, LastAnimationUpdate.VerticalClimb);
			BaseAnimator.SetInteger(MovementController.HandGripHash, LastAnimationUpdate.HandGripInt);
			BaseAnimator.SetFloat(MovementController.CastingAnimationHash, LastAnimationUpdate.CastingAnimation);
			BaseAnimator.SetBool(MovementController.ThrowingHash, LastAnimationUpdate.Throwing);
			SetEyesClosed(base.State != EntityState.Alive);
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!PauseFoleyAudio)
		{
			UpdateFoleyAudio(deltaTime);
		}
	}

	private async UniTaskVoid ReturnToPlayableAreaTask()
	{
		_returnToPlayableAreaMessageOnCooldown = true;
		AlertMessage.Show(GameStrings.LeavingPlayableArea, 5f);
		await UniTask.Delay(15000);
		_returnToPlayableAreaMessageOnCooldown = false;
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (InventoryManager.ParentHuman == this && HasAuthority)
		{
			PointOfInterestManager.CheckPointsOfInterest(base.Position);
			if (WorldManager.HasLava)
			{
				CursorManager.Instance.HandleLavaBehaviour(this);
			}
			PlayableAreaState = CheckPlayableArea();
			if (PlayableAreaState == PlayableAreaRule.Valid)
			{
				LastValidPlayablePosition = new Vector2(base.Position.x, base.Position.z);
			}
			else if (!_returnToPlayableAreaMessageOnCooldown && InventoryManager.ParentHuman == this)
			{
				ReturnToPlayableAreaTask().Forget();
			}
		}
		if (!IsOccluded)
		{
			SetEyesClosed(base.State != EntityState.Alive);
		}
		if (Invulnerability > 0f)
		{
			Invulnerability -= Time.deltaTime;
		}
		if (LockedToSeat)
		{
			CameraController.MainCameraTransform.position = CameraRig.position;
		}
		if (HasAuthority && _isBreathingAudio)
		{
			UpdateBreathingAudio();
		}
		if ((GameManager.IsBatchMode || (!IsOccluded && !HasAuthority)) && _updatedLastAnimation)
		{
			float num = BaseAnimator.GetFloat(MovementController.VHash);
			float t = Mathf.Clamp(Mathf.Abs(LastAnimationUpdate.VFloat - num) / _animateRate, 0.25f, 1f);
			BaseAnimator.SetFloat(MovementController.VHash, Mathf.Lerp(num, LastAnimationUpdate.VFloat, t));
			float num2 = BaseAnimator.GetFloat(MovementController.HHash);
			t = Mathf.Clamp(Mathf.Abs(LastAnimationUpdate.HFloat - num2) / _animateRate, 0.25f, 1f);
			BaseAnimator.SetFloat(MovementController.HHash, Mathf.Lerp(num2, LastAnimationUpdate.HFloat, t));
			float num3 = BaseAnimator.GetFloat(MovementController.VelocityHash);
			t = Mathf.Clamp(Mathf.Abs(LastAnimationUpdate.VelocityFloat - num3) / _animateRate, 0.25f, 1f);
			BaseAnimator.SetFloat(MovementController.VelocityHash, Mathf.Lerp(num3, LastAnimationUpdate.VelocityFloat, t));
			float num4 = BaseAnimator.GetFloat(MovementController.JetpackHash);
			t = Mathf.Clamp(Mathf.Abs(LastAnimationUpdate.JetPackFloat - num4) / _animateRate, 0.25f, 1f);
			BaseAnimator.SetFloat(MovementController.JetpackHash, Mathf.Lerp(num4, LastAnimationUpdate.JetPackFloat, t));
		}
	}

	public override bool IsHealing()
	{
		if (base.IsHealing())
		{
			return true;
		}
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			if (medicalEffect is IHealEffectMoodle)
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsStimmed()
	{
		if (base.IsStimmed())
		{
			return true;
		}
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			if (medicalEffect is IStimEffectMoodle)
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsStunned()
	{
		if (base.IsStunned())
		{
			return true;
		}
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			if (medicalEffect is IStunEffectMoodle)
			{
				return true;
			}
		}
		return false;
	}

	public override float GetTime<T>()
	{
		float num = 0f;
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			if (medicalEffect is T val)
			{
				num = Mathf.Max(num, val.GetRemaining());
			}
		}
		return num;
	}

	private bool IsAllowedKinematic()
	{
		if (HasAuthority)
		{
			return MovementController.ControlMode != MovementController.Mode.JetpackGravity;
		}
		return LastAnimationUpdate.ControlModeInt != 1;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetWearVisibility();
			}
		}
		if (HasAuthority)
		{
			CameraController?.RotateCameraToSlot(this);
		}
		RigidBody.isKinematic = true;
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		if (InventoryManager.ParentHuman == parent)
		{
			base.GameObject.layer = Layers.Player;
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetWearVisibility();
			}
		}
		RigidBody.isKinematic = false;
		if (parent is ILifeSuspender && base.State == EntityState.Unconscious && GameManager.RunSimulation)
		{
			base.State = EntityState.Alive;
		}
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if ((bool)MovementController && !MovementController.enabled)
		{
			MovementController.Mode controlMode = MovementController.ControlMode;
			if (controlMode == MovementController.Mode.Seated || controlMode == MovementController.Mode.CryoTube || controlMode == MovementController.Mode.LyingDown)
			{
				ThingTransform.localPosition = Vector3.zero;
				ThingTransform.localRotation = Quaternion.identity;
				base.CharacterRotationY.localRotation = Quaternion.identity;
			}
		}
	}

	public override void SetWearVisibility(bool shouldUpdateLayers = true)
	{
		base.SetWearVisibility(shouldUpdateLayers: false);
	}

	public override void SetVisibility(bool isVisible, bool hideOnPlayer = false, bool isRecursive = false, bool shouldUpdateLayers = true)
	{
		base.SetVisibility(isVisible, hideOnPlayer, isRecursive, shouldUpdateLayers: false);
	}

	public void OnConsumeFood(float eatAmount, INutrition food)
	{
		float num = food.Nutrition(eatAmount);
		base.Nutrition += num;
		float foodQualityRatio = Food.GetFoodQualityRatio(food);
		float num2 = num / BaseNutritionStorage;
		base.FoodQuality = ((base.FoodQuality < foodQualityRatio) ? Math.Min(base.FoodQuality + num2, foodQualityRatio) : Math.Max(base.FoodQuality - num2, foodQualityRatio));
		base.Mood += food.MoodBonus * eatAmount;
		float num3 = food.WaterMoles * eatAmount;
		if (num3 > 0f && OrganStomach?.InternalAtmosphere != null)
		{
			MoleQuantity quantity = new MoleQuantity(num3);
			MoleEnergy energy = IdealGas.Energy(Entity.BodyTemperature, Mole.SpecificHeat(Chemistry.GasType.Water), quantity);
			AtmosphericEventInstance.CreateAdd(OrganStomach.InternalAtmosphere, new GasMixture(new Mole(Chemistry.GasType.Water, quantity, energy)));
		}
	}

	public override SelectionInstance GetSelection()
	{
		return new SelectionInstance
		{
			TargetIsHuman = true,
			Position = RigRoot.position + RigRoot.forward * 0.088f,
			Rotation = RigRoot.rotation,
			Bounds = _humanBounds,
			ParentThingRefernceId = base.ReferenceId,
			InteractableId = -1
		};
	}

	public DelayedActionInstance DragInto(Slot activeHandSlot, Vector3 offset, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.ActionDrag.DisplayString,
			ExtendedMessage = GameStrings.EntityIsCurrentlyState.AsString(ToTooltip(), EnumCollections.EntityStates.GetName(base.State))
		};
		if (!(activeHandSlot.Parent is Human human))
		{
			return delayedActionInstance.Fail(GameStrings.OnlyHumansCanDragThings, activeHandSlot.Parent.ToTooltip());
		}
		if (base.State == EntityState.Alive)
		{
			return delayedActionInstance.Fail(GameStrings.CannotDragWhileAlive, ToTooltip());
		}
		if (Vector3.Distance(base.Position, human.Position) > 1.5f)
		{
			return delayedActionInstance.Fail(GameStrings.ThingToFarAwayForDrag, ToTooltip());
		}
		if (activeHandSlot.IsNotEmpty())
		{
			return delayedActionInstance.Fail(GameStrings.ThingCanNotBeDraggedWithSomethingIn, activeHandSlot.ToTooltip());
		}
		if ((bool)human.Joint)
		{
			delayedActionInstance.Fail(GameStrings.ThingAlreadyDraggedBy, ToTooltip(), (human.AsEntity.ParentSlot == null) ? string.Empty : human.AsEntity.ParentSlot.Parent.ToTooltip());
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		OnServer.PublishDragHuman(this, activeHandSlot, Vector3.zero);
		return delayedActionInstance.Succeed();
	}

	public override void Hydrate(Mole water)
	{
		base.Hydrate(water);
		if (!(OrganStomach == null) && OrganStomach.InternalAtmosphere != null && water.Quantity > MoleQuantity.Zero)
		{
			AtmosphericEventInstance.CreateAdd(OrganStomach.InternalAtmosphere, new GasMixture(water));
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (attack.SourceItem is ISuitReparier suitReparier && Suit != null)
		{
			float num = suitReparier.RepairQuantity(Suit.AsRepairable);
			if (num <= 0f)
			{
				return null;
			}
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = num * suitReparier.GetRepairSpeed(),
				ActionMessage = GameStrings.ActionFixLeak.DisplayString
			};
			if (!doAction)
			{
				return delayedActionInstance;
			}
			suitReparier.RepairLeak(Suit.AsThing.netId, num * attack.CompletedRatio);
			return delayedActionInstance.Succeed();
		}
		DynamicThing sourceItem = attack.SourceItem;
		if (!(sourceItem is INutrition nutrition) || !(attack.DestinationThing is Entity entity) || !(attack.SourceItem is Item))
		{
			if (!(sourceItem is IHydration hydration) || !(attack.DestinationThing is Entity entity2))
			{
				if (!(sourceItem is Injector injector))
				{
					if (!(sourceItem is SanitationPacket sanitationPacket))
					{
						if (!(sourceItem is HemDroidRepairKit hemDroidRepairKit))
						{
							if (!(sourceItem is DisposableBatteryCharger disposableBatteryCharger))
							{
								if (sourceItem is Defibrillator defibrillator && attack.DestinationThing is Human human && human.IsDamaged())
								{
									DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
									{
										Duration = Defibrillator.TimeToUse,
										ActionMessage = GameStrings.Defibrillate.DisplayString
									};
									if (!defibrillator.IsOperable || !defibrillator.OnOff)
									{
										return delayedActionInstance2.Fail(GameStrings.DefibrillatorNotOperable);
									}
									if (!doAction)
									{
										return delayedActionInstance2;
									}
									if (GameManager.RunSimulation)
									{
										defibrillator.OnUseItem(attack.CompletedRatio, human);
									}
									return delayedActionInstance2.Succeed();
								}
							}
							else if (attack.DestinationThing is Human targetHuman)
							{
								BatteryCell targetBattery = disposableBatteryCharger.GetTargetBattery(targetHuman);
								if (!(targetBattery == null) && !(targetBattery.PowerRatio >= 0.99f) && disposableBatteryCharger.OnOff)
								{
									float num2 = Mathf.Min(disposableBatteryCharger.PowerStored, targetBattery.PowerMaximum - targetBattery.PowerStored) / disposableBatteryCharger.PowerBase;
									DelayedActionInstance delayedActionInstance3 = new DelayedActionInstance
									{
										Duration = 1f,
										ActionMessage = GameStrings.Recharge.DisplayString
									};
									if (!doAction || attack.CompletedRatio < 1f)
									{
										return delayedActionInstance3;
									}
									if (GameManager.RunSimulation)
									{
										disposableBatteryCharger.OnUseItem(num2 * attack.CompletedRatio, targetBattery);
									}
									return delayedActionInstance3.Succeed();
								}
							}
						}
						else if (attack.DestinationThing is Human { SpeciesClass: SpeciesClass.Robot } human2 && human2.IsDamaged())
						{
							DelayedActionInstance delayedActionInstance4 = new DelayedActionInstance
							{
								Duration = 1f,
								ActionMessage = GameStrings.Repair.DisplayString,
								ActionSoundHash = Defines.Sounds.HemDroidRepairKitHash,
								ActionCompleteSoundHash = Defines.Sounds.HemDroidRepairKitFinishedHash
							};
							if (!doAction || attack.CompletedRatio < 1f)
							{
								return delayedActionInstance4;
							}
							if (GameManager.RunSimulation)
							{
								hemDroidRepairKit.OnUseItem(attack.CompletedRatio, human2);
								return delayedActionInstance4.Succeed();
							}
							return delayedActionInstance4.Succeed();
						}
					}
					else if (attack.DestinationThing is Human human3 && attack.SourceItem is Item)
					{
						if (!human3.CanDefecate())
						{
							return DelayedActionInstance.Failure(ActionStrings.ForceFeed, GameStrings.DifficultyCannotDefecateThroughSuit);
						}
						DelayedActionInstance useAction = sanitationPacket.GetUseAction(human3);
						useAction.ActionMessage = ActionStrings.ForceDefecate;
						if (useAction.IsDisabled || !doAction)
						{
							return useAction;
						}
						if (GameManager.RunSimulation)
						{
							sanitationPacket.OnUseItem(attack.CompletedRatio, human3);
						}
						return useAction.Succeed();
					}
				}
				else if (attack.DestinationThing is Entity entity3)
				{
					DelayedActionInstance delayedActionInstance5 = new DelayedActionInstance
					{
						Duration = 1f,
						ActionMessage = GameStrings.Inject.DisplayString,
						ActionSoundHash = Defines.Sounds.HemDroidRepairKitHash,
						ActionCompleteSoundHash = Defines.Sounds.HemDroidRepairKitFinishedHash
					};
					if (!doAction || attack.CompletedRatio < 1f)
					{
						return delayedActionInstance5;
					}
					if (injector.OnUseItem(attack.CompletedRatio, entity3))
					{
						if (entity3 is Human target)
						{
							Achievements.AssessMedic(injector, attack.SourceItem.RootParentHuman, target);
						}
						return delayedActionInstance5.Succeed();
					}
					delayedActionInstance5.Duration = float.MaxValue;
					return delayedActionInstance5.Fail();
				}
				return base.AttackWith(attack, doAction);
			}
			if (!(attack.SourceItem is Item))
			{
				return base.AttackWith(attack, doAction);
			}
			if (!entity2.CanDrink())
			{
				return DelayedActionInstance.Failure(ActionStrings.ForceFeed, GameStrings.DifficultyCannotDrinkThroughHelmet);
			}
			float num3 = hydration.HydrateAmount(entity2);
			DelayedActionInstance delayedActionInstance6 = new DelayedActionInstance
			{
				Duration = hydration.HydrateTime(num3),
				ActionMessage = ActionStrings.ForceFeed,
				ActionSoundHash = Item.DrinkingHash,
				ActionCompleteSoundHash = Item.DrinkingFinishedHash
			};
			if (!doAction)
			{
				return delayedActionInstance6;
			}
			if (GameManager.RunSimulation)
			{
				hydration.OnUseItem(num3 * attack.CompletedRatio, entity2);
			}
			return delayedActionInstance6.Succeed();
		}
		if (!entity.CanEat())
		{
			return DelayedActionInstance.Failure(ActionStrings.ForceFeed, GameStrings.DifficultyCannotEatThroughHelmet);
		}
		float num4 = nutrition.EatAmount(entity);
		DelayedActionInstance delayedActionInstance7 = new DelayedActionInstance
		{
			Duration = nutrition.EatTime(num4),
			ActionMessage = ActionStrings.ForceFeed,
			ActionSoundHash = Item.EatingHash,
			ActionCompleteSoundHash = Item.EatingFinishedHash
		};
		if (!doAction)
		{
			return delayedActionInstance7;
		}
		if (GameManager.RunSimulation)
		{
			if (entity is Human human4)
			{
				Achievements.AssessMedic(attack.SourceItem as Item, attack.SourceItem.RootParentHuman, human4);
				human4.OnFoodEaten(nutrition);
			}
			nutrition.OnUseItem(num4 * attack.CompletedRatio, entity);
		}
		return delayedActionInstance7.Succeed();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new HumanSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is HumanSaveData humanSaveData))
		{
			return;
		}
		CosmeticData = humanSaveData.Cosmetics ?? new PlayerCosmetics();
		_daysOfPotatoOnly = humanSaveData.PotatoDays;
		LastValidPlayablePosition = humanSaveData.LastValidPlayablePosition;
		MedicalEffects = humanSaveData.MedicalEffects ?? new List<MedicalEffectBase>(5);
		foreach (MedicalEffectBase medicalEffect in MedicalEffects)
		{
			medicalEffect.Human = this;
		}
		if (humanSaveData.StartLocation != null)
		{
			DataCollection.TryGet<StartLocationData>(humanSaveData.StartLocation, out var data);
			StartLocation = data;
		}
		else
		{
			StartLocation = WorldSetting.Current.StartLocationData;
		}
		if (humanSaveData.Cosmetics != null && humanSaveData.Cosmetics.SpeciesClassDeprecated != SpeciesClass.None)
		{
			CosmeticData.SpeciesClass = humanSaveData.Cosmetics.SpeciesClassDeprecated;
		}
		UpdateCosmeticIdentity();
	}

	public CreditCard GetCreditCard()
	{
		if (UniformSlot.Contains<Uniform>(out var occupant))
		{
			foreach (Slot slot in occupant.Slots)
			{
				if (slot.Contains<CreditCard>(out var occupant2))
				{
					return occupant2;
				}
			}
		}
		if (LeftHandSlot.Contains<CreditCard>(out var occupant3))
		{
			return occupant3;
		}
		if (RightHandSlot.Contains<CreditCard>(out var occupant4))
		{
			return occupant4;
		}
		return null;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (GameManager.GameState != GameState.None && savedData is HumanSaveData humanSaveData)
		{
			humanSaveData.Cosmetics = CosmeticData;
			humanSaveData.PotatoDays = _daysOfPotatoOnly;
			humanSaveData.LastValidPlayablePosition = LastValidPlayablePosition;
			humanSaveData.MedicalEffects = MedicalEffects;
			if (StartLocation != null)
			{
				humanSaveData.StartLocation = new StringReference(StartLocation.Id);
			}
		}
	}

	private bool DisableIks()
	{
		if (((object)OrganBrain == null || OrganBrain.IsOnline) && !IsOccluded && Animator.GetInteger(MovementController.ControlModeHash) != 2 && Animator.GetInteger(MovementController.ControlModeHash) != 4 && Animator.GetInteger(MovementController.ControlModeHash) != 5 && !base.Unconscious)
		{
			return base.Dead;
		}
		return true;
	}

	private bool SeatedInFreeLook()
	{
		if (base.Unconscious || base.Dead || IsOccluded)
		{
			return false;
		}
		if ((object)OrganBrain != null && !OrganBrain.IsOnline)
		{
			return false;
		}
		if (Animator.GetInteger(MovementController.ControlModeHash) == 4)
		{
			if (base.ParentSlot?.Parent is IExitable exitable)
			{
				return exitable.FreeLook;
			}
			return false;
		}
		return false;
	}

	public void LateUpdate()
	{
		if (!GameManager.IsRunning)
		{
			return;
		}
		if (GameManager.IsBatchMode)
		{
			AimIk.position = Vector3.Lerp(AimIk.position, AimIkTarget, Time.fixedDeltaTime * 5f);
			return;
		}
		bool flag = this == InventoryManager.ParentHuman && !CameraController.CinematicMode;
		if (HasAuthority && flag)
		{
			if (!CameraController.IsHoldedCamera)
			{
				AimIkTarget = GetIkTarget();
				AimIk.position = AimIkTarget;
			}
		}
		else
		{
			AimIk.position = Vector3.Lerp(AimIk.position, AimIkTarget, Time.fixedDeltaTime * 5f);
		}
		AtmosControl = Animator.GetInteger(MovementController.ControlModeHash) != 2;
		Jump = BaseAnimator.GetBool(_jumpHash);
		Grounded = BaseAnimator.GetBool(_groundedHash);
		if (DisableIks())
		{
			if (SeatedInFreeLook())
			{
				IkSolveHead();
			}
			return;
		}
		IkSolveHead();
		IkSolveSpine();
		if (InventoryManager.ActiveHandSlot != null && InventoryManager.ActiveHandSlot.Contains<Item>(out var occupant) && occupant.PrecisionIk)
		{
			IkSolveHand();
		}
	}

	private void IkSolveHand()
	{
		LookAt(direction: (!IkShouldUseCameraLook()) ? (AimIk.position - HeadBone.position) : (CameraController.CameraOrigin + CameraController.CurrentCamera.transform.forward * 10f - HeadBone.position), trans: HandBones[Animator.GetInteger(MovementController.ActiveHandHash)], weight: _handWeight, hand: true);
	}

	private void IkSolveHead()
	{
		if (!(HeadBone == null))
		{
			Vector3 direction = ((!IkShouldUseCameraLook()) ? (AimIk.position - HeadBone.position) : (CameraController.CameraOrigin + CameraController.CurrentCamera.transform.forward * 10f - HeadBone.position));
			float weight = (SeatedInFreeLook() ? 0.9f : (_headWeight * _ikPositionWeight));
			LookAt(HeadBone, direction, weight);
		}
	}

	public override void OnNewDay()
	{
		base.OnNewDay();
		_daysOfPotatoOnly++;
	}

	private void IkSolveSpine()
	{
		if (SpineBones == null)
		{
			return;
		}
		foreach (Transform spineBone in SpineBones)
		{
			Vector3 direction = ((!IkShouldUseCameraLook()) ? (AimIk.position - spineBone.position) : (CameraController.CameraOrigin + CameraController.CurrentCamera.transform.forward * 10f - HeadBone.position));
			LookAt(spineBone, direction, _bodyWeight * _ikPositionWeight);
		}
	}

	public void LookAt(Transform trans, Vector3 direction, float weight, bool hand = false)
	{
		if (!float.IsNaN(weight))
		{
			Quaternion quaternion = Quaternion.FromToRotation(hand ? IkHandForward(trans) : IkForward(trans), direction);
			Quaternion rotation = trans.rotation;
			if (!float.IsNaN(quaternion.x) && !float.IsNaN(quaternion.y) && !float.IsNaN(quaternion.z) && !float.IsNaN(quaternion.w))
			{
				trans.rotation = Quaternion.Lerp(rotation, quaternion * rotation, weight);
			}
		}
	}

	public Vector3 IkForward(Transform trans)
	{
		return trans.rotation * IkAxis;
	}

	public Vector3 IkHandForward(Transform trans)
	{
		return trans.rotation * HandAxis;
	}

	public bool IkShouldUseCameraLook()
	{
		if (this == InventoryManager.ParentHuman && !CameraController.CinematicMode)
		{
			return !CameraController.IsThirdPerson;
		}
		return false;
	}

	public void SetChatStatus(bool show)
	{
		if (!IsOccluded)
		{
			ChatPanel.gameObject.SetActive(value: true);
			ChatPanel.ShowStatus(show);
		}
	}

	public void SetChatText(string chattext)
	{
		if (!IsOccluded)
		{
			ChatPanel.gameObject.SetActive(value: true);
			ChatPanel.SetText(chattext);
		}
	}

	public override bool ForceKinematicClient(bool kinematic, bool localCheck)
	{
		if (base.Room == null)
		{
			return false;
		}
		if (!IsAllowedKinematic())
		{
			return false;
		}
		if (HasAuthority && localCheck)
		{
			if (Input.anyKey && kinematic)
			{
				_lastKinematicUpdateTime = Time.time;
				return false;
			}
			if (kinematic && Time.time - _lastKinematicUpdateTime < _timeUntilClamp)
			{
				return false;
			}
		}
		_lastKinematicUpdateTime = Time.time;
		return base.ForceKinematicClient(kinematic, localCheck);
	}

	public void OnPlayerStayLadder(Ladder target)
	{
		base.TargetLadder = target;
		base.ActiveRigidbody.useGravity = false;
		if (HasAuthority && MovementController != null)
		{
			Vector3 thingTransformPosition = target.Position + target.transform.forward * 0.5f;
			thingTransformPosition.y = base.ActiveRigidbody.position.y;
			base.ThingTransformPosition = thingTransformPosition;
			if (MovementController.ControlMode != MovementController.Mode.Ladder)
			{
				Quaternion localRotation = Quaternion.Euler(0f, target.transform.eulerAngles.y - 180f, 0f);
				base.CharacterRotationY.localRotation = localRotation;
				MovementController.ControlMode = MovementController.Mode.Ladder;
			}
		}
	}

	public void OnPlayerLeaveLadder()
	{
		if (base.TargetLadder == null)
		{
			return;
		}
		base.TargetLadder = null;
		if (HasAuthority)
		{
			Invulnerability += 1f;
			if (MovementController != null)
			{
				MovementController.ControlMode = MovementController.Mode.Animation;
			}
		}
	}

	public void SwapHands()
	{
		HumanHandsBehaviour.SwapHands();
	}

	public void ToggleHelmetLight()
	{
		if (!base.IsUnresponsive && !base.IsSleeping && (bool)AsHuman && AsHuman.HelmetSlot.Contains<IWearableLight>(out var occupant))
		{
			Thing.Interact(Thing.Find(occupant.netId), InteractableType.OnOff, (!occupant.OnOff) ? 1 : 0);
			SetPowerDrain(105f);
		}
	}

	public void ToggleNightVision()
	{
		if (!base.IsUnresponsive && !base.IsSleeping && (bool)AsHuman && AsHuman.SpeciesClass == SpeciesClass.Robot)
		{
			CameraController.SetNightVision(!CurrentlyUsingNightVision, 1f, 0.5f, robotMode: true);
			ResetPowerDrain();
		}
	}

	public void SpawnDynamicThing()
	{
		if (GameManager.IsTutorial || WorldManager.Instance.GameMode != GameMode.Creative)
		{
			return;
		}
		ICreativeSpawnable spawnPrefab = InventoryManager.SpawnPrefab;
		if (spawnPrefab != null)
		{
			if (spawnPrefab is Thing)
			{
				OnServer.SpawnDynamicThingMaxStack(base.ReferenceId, spawnPrefab.SpawnableName);
			}
			else
			{
				OnServer.SpawnSpawnData(base.ReferenceId, spawnPrefab.SpawnId);
			}
		}
	}

	public void ToggleInternals()
	{
		if (!base.IsUnresponsive && !base.IsSleeping && HasInternals && !InternalsLocked)
		{
			InternalsOn = !InternalsOn;
		}
	}

	public List<DynamicThing> GetContents()
	{
		List<DynamicThing> list = new List<DynamicThing>(2);
		AddChildrenToContents(LeftHandSlot, list);
		AddChildrenToContents(RightHandSlot, list);
		return list;
	}

	private static void AddChildrenToContents(Slot slot, List<DynamicThing> contents)
	{
		if (slot.IsEmpty())
		{
			return;
		}
		Slot.Class type = slot.Type;
		if ((type != Slot.Class.None && type != Slot.Class.Ore) || !slot.Contains<DynamicThing>(out var occupant))
		{
			return;
		}
		contents.Add(occupant);
		foreach (Slot slot2 in occupant.Slots)
		{
			AddChildrenToContents(slot2, contents);
		}
	}

	public List<Slot> GetSlots()
	{
		List<Slot> list = new List<Slot>(2);
		AddChildrenToSlots(LeftHandSlot, list);
		AddChildrenToSlots(RightHandSlot, list);
		return list;
	}

	private static void AddChildrenToSlots(Slot slot, List<Slot> contents)
	{
		Slot.Class type = slot.Type;
		if (type != Slot.Class.None && type != Slot.Class.Ore)
		{
			return;
		}
		contents.Add(slot);
		if (!slot.Contains<DynamicThing>(out var occupant))
		{
			return;
		}
		foreach (Slot slot2 in occupant.Slots)
		{
			AddChildrenToSlots(slot2, contents);
		}
	}

	public bool IsSlotTradable(Slot slot)
	{
		if (slot.Parent == this)
		{
			return slot.IsHandSlot;
		}
		return false;
	}

	public bool TryGetJetpack(out Jetpack jetpack)
	{
		if (SuitSlot.Contains<SuitBase>(out var occupant))
		{
			if (occupant.BackSlot != null && occupant.BackSlot.Contains<Jetpack>(out jetpack))
			{
				return true;
			}
			jetpack = null;
			return false;
		}
		return BackpackSlot.Contains<Jetpack>(out jetpack);
	}

	public bool ExitAnything()
	{
		Slot parentSlot = base.ParentSlot;
		if (parentSlot == null || !(parentSlot.Parent is IExitable exitable) || LockedToSeat)
		{
			return false;
		}
		if (base.ParentSlot?.Parent is LanderCapsule { IsLocked: not false })
		{
			return false;
		}
		if (base.ParentSlot?.Parent is CrewModuleChair crewModuleChair && !crewModuleChair.CanExitSeat())
		{
			return false;
		}
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new MoveToWorldMessage
			{
				ChildId = base.ReferenceId,
				Force = 0f,
				IsPrecisionPlacement = true,
				Position = exitable.GetExitPosition(this),
				Rotation = base.ParentSlot.Parent.Rotation,
				Velocity = Vector3.zero,
				AngularVelocity = Vector3.zero
			});
			if (base.IsLocalPlayer)
			{
				exitable.Exit(this);
			}
		}
		else
		{
			exitable.Exit(this);
		}
		return true;
	}

	public int GetPotatoDays()
	{
		return _daysOfPotatoOnly;
	}

	public void OnFoodEaten(INutrition food)
	{
		if (!food.Equals(_potatoOnly))
		{
			_daysOfPotatoOnly = 0;
		}
	}
}
