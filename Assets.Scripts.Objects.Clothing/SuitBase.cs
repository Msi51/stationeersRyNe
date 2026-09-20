using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Clothing.Suits;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Sound;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;
using Weather;

namespace Assets.Scripts.Objects.Clothing;

public abstract class SuitBase : AtmosphericItem, ISuit, IReferencable, IEvaluable, IWearable, IFullBody, IBatteryPowered, IPowered, IDensePoolable, ICircuitHolder, ITransmitable, ILogicable, ISetable, ISoundAlert, ISuitCooling, IMemoryReadable, IMemory, IMemoryWritable
{
	private enum SuitState
	{
		None,
		Off,
		Okay,
		Error
	}

	[Header("Suit")]
	[SerializeField]
	private KitItem _suitAsset;

	[SerializeField]
	private SuitConvectionData suitConvectionData;

	[SerializeField]
	private float pressurePerTick = 101.325f;

	[FormerlySerializedAs("airTankSlot")]
	[SerializeField]
	[HideInInspector]
	protected int airTankSlotId;

	[FormerlySerializedAs("wasteTankSlot")]
	[SerializeField]
	protected int wasteTankSlotId;

	[FormerlySerializedAs("coolantTankSlot")]
	[SerializeField]
	protected int coolantTankSlotId;

	[FormerlySerializedAs("batterySlot")]
	[SerializeField]
	protected int batterySlotId;

	[FormerlySerializedAs("chipSlot")]
	[SerializeField]
	protected int chipSlotId = -1;

	[FormerlySerializedAs("modSlot")]
	[SerializeField]
	protected int modSlotId;

	[FormerlySerializedAs("filterSlot1")]
	[SerializeField]
	protected int filterSlot1Id;

	[FormerlySerializedAs("filterSlot2")]
	[SerializeField]
	protected int filterSlot2Id;

	[FormerlySerializedAs("filterSlot3")]
	[SerializeField]
	protected int filterSlot3Id;

	[SerializeField]
	protected int backSlot;

	public List<Item> AllowedBackPackPrefabs = new List<Item>();

	public static string MaterialMaskColorPropertyName = "_MaskColor";

	public static float MinSetting = 0f;

	public static float MaxSetting = 202.65f;

	private const float EXPLOSION_FORCE = 200f;

	private const float EXPLOSION_RADIUS = 2.3f;

	private static readonly TemperatureKelvin WarningBuffer = new TemperatureKelvin(10.0);

	private float _outputSetting = 101.325f;

	private TemperatureKelvin _outputTemperature = Chemistry.Temperature.TwentyDegrees;

	private Vector3 _baseLeakScale;

	private Coroutine EntityWaitCoroutine;

	private List<Slot> _filterSlots = new List<Slot>();

	private const int WAIT_FOR_ENTITY_MS = 20000;

	private static readonly int SuitButtonUpHash = Animator.StringToHash("SuitButtonUp");

	private static readonly int SuitButtonDownHash = Animator.StringToHash("SuitButtonDown");

	private int _destroySuitHash = Animator.StringToHash("DestroySuit");

	private const float EFFICIENCY_MULTIPLIER_MIN = 0.1f;

	private const float EFFICIENCY_MULTIPLIER_MAX = 0.5f;

	private const float HOT_COOLANT_EFFICIENCY = 0.25f;

	protected bool _emptyFilter;

	[ReadOnly]
	public float FilterQuantitySum;

	private static readonly int AirLeakHash = Animator.StringToHash("AirLeak");

	public static float EnergyCoolPowerCostPercent = 0.01f;

	[SerializeField]
	private float maxEnergy = 4000f;

	private MoleEnergy _desiredEnergyDelta;

	private MoleEnergy _desiredEnergy;

	private MoleEnergy _usedEnergy;

	public Material ColorOff;

	public Material ColorOkay;

	public Material ColorError;

	public Material ColorErrorFlash;

	private bool _doFlash;

	private Material[] _materials;

	public int ColorMaterialIndex = 1;

	public int StateMaterialIndex = 2;

	private SuitState _currentState;

	private List<ILogicable> _logicList = new List<ILogicable>(20);

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	[ByteArraySync]
	private int _setting = 1;

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	public ulong LastEditedBy { get; set; }

	public SuitConvectionData SuitConvectionData => suitConvectionData;

	public PressurekPa PressurePerTick => new PressurekPa(pressurePerTick);

	public virtual TemperatureKelvin MaxCoolantTemperatureK => new TemperatureKelvin(323.15);

	public virtual TemperatureKelvin MinCoolantTemperatureK => MinTempSetting;

	public Thing AsThing => this;

	public IRepairable AsRepairable => this;

	public Slot AirTankSlot
	{
		get
		{
			if (airTankSlotId <= -1)
			{
				return null;
			}
			return Slots[airTankSlotId];
		}
	}

	public bool HasAirTankSlot => airTankSlotId > -1;

	public Slot WasteTankSlot
	{
		get
		{
			if (wasteTankSlotId <= -1)
			{
				return null;
			}
			return Slots[wasteTankSlotId];
		}
	}

	public bool HasWasteTankSlot => wasteTankSlotId > -1;

	public Slot CoolantTankSlot
	{
		get
		{
			if (coolantTankSlotId <= -1)
			{
				return null;
			}
			return Slots[coolantTankSlotId];
		}
	}

	public bool HasCoolantTankSlot => coolantTankSlotId > -1;

	public Slot BatterySlot
	{
		get
		{
			if (batterySlotId <= -1)
			{
				return null;
			}
			return Slots[batterySlotId];
		}
	}

	public bool HasBatterySlot => batterySlotId > -1;

	public Slot ChipSlot
	{
		get
		{
			if (chipSlotId <= -1)
			{
				return null;
			}
			return Slots[chipSlotId];
		}
	}

