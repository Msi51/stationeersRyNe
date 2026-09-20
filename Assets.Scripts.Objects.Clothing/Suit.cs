using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Clothing;

public class Suit : AtmosphericItem, ISuit, IReferencable, IEvaluable, IWearable, IFullBody, IBatteryPowered, IPowered, IDensePoolable, IInternalConditioner
{
	private enum SuitState
	{
		None,
		Off,
		Okay,
		Error
	}

	public static string MaterialMaskColorPropertyName = "_MaskColor";

	private IInternalConditionerHandler _conditioningHandler;

	[Header("Suit")]
	[SerializeField]
	private KitItem _suitAsset;

	[FormerlySerializedAs("PressurePerTick")]
	public float pressurePerTick = 101.325f;

	private float _outputSetting = 101.325f;

	private TemperatureKelvin _outputTemperature = new TemperatureKelvin(293.15);

	public static float MinSetting = 0f;

	public static float MaxSetting = 202.65f;

	private Vector3 _baseLeakScale;

	private Coroutine EntityWaitCoroutine;

	private List<Slot> _filterSlots = new List<Slot>();

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	private const int WAIT_FOR_ENTITY_MS = 20000;

	public Event OnSuitTemperatureChanged;

	public Event OnSuitPressureChanged;

	private static readonly int SuitButtonUpHash = Animator.StringToHash("SuitButtonUp");

	private static readonly int SuitButtonDownHash = Animator.StringToHash("SuitButtonDown");

	private int _destroySuitHash = Animator.StringToHash("DestroySuit");

	[Tooltip("Max velocity a suit can completely absorb force without taking any damage")]
	public float SuitVelocityAbsorbed = 12f;

	[Tooltip("Scales the velocity damage, lower the number the less damage taken")]
	public float SuitVelocityScale = 1f;

	[Tooltip("Scales the velocity leak damage, lower the number the less damage taken")]
	public float SuitVelocityLeakRatio = 0.005f;

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

	public const float DEFAULT_MAX_WASTE_PRESSURE = 4053f;

	public float wasteMaxPressure = 4053f;

	public Thing AsThing => this;

	public IRepairable AsRepairable => this;

	public virtual float HygieneReductionMultiplier => 1f;

	protected override bool HasPaintableMaskMaterial => true;

	public GasCanister CoolantTank { get; }

	public GasCanister AirTank => AirTankSlot.Get<GasCanister>();

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	public GasCanister WasteTank => WasteTankSlot.Get<GasCanister>();

	public virtual GasFilter Filter1 => FilterSlot1.Get<GasFilter>();

	public virtual GasFilter Filter2 => FilterSlot2.Get<GasFilter>();

	public virtual GasFilter Filter3 => FilterSlot3.Get<GasFilter>();

	public Slot AirTankSlot => Slots[0];

	public Slot WasteTankSlot => Slots[1];

	public Slot BatterySlot => Slots[2];

	public bool HasWasteTankSlot => true;

	public virtual Slot FilterSlot1 => Slots[3];

	public virtual Slot FilterSlot2 => Slots[4];

	public virtual Slot FilterSlot3 => Slots[5];

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

	[Tooltip("Instances of the actual renderer of the created objects")]
	public List<SkinnedMeshRendererInstance> SkinnedMeshes { get; } = new List<SkinnedMeshRendererInstance>();

	public PressurekPa PressurePerTick => new PressurekPa(pressurePerTick);

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

	float ISuit.SuitVelocityAbsorbed => SuitVelocityAbsorbed;

	float ISuit.SuitVelocityScale => SuitVelocityScale;

	float ISuit.SuitVelocityLeakRatio => SuitVelocityLeakRatio;

	private bool InSuitStorage
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

	public virtual bool HasFilters => Slot.Contains<GasFilter>(FilterSlot1, FilterSlot2, FilterSlot3);

	public bool EmptyFilter => _emptyFilter;

	public bool AllFiltersEmpty { get; protected set; }

	public bool LowFilter => FilterQuantitySum <= 30f;

	public float MovementSpeedMultiplier => 1f;

	public MoleEnergy MaxEnergy => new MoleEnergy(maxEnergy);

	public float Efficiency => DamageState.TotalRatioClampedUndamaged;

	public PressurekPa WasteMaxPressure => new PressurekPa(wasteMaxPressure);

	PressurekPa ISuit.WasteMaxPressure => WasteMaxPressure;