	public bool HasChipSlot => chipSlotId > -1;

	public Slot ModSlot
	{
		get
		{
			if (modSlotId <= -1)
			{
				return null;
			}
			return Slots[modSlotId];
		}
	}

	public bool HasModSlot => modSlotId > -1;

	public virtual Slot FilterSlot1
	{
		get
		{
			if (filterSlot1Id <= -1)
			{
				return null;
			}
			return Slots[filterSlot1Id];
		}
	}

	public bool HasFilterSlot1 => filterSlot1Id > -1;

	public virtual Slot FilterSlot2
	{
		get
		{
			if (filterSlot2Id <= -1)
			{
				return null;
			}
			return Slots[filterSlot2Id];
		}
	}

	public bool HasFilterSlot2 => filterSlot2Id > -1;

	public virtual Slot FilterSlot3
	{
		get
		{
			if (filterSlot3Id <= -1)
			{
				return null;
			}
			return Slots[filterSlot3Id];
		}
	}

	public bool HasFilterSlot3 => filterSlot3Id > -1;

	public virtual Slot BackSlot
	{
		get
		{
			if (backSlot <= -1)
			{
				return null;
			}
			return Slots[backSlot];
		}
	}

	public bool HasBackSlot => backSlot > -1;

	public virtual GasFilter Filter1 => FilterSlot1.Get<GasFilter>();

	public virtual GasFilter Filter2 => FilterSlot2.Get<GasFilter>();

	public virtual GasFilter Filter3 => FilterSlot3.Get<GasFilter>();

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	public GasCanister WasteTank => WasteTankSlot.Get<GasCanister>();

	public GasCanister CoolantTank => CoolantTankSlot.Get<GasCanister>();

	public GasCanister AirTank => AirTankSlot.Occupant as GasCanister;

	public SuitModBase SuitMod => ModSlot.Get<SuitModBase>();

	public virtual float BruteDamagePassthroughAsStun => 4f;

	public virtual float HygieneReductionMultiplier => 1f;

	protected override bool HasPaintableMaskMaterial => true;

	public virtual float MovementSpeedMultiplier => 1f;

	public virtual float StormMovementSpeedMultiplier => (WeatherManager.CurrentWeatherEvent?.MovementSpeedMultiplier?.Value).GetValueOrDefault();

	public virtual float ToolSpeedMultiplier => 1f;

	public virtual float SuitVelocityAbsorbed => 9f;

	public virtual float SuitVelocityScale => 3f;

	public virtual float SuitVelocityLeakRatio => 0.005f;

	public virtual float MaxACEnergy => 350f;

	public virtual float EnergyCoolingPowerCostPercent => 0.3f;

	public virtual float EnergyHeatingPowerCostPercent => 1f;

	protected bool InSuitStorage
	{
		get
		{
			Slot parentSlot = base.ParentSlot;
			if (parentSlot != null)
			{
				return parentSlot.Parent is SuitStorage;
			}
			return false;
		}
	}

	private bool IsError
	{
		get
		{
			if (!EmptyFilter)
			{
				return base.LeakRatio > 0f;
			}
			return true;
		}
	}

	private Atmosphere BreathingAtmosphere
	{
		get
		{
			if (!ParentEntity)
			{
				return base.InternalAtmosphere;
			}
			return ParentEntity.BreathingAtmosphere;
		}
	}

	public List<SkinnedMeshRendererInstance> SkinnedMeshes { get; } = new List<SkinnedMeshRendererInstance>();

	public static TemperatureKelvin MinTempSetting => Chemistry.Temperature.ZeroDegrees;

	public static TemperatureKelvin MaxTempSetting => new TemperatureKelvin(333.15);

	public override float RadiationFactor => 0.1f;

	public override float ConvectionFactor
	{
		get
		{
			if (base.WorldAtmosphere == null || base.InternalAtmosphere == null)
			{
				return 0f;
			}
			return CalculateAdjustedConvectionFactor();
		}
	}

	public Human ParentEntity
	{
		get
		{
			Slot.Class? obj = base.ParentSlot?.Type;
			if (!obj.HasValue || obj != Slot.Class.Suit)
			{
				return null;
			}
			return base.ParentSlot?.Parent as Human;
		}
	}

	public float OutputSetting
	{
		get
		{
			return _outputSetting;
		}
		set
		{
			if (!RocketMath.Approximately(_outputSetting, value) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			_outputSetting = value;
		}
	}

	public TemperatureKelvin OutputTemperature
	{
		get
		{
			return _outputTemperature;
		}
		set
		{
			if (!RocketMath.Approximately(_outputTemperature, value) && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			_outputTemperature = value;
		}
	}

	public override bool AttackWithAllowIncomplete => true;

	public virtual bool HasFilters => Slot.Contains<GasFilter>(FilterSlot1, FilterSlot2, FilterSlot3);

	public bool EmptyFilter => _emptyFilter;

	public bool AllFiltersEmpty { get; protected set; }

	public bool LowFilter => FilterQuantitySum <= 30f;

	public MoleEnergy MaxEnergy => new MoleEnergy(maxEnergy);

	public float Efficiency => DamageState.TotalRatioClampedUndamaged;

	public virtual PressurekPa WasteMaxPressure => Chemistry.OneAtmosphere * 40.0;

	public byte SoundVolume
	{
		get
		{
			return _soundVolume;
		}
		set
		{
			_soundVolume = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			UpdateVolume().Forget();
		}
	}

	public byte SoundAlert
	{
		get
		{
			return _soundAlert;
		}
		set
		{
			_soundAlert = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			if (!GameManager.IsBatchMode)
			{
				WaitThenPlay().Forget();
			}
		}
	}

	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if ((int)value != _setting)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				_setting = (int)value;
			}
		}
	}

	public void HasPut()
	{
	}

	public override bool IsHiddenInParentSlot()
	{
		if (base.ParentSlot?.Parent is Entity)
		{
			return false;
		}
		return base.IsHiddenInParentSlot();
	}

	public CanEnterResult IsAllowedBackpackPrefab(Backpack backpack)
	{
		foreach (Item allowedBackPackPrefab in AllowedBackPackPrefabs)
		{
			if (allowedBackPackPrefab.PrefabHash == backpack.PrefabHash)
			{
				return CanEnterResult.Succeed;
			}
		}
		return CanEnterResult.Fail(GameStrings.CantEquipThisBackpack.AsString(backpack.DisplayName, DisplayName));
	}

	public virtual bool IsCoolantCritical()
	{
		if (CoolantTank == null || CoolantTank.IsBroken)
		{
			return true;
		}
		if (!(CoolantTank.InternalAtmosphere.Temperature < MinCoolantTemperatureK))
		{
			return CoolantTank.InternalAtmosphere.Temperature > MaxCoolantTemperatureK;
		}
		return true;
	}

	public virtual bool IsCoolantWarning()
	{
		if (CoolantTank == null)
		{
			return true;
		}
		if (!(CoolantTank.InternalAtmosphere.Temperature < MinCoolantTemperatureK + WarningBuffer))
		{
			return CoolantTank.InternalAtmosphere.Temperature > MaxCoolantTemperatureK - WarningBuffer;
		}
		return true;
	}

	public bool HasClosedHelmetWithAtmosphere(out Atmosphere atmosphere)
	{
		if ((bool)ParentEntity && (bool)ParentEntity.HelmetSlot.Occupant && ParentEntity.HelmetSlot.Occupant.InternalAtmosphere != null && !ParentEntity.HelmetSlot.Occupant.IsOpen)
		{
			atmosphere = ParentEntity.HelmetSlot.Occupant.InternalAtmosphere;
			return true;
		}
		atmosphere = null;
		return false;
	}

	public override StringBuilder GetSlotTooltip()
	{
		StringBuilder slotTooltip = base.GetSlotTooltip();
		if (base.ParentSlot?.Parent is SuitStorage)
		{
			if ((bool)Battery)
			{
				slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(Battery.ToTooltip(), BatterySlot.ToTooltip(), Battery.GetQuantityText().AsColor("yellow")));
			}
			if ((bool)AirTank)
			{
				slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(AirTank.ToTooltip(), AirTankSlot.ToTooltip(), AirTank.GetQuantityText().AsColor("yellow")));
			}
			if ((bool)WasteTank)
			{
				slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(WasteTank.ToTooltip(), WasteTankSlot.ToTooltip(), WasteTank.GetQuantityText().AsColor("yellow")));
			}
		}
		return slotTooltip;
	}

	private float CalculateAdjustedConvectionFactor()
	{
		float suitConvectionTime = suitConvectionData.GetSuitConvectionTime(base.WorldAtmosphere.Temperature);
		float num = 30f / suitConvectionTime * GameManager.GameTickSpeedSeconds;
		TemperatureKelvin temperature = RocketMath.Max(base.InternalAtmosphere.Temperature, TemperatureKelvin.One);
		MoleEnergy moleEnergy = new MoleEnergy((double)num * 21.1 * IdealGas.Quantity(SuitConvectionData.DefaultPressure, base.InternalAtmosphere.Volume, temperature).ToDouble());
		int num2 = 1;
		MoleEnergy moleEnergy2 = RocketMath.Abs(AtmosphereHelper.CalculateConvection(base.WorldAtmosphere, base.InternalAtmosphere, base.WorldGrid, num2, SurfaceArea, 1.0));
		if (moleEnergy2.IsDenormalOrZero())
		{
			return 0f;
		}
		return Math.Min((moleEnergy / moleEnergy2).ToFloat(), 1f);
	}

	public List<Slot> GetFilterSlots()
	{
		return _filterSlots;
	}

	public void Recharge(float ammount)
	{
		if (BatterySlot.Contains<BatteryCell>(out var occupant))
		{
			occupant.PowerStored += ammount;
		}
	}

	public override void Awake()
	{
		base.Awake();
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			if (slot.Type == Slot.Class.GasFilter)
			{
				_filterSlots.Add(slot);
			}
		}
		if (BackSlot != null)
		{
			BackSlot.OccupantAlwaysVisible = true;
		}
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		foreach (Slot slot2 in Slots)
		{
			_ = slot2;
			_logicList.Add(null);
		}
		ElectricityManager.Register(this);
		if (!IsCursor)
		{
			CircuitHolders.Register(this);
		}
		SetUpSlots();
	}

	private void SetUpSlots()
	{
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting);
			writer.WriteSingle(OutputTemperature.ToFloat());
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(_setting);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte(SoundVolume);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte(SoundAlert);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			OutputSetting = reader.ReadSingle();
			OutputTemperature = new TemperatureKelvin(reader.ReadSingle());
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			_setting = reader.ReadInt32();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			SoundVolume = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SoundAlert = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting);
		writer.WriteSingle(OutputTemperature.ToFloat());
		writer.WriteInt32(_setting);
		writer.WriteByte(SoundVolume);
		writer.WriteByte(SoundAlert);
	}

	public void RefreshSkinnedMeshCustomColor()
	{
		if (CustomColor == null)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.SetColor(CustomColor, force: true);
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (CustomColor == null)
		{
			return;
		}
		HandlePaintableMaskMaterial();
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.SetColor(CustomColor);
		}
	}

	private void HandlePaintableMaskMaterial()
	{
		Material material = Renderers[0].GetRenderer().material;
		CustomColor.ApplyToSuitMaterial(material);
	}

	private async UniTaskVoid WaitForEntity(int i)
	{
		if ((object)ParentEntity == null)
		{
			await UniTask.Delay(20000);
			if ((object)ParentEntity == null)
			{
				throw new NullReferenceException("ParentEntity is null when trying to apply Leak Effects");
			}
		}
		if (!LeakReferences[i].Visualizer.activeSelf)
		{
			float shutOffRatio = base.LeakRatio / 2f;
			LeakReferences[i].Visualizer.SetActive(value: true);
			ScaleLeakEffect(LeakReferences[i].Transform, base.LeakRatio);
			LeakReferences[i].TaskCancel = new CancellationTokenSource();
			LeakReferences[i].LeakTask = LeakEffects(i, shutOffRatio, LeakReferences[i].TaskCancel.Token);
		}
	}

	public override void EnableLeak(int index)
	{
		base.EnableLeak(index);
		if (LeakReferences == null || LeakReferences.Count <= index || LeakReferences[index].Visualizer == null)
		{
			WaitForEntity(index).Forget();
		}
		else if (!LeakReferences[index].Visualizer.activeSelf)
		{
			float shutOffRatio = base.LeakRatio / 2f;
			LeakReferences[index].Visualizer.SetActive(value: true);
			ScaleLeakEffect(LeakReferences[index].Transform, base.LeakRatio);
			LeakReferences[index].TaskCancel = new CancellationTokenSource();
			LeakReferences[index].LeakTask = LeakEffects(index, shutOffRatio, LeakReferences[index].TaskCancel.Token);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.OnOff => GameStrings.SuitContextAirConState.AsString(OnOff ? ActionStrings.Off : ActionStrings.On), 
			InteractableType.Import => GameStrings.SuitContextAirPumpState.AsString((Importing == 1) ? ActionStrings.Off : ActionStrings.On), 
			InteractableType.Export => GameStrings.SuitContextFilterState.AsString((Exporting == 1) ? ActionStrings.Off : ActionStrings.On), 
			_ => base.GetContextualName(interactable), 
		};
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Open:
		case InteractableType.OnOff:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Button1:
			if (OutputSetting >= MaxSetting)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMax);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (ParentEntity != null && ParentEntity.IsLocalPlayer)
			{
				UIAudioManager.Play(SuitButtonUpHash);
			}
			if (GameManager.RunSimulation)
			{
				OutputSetting = Mathf.Min(OutputSetting + 1f, MaxSetting);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button2:
			if (OutputSetting <= MinSetting)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMin);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (ParentEntity != null && ParentEntity.IsLocalPlayer)
			{
				UIAudioManager.Play(SuitButtonDownHash);
			}
			if (GameManager.RunSimulation)
			{
				OutputSetting = Mathf.Max(OutputSetting - 1f, MinSetting);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button3:
			if (OutputTemperature >= MaxTempSetting)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMax);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (ParentEntity != null && ParentEntity.IsLocalPlayer)
			{
				UIAudioManager.Play(SuitButtonUpHash);
			}
			if (GameManager.RunSimulation)
			{
				OutputTemperature = RocketMath.Min(OutputTemperature + TemperatureKelvin.One, MaxTempSetting);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button4:
			if (OutputTemperature <= MinTempSetting)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMin);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (ParentEntity != null && ParentEntity.IsLocalPlayer)
			{
				UIAudioManager.Play(SuitButtonDownHash);
			}
			if (GameManager.RunSimulation)
			{
				OutputTemperature = RocketMath.Max(OutputTemperature - TemperatureKelvin.One, MinTempSetting);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Import:
		case InteractableType.Export:
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		Human human = destinationSlot.Parent as Human;
		base.MoveToSlot(destinationSlot, originThing, forced);
		SetWearableVisibility(destinationSlot == human?.SuitSlot);
		if ((object)human != null && human.ToolbeltSlot.Contains<ToolBelt>(out var occupant))
		{
			occupant.CheckSuitSlot();
		}
		return true;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (base.ParentSlot?.Parent is Human && base.ParentSlot.Type == Slot.Class.Suit)
		{
			SetWearableVisibility(clothingOn: true);
			SetCustomColor(CustomColor);
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		if (base.ParentSlot.Parent is Human human && base.ParentSlot.Type == SlotType)
		{
			human.SetWearable().Forget();
		}
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		if (base.ParentSlot?.Parent != null)
		{
			Human human = base.ParentSlot.Parent as Human;
			if (human != null && human.ToolbeltSlot.Occupant is ToolBelt toolBelt)
			{
				toolBelt.SwapMesh(suitEquip: false);
			}
		}
		SetWearableVisibility(clothingOn: false);
		return base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		if (!base.IsBeingDestroyed)
		{
			if (base.LeakRatio > 0f && LeakReferences != null)
			{
				EnableLeak(UnityEngine.Random.Range(0, LeakReferences.Count));
			}
			if ((bool)ParentEntity)
			{
				WornStatusDisplay().Forget();
			}
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		base.OnDestroy();
		foreach (LeakReference leakReference in LeakReferences)
		{
			if (leakReference != null && !(leakReference.Visualizer == null))
			{
				leakReference.Visualizer.SetActive(value: false);
				if (leakReference.LeakTask.Status != UniTaskStatus.Pending)
				{
					leakReference.Cancel();
				}
			}
		}
		LeakReferences = null;
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			ElectricityManager.Deregister(this);
			if (!IsCursor)
			{
				CircuitHolders.Deregister(this);
			}
		}
	}

	public override void OnDamageDestroyed()
	{
		if (ParentEntity != null && ParentEntity.AsHuman != null)
		{
			ParentEntity.PlaySound(_destroySuitHash);
		}
		base.OnDamageDestroyed();
	}

	public void ForceClothingVisible(bool clothingOn)
	{
		SetWearableVisibility(clothingOn);
	}

	public void RefreshVisibility()
	{
		Human human = base.ParentSlot.Parent as Human;
		if (!human || !base.IsChild)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			if (human.IsLocalPlayer)
			{
				skinnedMesh.Parent.GameObject.layer = Layers.PlayerImmune;
				skinnedMesh.Renderer.updateWhenOffscreen = true;
			}
			else
			{
				skinnedMesh.Parent.GameObject.layer = Layers.Default;
				skinnedMesh.Renderer.updateWhenOffscreen = false;
			}
		}
	}

	public void SetWearableVisibility(bool clothingOn)
	{
		if (!base.IsChild)
		{
			return;
		}
		if (!(base.ParentSlot.Parent is Human human) || base.ParentSlot.Type != SlotType || !clothingOn)
		{
			SkinnedMeshes.Clear();
			return;
		}
		human.ToolbeltSlot.Get<ToolBelt>()?.CheckSuitSlot();
		SkinnedMeshes.Clear();
		human.CosmeticsBehaviour.ApplyClothing(_suitAsset);
		human.CosmeticsBehaviour.ApplyArmor(null);
		SkinnedMeshRenderer bodyRenderer = human.CosmeticsBehaviour.BodyRenderer;
		LeakReferences = human.LeakReferences;
		_baseLeakScale = human.LeakReferences[0].Transform.localScale;
		if (base.LeakRatio > 0f)
		{
			EnableLeak(UnityEngine.Random.Range(0, LeakReferences.Count));
		}
		SkinnedMeshRendererInstance skinnedMeshRendererInstance = new SkinnedMeshRendererInstance
		{
			Parent = this,
			Renderer = bodyRenderer,
			PaintableIndex = ColorMaterialIndex,
			SetColorMaskOnMainMaterial = true
		};
		if (human.IsLocalPlayer)
		{
			bodyRenderer.gameObject.layer = Layers.PlayerImmune;
			skinnedMeshRendererInstance.Renderer.updateWhenOffscreen = true;
		}
		else
		{
			bodyRenderer.gameObject.layer = Layers.Default;
		}
		SkinnedMeshes.Add(skinnedMeshRendererInstance);
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.SetColor(CustomColor);
		}
		if (base.gameObject.activeInHierarchy)
		{
			WornStatusDisplay().Forget();
		}
	}

	private void EvaluateModules()
	{
		float num = 0f;
		num += DoCooling(num);
		num += DoHeating(num);
		DoAirflow();
		DoFilter();
		DoOverPressure();
		if (Battery != null)
		{
			Battery.PowerStored -= num;
		}
	}

	protected virtual void DoAirflow()
	{
		SuitModuleHelper.TakeGasFromAirTank(this);
	}

	protected virtual void DoOverPressure()
	{
		SuitModuleHelper.HandleSuitOverPressure(this);
	}

	protected virtual void DoFilter()
	{
		SuitModuleHelper.HandleFilters(this);
	}

	protected virtual float DoHeating(float powerUsedTotal)
	{
		if (OnOff && (object)Battery != null && !Battery.IsEmpty)
		{
			powerUsedTotal += SuitModuleHelper.Heat(base.InternalAtmosphere, OutputTemperature, MaxEnergy, EnergyHeatingPowerCostPercent);
			if (HasClosedHelmetWithAtmosphere(out var atmosphere))
			{
				powerUsedTotal += SuitModuleHelper.Heat(atmosphere, OutputTemperature, MaxEnergy, EnergyHeatingPowerCostPercent);
			}
		}
		return powerUsedTotal;
	}

	public virtual float DoCooling(float powerUsedTotal)
	{
		if (CoolantTankSlot != null)
		{
			return LiquidCooling();
		}
		return GasCooling();
	}

	private float GasCooling()
	{
		throw new NotImplementedException();
	}

	private float LiquidCooling()
	{
		float num = 0f;
		if (OnOff && (object)Battery != null && !Battery.IsEmpty && (object)CoolantTank != null)
		{
			float num2 = 1f;
			if (CoolantTank.InternalAtmosphere.Temperature > MaxCoolantTemperatureK)
			{
				num2 = RocketMath.MapToScaleClamp(MaxCoolantTemperatureK.ToFloat(), (MaxCoolantTemperatureK + WarningBuffer).ToFloat(), 0.5f, 0.1f, CoolantTank.InternalAtmosphere.Temperature.ToFloat());
			}
			if (CoolantTank.InternalAtmosphere.Temperature < MinCoolantTemperatureK)
			{
				num2 = RocketMath.MapToScaleClamp((MinCoolantTemperatureK - WarningBuffer).ToFloat(), MinCoolantTemperatureK.ToFloat(), 0.1f, 0.5f, CoolantTank.InternalAtmosphere.Temperature.ToFloat());
			}
			float num3 = 1f;
			if (CoolantTank.InternalAtmosphere.Temperature > base.InternalAtmosphere.Temperature)
			{
				num3 = RocketMath.MapToScaleClamp(0f, 100f, 1f, 0.25f, (CoolantTank.InternalAtmosphere.Temperature - base.InternalAtmosphere.Temperature).ToFloat());
			}
			MoleEnergy val = new MoleEnergy(MaxACEnergy * num2 * num3);
			if (CoolantTank.InternalAtmosphere.Temperature < MaxCoolantTemperatureK + WarningBuffer)
			{
				TemperatureKelvin temperatureKelvin = MaxCoolantTemperatureK + WarningBuffer - CoolantTank.InternalAtmosphere.Temperature;
				MoleEnergy val2 = IdealGas.Energy(CoolantTank.InternalAtmosphere.GasMixture.HeatCapacity, temperatureKelvin);
				val = RocketMath.Min(val, val2);
			}
			else
			{
				val = MoleEnergy.Zero;
			}
			float energyCoolingPowerCostPercent = EnergyCoolingPowerCostPercent / num2 / num3;
			num += SuitModuleHelper.Cool(base.InternalAtmosphere, CoolantTank.InternalAtmosphere, OutputTemperature, val, energyCoolingPowerCostPercent);
			if (HasClosedHelmetWithAtmosphere(out var atmosphere))
			{
				num += SuitModuleHelper.Cool(atmosphere, CoolantTank.InternalAtmosphere, OutputTemperature, val, energyCoolingPowerCostPercent);
			}
			PressurekPa pressurekPa = new PressurekPa(4053.0);
			PressurekPa pressurekPa2 = new PressurekPa(4053.0);
			if (CoolantTank.InternalAtmosphere.PressureGassesAndLiquids > pressurekPa && (bool)WasteTank)
			{
				PressurekPa val3 = CoolantTank.InternalAtmosphere.PressureGassesAndLiquids - pressurekPa;
				PressurekPa val4 = pressurekPa2 - WasteTank.InternalAtmosphere.PressureGassesAndLiquids;
				PressurekPa val5 = RocketMath.Min(val3, val4);
				val5 = RocketMath.Max(PressurekPa.Zero, val5);
				MoleQuantity transferMoles = IdealGas.Quantity(val5, CoolantTank.InternalAtmosphere.GetGasVolume(), CoolantTank.InternalAtmosphere.Temperature);
				GasMixture gasMixture = CoolantTank.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
				WasteTank.InternalAtmosphere.Add(gasMixture);
			}
		}
		return num;
	}

	public override void OnAtmosphericTick()
	{
		if (GameManager.GameState == GameState.Running && !InSuitStorage && (object)ParentEntity != null)
		{
			CheckPowerState();
			EvaluateModules();
			CheckActivateState();
			CheckAtmosphereState();
		}
		base.OnAtmosphericTick();
	}

	private void CheckAtmosphereState()
	{
		if (Powered)
		{
			bool isError = IsError;
			if (Error == 0 && isError)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			OnServer.Interact(base.InteractError, 0);
		}
	}

	private void CheckActivateState()
	{
		bool flag = !HasFilters || AllFiltersEmpty;
		bool flag2 = HasWasteTankSlot && ((object)WasteTank == null || WasteTank.InternalAtmosphere == null || WasteTank.InternalAtmosphere.PressureGassesAndLiquids >= WasteMaxPressure);
		if (Activate != 0 && AirTank == null && (flag || flag2))
		{
			OnServer.Interact(this, InteractableType.Activate, 0);
			return;
		}
		if (Activate != 1 && (bool)AirTank && (flag || flag2))
		{
			OnServer.Interact(this, InteractableType.Activate, 1);
			return;
		}
		bool flag3 = HasFilters && !AllFiltersEmpty;
		int num;
		if (HasWasteTankSlot)
		{
			GasCanister wasteTank = WasteTank;
			num = (((object)wasteTank != null && wasteTank.InternalAtmosphere != null && WasteTank.InternalAtmosphere.PressureGassesAndLiquids < WasteMaxPressure) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag4 = (byte)num != 0;
		if (Activate != 2 && AirTank == null && flag3 && flag4)
		{
			OnServer.Interact(this, InteractableType.Activate, 2);
		}
		else if (Activate != 3 && (bool)AirTank && flag3 && flag4)
		{
			OnServer.Interact(this, InteractableType.Activate, 3);
		}
	}

	public void UpdateEmptyFilter()
	{
		int num = 0;
		FilterQuantitySum = 0f;
		foreach (Slot slot in Slots)
		{
			if (slot.Type == Slot.Class.GasFilter && slot.Contains<GasFilter>(out var occupant))
			{
				FilterQuantitySum += occupant.Quantity;
				num++;
			}
		}
		_emptyFilter = Math.Abs(FilterQuantitySum) <= 10f;
		AllFiltersEmpty = num == 0;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SuitBaseSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SuitBaseSaveData suitBaseSaveData)
		{
			OutputSetting = suitBaseSaveData.OutputSetting;
			OutputTemperature = new TemperatureKelvin(suitBaseSaveData.OutputTemperatureSetting);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		SuitBaseSaveData suitBaseSaveData = savedData as SuitBaseSaveData;
		if (GameManager.GameState != GameState.None && suitBaseSaveData != null)
		{
			suitBaseSaveData.OutputSetting = OutputSetting;
			suitBaseSaveData.OutputTemperatureSetting = OutputTemperature.ToFloat();
		}
	}

	private async UniTask LeakEffects(int leakIndex, float shutOffRatio, CancellationToken cancelToken)
	{
		bool isOn = false;
		LeakReferences[leakIndex].LeakAudio = PlayPooledAudioSound(AirLeakHash, LeakReferences[leakIndex].Transform.localPosition);
		while (base.LeakRatio > shutOffRatio && ParentEntity != null)
		{
			if (LeakReferences[leakIndex].Visualizer != null)
			{
				LeakReferences[leakIndex].Visualizer.SetActive(!isOn);
			}
			isOn = !isOn;
			Vector3 baseLeakScale = _baseLeakScale;
			baseLeakScale.y *= UnityEngine.Random.Range(0.8f, 1.2f);
			LeakReferences[leakIndex].Transform.localScale = baseLeakScale;
			await UniTask.Delay(UnityEngine.Random.Range(10, 100), ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		if (LeakReferences[leakIndex]?.Visualizer != null)
		{
			LeakReferences[leakIndex].Visualizer.SetActive(value: false);
		}
		if (LeakReferences[leakIndex]?.LeakAudio != null)
		{
			LeakReferences[leakIndex].LeakAudio.Stop(AirLeakHash);
		}
	}

	private void ScaleLeakEffect(Transform leakReference, float leakRatio)
	{
		leakReference.localScale = _baseLeakScale * (leakRatio * 0.5f);
	}

	protected void CheckPowerState()
	{
		BatteryCell batteryCell = BatterySlot.Get<BatteryCell>();
		if (Powered && ((object)batteryCell == null || batteryCell.IsEmpty))
		{
			OnServer.Interact(base.InteractPowered, 0, skipAnimation: true);
		}
		else if (!Powered && (object)batteryCell != null && !batteryCell.IsEmpty)
		{
			OnServer.Interact(base.InteractPowered, 1, skipAnimation: true);
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		SetState(parent);
		Redraw();
		CheckPowerState();
		CheckActivateState();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		SetState(base.ParentSlot?.Parent);
		Redraw();
		CheckPowerState();
		CheckActivateState();
	}

	private void SetState(Thing parent)
	{
		if (!(parent is Human))
		{
			if (parent is SuitStorage)
			{
				_currentState = SuitState.Okay;
			}
			else
			{
				_currentState = SuitState.Off;
			}
		}
	}

	private void Redraw()
	{
		if (_materials != null && _materials.ValidIndex(StateMaterialIndex))
		{
			Material[] materials = _materials;
			int stateMaterialIndex = StateMaterialIndex;
			materials[stateMaterialIndex] = _currentState switch
			{
				SuitState.Off => ColorOff, 
				SuitState.Okay => ColorOkay, 
				SuitState.Error => ColorError, 
				_ => _materials[StateMaterialIndex], 
			};
		}
	}

	private async UniTaskVoid WornStatusDisplay()
	{
		_materials = ((SkinnedMeshes.Count <= 0) ? null : SkinnedMeshes[0]?.Renderer?.materials);
		_currentState = SuitState.None;
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while ((bool)ParentEntity && !IsOccluded)
		{
			SuitState suitState = SuitState.Off;
			if (Powered)
			{
				suitState = ((Error == 0) ? SuitState.Okay : SuitState.Error);
			}
			if (_materials != null && SkinnedMeshes.Count > 0 && (object)SkinnedMeshes[0]?.Renderer != null)
			{
				if (suitState != _currentState && !IsEmergency && _currentState >= SuitState.None && _materials.Length > (int)_currentState)
				{
					_currentState = suitState;
					Redraw();
					SkinnedMeshes[0].Renderer.materials = _materials;
				}
				if (_currentState == SuitState.Error)
				{
					_materials[StateMaterialIndex] = (_doFlash ? ColorErrorFlash : ColorError);
					SkinnedMeshes[0].Renderer.materials = _materials;
					_doFlash = !_doFlash;
				}
			}
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public List<ILogicable> GetBatchOutput()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			_logicList[i] = slot.Get<ILogicable>();
		}
		return _logicList;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.GameState == GameState.Running && ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			occupant.Reset();
			ClearError();
		}
		ReParentSuitBackOccupant(newChild);
	}

	private void ReParentSuitBackOccupant(DynamicThing newChild)
	{
		if (newChild.ParentSlot == BackSlot && !(ParentEntity == null))
		{
			Transform location = ParentEntity.BackpackSlot.Location;
			newChild.Transform.SetParent(location, worldPositionStays: false);
			newChild.SetVisibility(isVisible: true);
		}
	}

	public async UniTask HaltAndCatchFire()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
			if (ThingTransform.GetCancellationTokenOnDestroy().IsCancellationRequested)
			{
				return;
			}
		}
		base.InternalAtmosphere.Sparked = true;
		global::Explosion.Explode(200f, RootParent.Position, 2.3f);
		OnServer.Destroy(this);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!Powered && _playingAudio != null)
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		else if (Powered && _playingAudio == null && SoundAlert != 0 && !GameManager.IsBatchMode)
		{
			WaitThenPlay().Forget();
		}
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}

	private async UniTaskVoid UpdateVolume()
	{
		await UniTask.SwitchToMainThread();
		if ((bool)_playingAudio)
		{
			_playingAudio.GameAudioSource?.SetVolumeMultiplier(AudioManager.Find((SoundAlert)SoundAlert)?.NameHash ?? 0, (float)(int)SoundVolume / 100f);
		}
	}

	private async UniTaskVoid WaitThenPlay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (_playingAudio != null && PooledAudioSources.Contains(_playingAudio))
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		if (SoundAlert != 0)
		{
			_playingAudio = Thing.PlayPooledAudioSound(this, Singleton<AudioManager>.Instance.GetChannelData(Defines.SoundChannel.Medium));
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_setting = reader.ReadInt32();
		SoundVolume = reader.ReadByte();
		SoundAlert = reader.ReadByte();
		OutputSetting = reader.ReadSingle();
		OutputTemperature = new TemperatureKelvin(reader.ReadSingle());
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && BatterySlot != null && BatterySlot.Contains<BatteryCell>(out var occupant) && !occupant.IsEmpty && ChipSlot.Contains<ProgrammableChip>(out var occupant2))
		{
			occupant.PowerStored -= 2.5f;
			if (!occupant2.CompilationError)
			{
				occupant2.Execute(128);
			}
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureExternal:
		case LogicType.Setting:
		case LogicType.Volume:
		case LogicType.PressureSetting:
		case LogicType.TemperatureSetting:
		case LogicType.TemperatureExternal:
		case LogicType.Filtration:
		case LogicType.AirRelease:
		case LogicType.PositionX:
		case LogicType.PositionY:
		case LogicType.PositionZ:
		case LogicType.VelocityMagnitude:
		case LogicType.VelocityRelativeX:
		case LogicType.VelocityRelativeY:
		case LogicType.VelocityRelativeZ:
		case LogicType.SoundAlert:
		case LogicType.ForwardX:
		case LogicType.ForwardY:
		case LogicType.ForwardZ:
		case LogicType.Orientation:
		case LogicType.VelocityX:
		case LogicType.VelocityY:
		case LogicType.VelocityZ:
		case LogicType.EntityState:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Error:
		case LogicType.Setting:
		case LogicType.Volume:
		case LogicType.PressureSetting:
		case LogicType.TemperatureSetting:
		case LogicType.Filtration:
		case LogicType.AirRelease:
		case LogicType.SoundAlert:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.EntityState:
			if ((object)base.ParentSlot?.Parent == null || base.ParentSlot.Type != Slot.Class.Suit)
			{
				return -1.0;
			}
			return ((int?)ParentEntity?.State) ?? (-1);
		case LogicType.TemperatureExternal:
			return base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0;
		case LogicType.PressureExternal:
			return base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.PressureSetting:
			return OutputSetting;
		case LogicType.TemperatureSetting:
			return OutputTemperature.ToDouble();
		case LogicType.AirRelease:
			return Importing;
		case LogicType.Filtration:
			return Exporting;
		case LogicType.PositionX:
			return RootParent.Position.x;
		case LogicType.PositionY:
			return RootParent.Position.y;
		case LogicType.PositionZ:
			return RootParent.Position.z;
		case LogicType.VelocityMagnitude:
			return base.VelocityMagnitude;
		case LogicType.VelocityRelativeX:
			return RootParentHuman ? RootParentHuman.RelativeVelocity.x : RelativeVelocity.x;
		case LogicType.VelocityRelativeY:
			return RootParentHuman ? RootParentHuman.RelativeVelocity.y : RelativeVelocity.y;
		case LogicType.VelocityRelativeZ:
			return RootParentHuman ? RootParentHuman.RelativeVelocity.z : RelativeVelocity.z;
		case LogicType.VelocityX:
			return RootParentHuman ? RootParentHuman.Velocity.x : base.Velocity.x;
		case LogicType.VelocityY:
			return RootParentHuman ? RootParentHuman.Velocity.y : base.Velocity.y;
		case LogicType.VelocityZ:
			return RootParentHuman ? RootParentHuman.Velocity.z : base.Velocity.z;
		case LogicType.ForwardX:
			return RootParentHuman ? RootParentHuman.EntityForward.x : Forward.x;
		case LogicType.ForwardY:
			return RootParentHuman ? RootParentHuman.EntityForward.y : Forward.y;
		case LogicType.ForwardZ:
			return RootParentHuman ? RootParentHuman.EntityForward.z : Forward.z;
		case LogicType.Orientation:
			return Orientation;
		case LogicType.Setting:
			return Setting;
		case LogicType.Volume:
			return (int)SoundVolume;
		case LogicType.SoundAlert:
			return (int)SoundAlert;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.PressureSetting:
			OutputSetting = (float)value;
			break;
		case LogicType.TemperatureSetting:
			OutputTemperature = new TemperatureKelvin(value);
			break;
		case LogicType.Error:
			OnServer.Interact(base.InteractError, (int)value);
			break;
		case LogicType.AirRelease:
			OnServer.Interact(base.InteractImport, (int)value);
			break;
		case LogicType.Filtration:
			OnServer.Interact(base.InteractExport, (int)value);
			break;
		case LogicType.Setting:
			Setting = value;
			break;
		case LogicType.Volume:
			SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100);
			break;
		case LogicType.SoundAlert:
			SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1);
			break;
		}
	}

	public void ClearError()
	{
	}

	public void RaiseError(int state)
	{
	}

	public List<LogicBinding> GetLogicBindings()
	{
		return new List<LogicBinding>
		{
			new LogicBinding("SUIT"),
			new LogicBinding(0, "HELMET"),
			new LogicBinding(1, "BACKPACK"),
			new LogicBinding(2, "TOOLBELT"),
			new LogicBinding(3, "GLASSES"),
			new LogicBinding(4, "LEFT_HAND"),
			new LogicBinding(5, "RIGHT_HAND")
		};
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		return deviceIndex switch
		{
			int.MaxValue => this, 
			0 => ParentEntity?.HelmetSlot.Get<ILogicable>(), 
			1 => ParentEntity?.BackpackSlot.Get<ILogicable>(), 
			2 => ParentEntity?.ToolbeltSlot.Get<ILogicable>(), 
			3 => ParentEntity?.GlassesSlot.Get<ILogicable>(), 
			4 => ParentEntity?.LeftHandSlot.Get<ILogicable>(), 
			5 => ParentEntity?.RightHandSlot.Get<ILogicable>(), 
			_ => null, 
		};
	}

	public ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue)
	{
		if (deviceId == 0L)
		{
			return null;
		}
		ILogicable logicable = Referencable.Find<ILogicable>(deviceId);
		if (logicable == null)
		{
			return null;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == logicable)
			{
				return logicable;
			}
		}
		return null;
	}

	public bool IsValidIndex(int index)
	{
		if (index != int.MaxValue && index != 0 && index != 1)
		{
			return index == 2;
		}
		return true;
	}

	public void SetDeviceLabel(int index, string label)
	{
	}

	public void SetSourceCode(string sourceCode)
	{
		if (ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			occupant.SetSourceCode(sourceCode, this);
			occupant.SendUpdate();
		}
	}

	public string GetSourceCode()
	{
		AsciiString? asciiString = ChipSlot.Get<ProgrammableChip>()?.GetSourceCode();
		if (!asciiString.HasValue)
		{
			return string.Empty;
		}
		return asciiString.GetValueOrDefault();
	}

	public double ReadMemory(int address)
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		return occupant.ReadMemory(address);
	}

	public void WriteMemory(int address, double value)
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		occupant.WriteMemory(address, value);
	}

	public void ClearMemory()
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		occupant.ClearMemory();
	}

	public int GetStackSize()
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			return 0;
		}
		return occupant?.GetStackSize() ?? 0;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (HasChipSlot && ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			extendedText.Append(occupant.GetErrorCode());
		}
		return extendedText;
	}

	public virtual bool AllowedInBack(DynamicThing thing)
	{
		return true;
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_circuitHolderPool.CanAddToPool(densePool))
		{
			return _circuitHolderPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_circuitHolderPool.OnRemovedFrom(densePool);
	}
}