	public override bool IsHiddenInParentSlot()
	{
		if (base.ParentSlot?.Parent is Entity)
		{
			return false;
		}
		return base.IsHiddenInParentSlot();
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

	public Atmosphere GetInternalAtmosphere()
	{
		return base.InternalAtmosphere;
	}

	public void SetInternalAtmosphere(Atmosphere atmosphere)
	{
		base.InternalAtmosphere = atmosphere;
	}

	public float GetOutputSetting()
	{
		return OutputSetting;
	}

	public TemperatureKelvin GetOutputTemperature()
	{
		return OutputTemperature;
	}

	public PressurekPa GetPressurePerTick()
	{
		return PressurePerTick;
	}

	public PressurekPa GetWasteMaxPressure()
	{
		return WasteMaxPressure;
	}

	public MoleEnergy GetMaxEnergy()
	{
		return MaxEnergy;
	}

	public List<Slot> GetFilterSlots()
	{
		return _filterSlots;
	}

	public void SetConditioningHandler(IInternalConditionerHandler handler)
	{
		_conditioningHandler = handler;
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
		if (!IsCursor && this is ICircuitHolder iCircuitHolder)
		{
			CircuitHolders.Register(iCircuitHolder);
		}
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

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting);
			writer.WriteSingle(OutputTemperature.ToFloat());
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
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting);
		writer.WriteSingle(OutputTemperature.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputSetting = reader.ReadSingle();
		OutputTemperature = new TemperatureKelvin(reader.ReadSingle());
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
			if (OnSuitPressureChanged != null)
			{
				OnSuitPressureChanged();
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
			if (OnSuitPressureChanged != null)
			{
				OnSuitPressureChanged();
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button3:
			if (OutputTemperature >= Chemistry.Temperature.MAXSuitSetting)
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
			if (OnSuitTemperatureChanged != null)
			{
				OnSuitTemperatureChanged();
			}
			if (GameManager.RunSimulation)
			{
				OutputTemperature = RocketMath.Min(OutputTemperature + TemperatureKelvin.One, Chemistry.Temperature.MAXSuitSetting);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button4:
			if (OutputTemperature <= Chemistry.Temperature.MINSuitSetting)
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
			if (OnSuitTemperatureChanged != null)
			{
				OnSuitTemperatureChanged();
			}
			if (GameManager.RunSimulation)
			{
				OutputTemperature = RocketMath.Max(OutputTemperature - TemperatureKelvin.One, Chemistry.Temperature.MINSuitSetting);
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
		if (!IsCursor && this is ICircuitHolder iCircuitHolder)
		{
			CircuitHolders.Deregister(iCircuitHolder);
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

	public override void OnAtmosphericTick()
	{
		if (GameManager.GameState == GameState.Running && !InSuitStorage && (object)ParentEntity != null)
		{
			CheckPowerState();
			MoleQuantity zero = MoleQuantity.Zero;
			zero += _conditioningHandler.AirConditioning(base.InternalAtmosphere);
			if ((bool)ParentEntity && (bool)ParentEntity.HelmetSlot.Occupant && ParentEntity.HelmetSlot.Occupant.InternalAtmosphere != null && !ParentEntity.HelmetSlot.Occupant.IsOpen)
			{
				zero += _conditioningHandler.AirConditioning(ParentEntity.HelmetSlot.Occupant.InternalAtmosphere);
			}
			_ = MoleQuantity.Zero;
			_ = MoleQuantity.Zero;
			if (zero > MoleQuantity.Zero)
			{
				_conditioningHandler.SetGasToTank(zero);
				_conditioningHandler.GetGasFromTank();
			}
			else
			{
				_conditioningHandler.GetGasFromTank();
				_conditioningHandler.SetGasToTank(zero);
			}
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
		if (Activate != 0 && AirTank == null && (!HasFilters || AllFiltersEmpty || WasteTank?.InternalAtmosphere == null || WasteTank.InternalAtmosphere.PressureGassesAndLiquids >= WasteMaxPressure))
		{
			OnServer.Interact(this, InteractableType.Activate, 0);
		}
		else if (Activate != 1 && (bool)AirTank && (!HasFilters || AllFiltersEmpty || WasteTank?.InternalAtmosphere == null || WasteTank.InternalAtmosphere.PressureGassesAndLiquids >= WasteMaxPressure))
		{
			OnServer.Interact(this, InteractableType.Activate, 1);
		}
		else if (Activate != 2 && AirTank == null && HasFilters && !AllFiltersEmpty && WasteTank?.InternalAtmosphere != null && WasteTank.InternalAtmosphere.PressureGassesAndLiquids < WasteMaxPressure)
		{
			OnServer.Interact(this, InteractableType.Activate, 2);
		}
		else if (Activate != 3 && (bool)AirTank && HasFilters && !AllFiltersEmpty && WasteTank?.InternalAtmosphere != null && WasteTank.InternalAtmosphere.PressureGassesAndLiquids < WasteMaxPressure)
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
		ThingSaveData savedData = new SuitSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SuitSaveData suitSaveData)
		{
			OutputSetting = suitSaveData.OutputSetting;
			OutputTemperature = new TemperatureKelvin(suitSaveData.OutputTemperatureSetting);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		SuitSaveData suitSaveData = savedData as SuitSaveData;
		if (GameManager.GameState != GameState.None && suitSaveData != null)
		{
			suitSaveData.OutputSetting = OutputSetting;
			suitSaveData.OutputTemperatureSetting = OutputTemperature.ToFloat();
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

	private void CheckPowerState()
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
}
