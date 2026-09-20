using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Reagents;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class FurnaceBase : DeviceInputOutputImportExport, IPrefabHash, IThermal, IConsumesAllOres, IResourceConsumer, IReferencable, IEvaluable
{
	public delegate void OnFurnaceOpened(long state);

	[Tooltip("The volume of the internal atmosphere of the furnace")]
	[FormerlySerializedAs("Volume")]
	[SerializeField]
	private float volume = 250f;

	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public static float LerpSpeed = 2f;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum = 280f;

	[Tooltip("The needle (required)")]
	public GameObject Needle;

	[Tooltip("The collider for tank display")]
	public Collider InfoPanel;

	[Tooltip("The collider for interior display")]
	public Collider WindowPanel;

	[Tooltip("The renderer that will display the flame in the furnace")]
	public Renderer FlameRenderer;

	[SerializeField]
	private AnimationCurve radiationCurve = new AnimationCurve(new Keyframe(400f, 0.1f), new Keyframe(2300f, 0.75f), new Keyframe(5000f, 1f));

	public static readonly PressurekPa MAXPressureDelta = new PressurekPa(60795.0);

	private static readonly int FurnaceSparkHash = Animator.StringToHash("FurnaceSpark");

	private static readonly int FurnaceSmeltHash = Animator.StringToHash("FurnaceSmelt");

	private PressurekPa _pressureRating;

	private float _needleRotation;

	private bool _inflamedState;

	[ReadOnly]
	public bool Inflamed;

	private float _cleanBurnRate;

	private float _intensity;

	private float _temperatureRatio;

	private float _pressureRatio;

	private Transform _needleTransform;

	private float _lastAngleNeedle;

	private Quaternion _needleBaseRotation;

	private Material _flameMaterial;

	public Ore SlagPrefab;

	[NonSerialized]
	public IQuantity SmelterResult;

	[NonSerialized]
	public ReagentMixture RatioMix;

	[NonSerialized]
	public Recipe CurrentRecipe;

	public AnimationCurve PressureGaugeCurve;

	public float ExplosionForce = 1850f;

	public float ExplosionRadius = 5f;

	public float MaxExplosionRadius = 10f;

	[NonSerialized]
	public bool HasBlown;

	private static System.Random _random = new System.Random();

	private static readonly int InflamedLowHash = Animator.StringToHash("InflamedLow");

	private static readonly int InflamedHighHash = Animator.StringToHash("InflamedHigh");

	public AnimationCurve InflamedLowAudioCurve;

	public AnimationCurve InflamedHighAudioCurve;

	private bool _playSounds;

	private static readonly TemperatureKelvin inflamedTemperatureThreshold = new TemperatureKelvin(473.15);

	private float _audioIntensityRatio;

	private static readonly float AudioIntensityMax = 10f;

	private const float AUDIBLE_SQUARE_DISTANCE = 400f;

	private static readonly int FurnaceStressedHash = Animator.StringToHash("FurnaceStressed");

	private bool _isStressedSound;

	private static readonly int ActivateButtonHash = Animator.StringToHash("ActivateButton");

	private const int ACTIVATE_DELAY = 100;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override float RadiationFactor
	{
		get
		{
			if (radiationCurve == null || base.InternalAtmosphere == null)
			{
				return ThermodynamicsScale;
			}
			return radiationCurve.Evaluate(base.InternalAtmosphere.Temperature.ToFloat());
		}
	}

	public virtual int CurrentHash
	{
		get
		{
			if (SmelterResult == null)
			{
				return 0;
			}
			return SmelterResult.GetPrefabHash();
		}
		set
		{
		}
	}

	public override bool CanIceMelt => false;

	public override bool HasReadableReagentMixture => true;

	public override bool HasReadableAtmosphere => true;

	public bool PlaySounds
	{
		get
		{
			return _playSounds;
		}
		set
		{
			if (value != _playSounds)
			{
				if (value)
				{
					PlaySound(InflamedLowHash);
					PlaySound(InflamedHighHash);
				}
				else
				{
					StopSound(InflamedLowHash);
					StopSound(InflamedHighHash);
				}
			}
			_playSounds = value;
		}
	}

	public bool IsStressedSound
	{
		get
		{
			return _isStressedSound;
		}
		set
		{
			if (value == IsStressedSound)
			{
				return;
			}
			_isStressedSound = value;
			if (IsStressedSound)
			{
				if (ThreadedManager.IsThread)
				{
					PlaySoundFromThread(FurnaceStressedHash).Forget();
				}
				else
				{
					PlaySound(FurnaceStressedHash);
				}
			}
			else if (ThreadedManager.IsThread)
			{
				StopSoundFromThread(FurnaceStressedHash).Forget();
			}
			else
			{
				StopSound(FurnaceStressedHash);
			}
		}
	}

	public event Event OnFurnaceActivated;

	public event OnFurnaceOpened OnFurnaceOpenedEvent;

	public event Event OnInternalPressureHit;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public virtual List<Item> GetResourcesUsed()
	{
		return new List<Item>(Ore.AllOrePrefabs);
	}

	public virtual bool CanProcess(Recipe recipe)
	{
		foreach (Ore allOrePrefab in Ore.AllOrePrefabs)
		{
			if (allOrePrefab.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanProcess(Reagent reagentType)
	{
		foreach (Ore allOrePrefab in Ore.AllOrePrefabs)
		{
			if (allOrePrefab.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (base.ActivateButton != null)
		{
			base.ActivateButton.MaterialChanger.ChangeState((Mode == 1) ? Defines.Animator.ValidSmelt : Defines.Animator.InvalidSmelt);
		}
	}

	public Item CreateOutput(IQuantity orePrefab, int quantity)
	{
		Item item = (Item)orePrefab;
		Item item2 = Thing.Create<Item>(item, base.ExportConnection.Transform);
		IQuantity quantity2 = (IQuantity)item2;
		item2.name = item.name;
		item2.ParentSlot = null;
		quantity2.SetQuantity(quantity);
		return item2;
	}

	public Stackable CreateOutput(Stackable orePrefab, int quantity)
	{
		Stackable stackable = Thing.Create<Stackable>(orePrefab, base.ExportConnection.Transform);
		stackable.name = orePrefab.name;
		stackable.ParentSlot = null;
		stackable.SetQuantity(quantity);
		return stackable;
	}

	public Consumable CreateOutput(Consumable orePrefab, int quantity)
	{
		Consumable consumable = Thing.Create<Consumable>(orePrefab, base.ExportConnection.Transform);
		consumable.name = orePrefab.name;
		consumable.ParentSlot = null;
		consumable.Quantity = quantity;
		return consumable;
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Activate && value > 0.0)
		{
			Ignite();
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.RecipeHash)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.RecipeHash)
		{
			return (SmelterResult != null) ? SmelterResult.GetPrefabHash() : 0;
		}
		return base.GetLogicValue(logicType);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is FurnaceSaveData;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FurnaceSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
	}

	public virtual void Ignite()
	{
		if (!GameManager.RunSimulation)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(this, (ImportingThing is Ice) ? FurnaceSmeltHash : FurnaceSparkHash, Vector3.zero);
			return;
		}
		AtmosphericEventInstance.CreateAddEnergy(base.InternalAtmosphere, new MoleEnergy(5.0), spark: true);
		if (ImportingThing is Ice)
		{
			ImportingThing.Smelt(base.InternalAtmosphere, ReagentMixture);
			if (!GameManager.IsBatchMode)
			{
				Singleton<AudioManager>.Instance.PlayAudioClipsData(this, FurnaceSmeltHash, Vector3.zero);
			}
		}
		else if (!GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(this, FurnaceSparkHash, Vector3.zero);
		}
		this.OnFurnaceActivated?.Invoke();
	}

	public virtual IQuantity GetSmelterResult()
	{
		return null;
	}

	public virtual float GetSmelterScale()
	{
		return 1f;
	}

	public bool CreateIngots()
	{
		if (SmelterResult != null)
		{
			int num = (int)(ReagentMixture.TotalReagents / RatioMix.TotalReagents);
			float smelterScale = GetSmelterScale();
			if (smelterScale > 0f)
			{
				num = Mathf.RoundToInt((float)num * smelterScale);
			}
			int num2 = (int)Mathf.Clamp(num, 0f, SmelterResult.GetMaxQuantity);
			int num3 = num2;
			if (smelterScale > 0f)
			{
				num3 = Mathf.RoundToInt((float)num2 / smelterScale);
			}
			Item childThing = CreateOutput(SmelterResult, num2);
			ReagentMixture.Subtract(RatioMix * num3);
			if (CurrentRecipe.Energy > 0f)
			{
				AtmosphericEventInstance.CreateRemoveEnergy(base.InternalAtmosphere, new MoleEnergy(CurrentRecipe.Energy * (float)num3));
			}
			if (CurrentRecipe.RequiredMix.IsAnyToRemove)
			{
				AtmosphericEventInstance.CreateRemove(base.InternalAtmosphere, CurrentRecipe.RequiredMix * num2);
			}
			OnServer.MoveToSlot(childThing, ExportSlot);
			Achievements.AssessFurnaceApprentice(SmelterResult);
			Achievements.AssessFurnaceJourneyman(SmelterResult);
			Achievements.AssessFurnaceMaster(SmelterResult);
			return true;
		}
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			return false;
		}
		RatioMix = ReagentMixture.GetRatioMixture();
		ReagentMixture reagentMixture = new ReagentMixture(RatioMix) * Math.Min(ReagentMixture.TotalReagents, SlagPrefab.MaxQuantity);
		ReagentMixture.Subtract(reagentMixture);
		int quantity = (int)Math.Round(reagentMixture.TotalReagents);
		OnServer.MoveToSlot(Ore.CreateOreType(SlagPrefab, base.ExportConnection, RatioMix, quantity), ExportSlot);
		return true;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		HandleBrokenMix();
		if (!base.IsStructureCompleted)
		{
			return;
		}
		HandlePressureCheck();
		HandleGasInput();
		HandleGasOutput();
		if (this.OnInternalPressureHit != null)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(delegate
			{
				this.OnInternalPressureHit();
			});
		}
	}

	public virtual void HandleGasInput()
	{
	}

	public virtual void HandleGasOutput()
	{
	}

	public void HandleBrokenMix()
	{
		if (IsBroken)
		{
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			if (base.InternalAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.001) && atmosphere == null)
			{
				base.InternalAtmosphere.GasMixture.Reset();
				return;
			}
			Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			AtmosphereHelper.Mix(base.InternalAtmosphere, outputAtmos, AtmosphereHelper.MatterState.All);
		}
	}

	public bool Smelt(DynamicThing dynamicThing)
	{
		dynamicThing.Smelt(base.InternalAtmosphere, ReagentMixture);
		return true;
	}

	public override void OnDamageDestroyed()
	{
		base.OnDamageDestroyed();
		ReagentMixture.Clear();
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (ReagentMixture.TotalReagents > 0.0 && ReagentMixture.TotalReagents < 0.1)
		{
			ReagentMixture.Clear();
		}
		if (ExportingThing == null && IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		SmelterResult = GetSmelterResult();
		if (base.IsImportClosed)
		{
			if (ImportingThing != null)
			{
				if (base.InternalAtmosphere.Temperature >= ImportingThing.FlashPointTemperature && Smelt(ImportingThing))
				{
					return;
				}
				if (IsOpen && IsNextExportReady)
				{
					OnServer.MoveToSlot(ImportingThing, ExportSlot);
					return;
				}
			}
			else
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
		if (IsOpen && IsNextExportReady)
		{
			CreateIngots();
		}
	}

	protected override void OnServerExportTick(float deltaTime)
	{
		if (base.IsStructureCompleted)
		{
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
			if (base.IsImportClosed && (object)ImportingThing == null)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			if (Needle != null)
			{
				_needleTransform = Needle.transform;
				_needleBaseRotation = _needleTransform.localRotation;
			}
			if (FlameRenderer != null)
			{
				_flameMaterial = FlameRenderer.material;
			}
			ReagentMixture = new ReagentMixture(this);
			SetInflamedState();
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Open)
		{
			if (!IsOpen)
			{
				return ActionStrings.Open + " Mold";
			}
			return ActionStrings.Close + " Mold";
		}
		return base.GetContextualName(interactable);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (base.InternalAtmosphere == null)
		{
			return extendedText;
		}
		if (IsOverpressure())
		{
			extendedText.AppendLine(GameStrings.ThingOverPressure.AsString(ToTooltip()));
		}
		return extendedText;
	}

	private bool IsOverpressure()
	{
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressurekPa pressurekPa = PressurekPa.Zero;
		PressurekPa pressureGassesAndLiquids = base.InternalAtmosphere.PressureGassesAndLiquids;
		if (atmosphere != null)
		{
			pressurekPa = atmosphere.PressureGassesAndLiquids;
		}
		return RocketMath.Abs(pressurekPa - pressureGassesAndLiquids) > MAXPressureDelta;
	}

	protected virtual void InfoPanelText(PassiveTooltip tooltip)
	{
		if (IsOverpressure())
		{
			Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ThingOverPressure.AsString(ToTooltip()));
		}
		AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, Tooltip.ToolTipStringBuilder);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		PassiveTooltip passiveTooltip2 = passiveTooltip;
		if (base.InternalAtmosphere == null)
		{
			return passiveTooltip2;
		}
		if (hitCollider == InfoPanel)
		{
			InfoPanelText(passiveTooltip2);
			passiveTooltip2.Title = InterfaceStrings.FurnaceChamber;
			passiveTooltip2.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return passiveTooltip2;
		}
		if (hitCollider == WindowPanel)
		{
			if (ImportingThing != null)
			{
				if (IsOverpressure())
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ThingOverPressure.AsString(ToTooltip()));
				}
				Tooltip.SetProcessingText(ImportingThing);
				if ((ImportingThing.FlashPointTemperature > base.InternalAtmosphere.Temperature || base.InternalAtmosphere.PressureGassesAndLiquidsInPa <= 0f) && ImportingThing is Ice)
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.FurnaceMeltThing.AsString(ImportingThing.ToTooltip()));
				}
			}
			string value = ReagentMixture.ToString();
			Tooltip.ToolTipStringBuilder.Append(value);
			if (SmelterResult != null)
			{
				int num = (int)(ReagentMixture.TotalReagents / RatioMix.TotalReagents);
				float smelterScale = GetSmelterScale();
				if (smelterScale > 0f)
				{
					num = Mathf.RoundToInt((float)num * smelterScale);
				}
				int value2 = (int)Mathf.Clamp(num, 0f, SmelterResult.GetMaxQuantity);
				float num2 = Mathf.Floor((float)num / SmelterResult.GetMaxQuantity);
				float num3 = (float)num / SmelterResult.GetMaxQuantity - num2;
				if ((float)num > SmelterResult.GetMaxQuantity)
				{
					if (num2 > 1f)
					{
						Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ProduceQuantityOfResource.AsString(num2.ToStringRounded("yellow"), value2.ToStringPrefix("g", "yellow"), SmelterResult.ToTooltip()));
					}
					else
					{
						Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ProduceResource.AsString(value2.ToStringPrefix("g", "yellow"), SmelterResult.ToTooltip()));
					}
					if (num3 > 0f)
					{
						float value3 = num3 * SmelterResult.GetMaxQuantity;
						Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ProduceAnotherResource.AsString(value3.ToStringPrefix("g", "yellow"), SmelterResult.ToTooltip()));
					}
				}
				else
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.ProduceResource.AsString(value2.ToStringPrefix("g", "yellow"), SmelterResult.ToTooltip()));
				}
			}
			passiveTooltip2.Title = Localization.GetInterface("Contents");
			passiveTooltip2.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return passiveTooltip2;
		}
		passiveTooltip2.Title = DisplayName;
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnThreadUpdate()
	{
		if (base.InternalAtmosphere != null)
		{
			_pressureRating = RocketMath.Clamp(base.InternalAtmosphere.PressureGassesAndLiquids / MAXPressureDelta, PressurekPa.Zero, PressurekPa.One);
			if (_pressureRating.IsNaN())
			{
				_pressureRating = PressurekPa.Zero;
			}
			_needleRotation = PressureGaugeCurve.Evaluate(_pressureRating.ToFloat());
			_temperatureRatio = (base.InternalAtmosphere.Temperature / Chemistry.Temperature.Maximum).ToFloat();
			_pressureRatio = base.InternalAtmosphere.RatioOneAtmosphereUnclamped;
		}
	}

	public void HandlePressureCheck()
	{
		if (!base.HasOpenGrid)
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressurekPa pressurekPa = PressurekPa.Zero;
		PressurekPa pressureGassesAndLiquids = base.InternalAtmosphere.PressureGassesAndLiquids;
		if (atmosphere != null)
		{
			pressurekPa = atmosphere.PressureGassesAndLiquids;
		}
		PressurekPa pressurekPa2 = RocketMath.Abs(pressurekPa - pressureGassesAndLiquids);
		if (!GameManager.IsBatchMode)
		{
			IsStressedSound = pressurekPa2 > MAXPressureDelta * 0.6600000262260437;
		}
		int num = (int)Mathf.Clamp((int)pressurekPa2.ToFloat(), 2f, float.PositiveInfinity);
		if (!base.Indestructable && !IsBroken && (float)num > MAXPressureDelta.ToFloat() && (float)_random.Next(1, num) > MAXPressureDelta.ToFloat())
		{
			if (!HasBlown)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(Explode);
				StopAllAudio(immediate: true);
				HasBlown = true;
			}
			DamageState.Damage(ChangeDamageType.Increment, 200f, DamageUpdateType.Brute);
		}
	}

	public override void Explode()
	{
		base.Explode();
		if (GameManager.RunSimulation && base.InternalAtmosphere.IsAboveArmstrong())
		{
			global::Explosion.Explode(ExplosionForce * (base.InternalAtmosphere.PressureGassesAndLiquids / MAXPressureDelta).ToFloat(), radius: Mathf.Clamp(ExplosionRadius * base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / MAXPressureDelta.ToFloat(), 0f, MaxExplosionRadius), pos: base.Position, maxDamage: float.MaxValue, mineTerrain: true);
			AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, new GasMixture(base.InternalAtmosphere.GasMixture));
			AtmosphericEventInstance.Reset(base.InternalAtmosphere);
		}
	}

	private void SetInflamedState()
	{
		if (!(_flameMaterial == null))
		{
			_flameMaterial.EnableKeyword("_EMISSION");
			Inflamed = _inflamedState;
			if (!Inflamed)
			{
				_flameMaterial.color = Color.black;
				_flameMaterial.SetColor(Thing.EMISSION_COLOR, Color.black);
			}
		}
	}

	private async UniTaskVoid AssessRenderedFlame()
	{
		if (IsOccluded || IsCursor)
		{
			return;
		}
		await UniTask.NextFrame(PlayerLoopTiming.Update);
		if (GameManager.GameState != GameState.None && !base.BeingDestroyed)
		{
			_inflamedState = base.InternalAtmosphere != null && base.InternalAtmosphere.Temperature > inflamedTemperatureThreshold;
			if (_inflamedState != Inflamed)
			{
				SetInflamedState();
			}
		}
	}

	public override void OnAtmosphereClient()
	{
		if (!GameManager.IsBatchMode)
		{
			base.OnAtmosphereClient();
			AssessRenderedFlame().Forget();
			SmelterResult = GetSmelterResult();
			IsStressedSound = base.InternalAtmosphere.PressureGassesAndLiquids > MAXPressureDelta * 0.6600000262260437;
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		bool playSounds = Inflamed && Vector3.SqrMagnitude(base.Position - InventoryManager.ParentPosition) < 400f;
		PlaySounds = playSounds;
		if (PlaySounds)
		{
			_audioIntensityRatio = Mathf.Clamp01(_intensity / AudioIntensityMax);
			GetAudioEvent(InflamedLowHash)?.SetVolumeAndPitch(InflamedLowAudioCurve.Evaluate(_audioIntensityRatio), Mathf.Clamp(_audioIntensityRatio * 2f, 1f, 2f));
			GetAudioEvent(InflamedHighHash)?.SetVolumeAndPitch(InflamedHighAudioCurve.Evaluate(_audioIntensityRatio), Mathf.Clamp(_audioIntensityRatio, 0.5f, 1f));
		}
	}

	public override void UpdateEachFrame()
	{
		if (GameManager.IsBatchMode || WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (IsOccluded || IsCursor || !base.IsStructureCompleted)
		{
			return;
		}
		if (GameManager.RunSimulation)
		{
			if (Mode != ((SmelterResult != null) ? 1 : 0))
			{
				OnServer.Interact(base.InteractMode, (SmelterResult != null) ? 1 : 0);
			}
			AssessRenderedFlame().Forget();
		}
		if (Inflamed)
		{
			_intensity = Mathf.Lerp(_intensity, _temperatureRatio * _pressureRatio, 0.2f * Time.deltaTime);
			_cleanBurnRate = Mathf.Lerp(_cleanBurnRate, base.InternalAtmosphere.CleanBurnRate, 0.2f * Time.deltaTime);
			Color color = AtmosphericsManager.Instance.TemperatureGradient.Evaluate(_cleanBurnRate);
			_flameMaterial.color = Color.white;
			_flameMaterial.SetColor(Thing.EMISSION_COLOR, color * (_intensity * 5f));
		}
		if (_needleTransform != null)
		{
			_lastAngleNeedle = Mathf.Lerp(_lastAngleNeedle, _needleRotation, Time.deltaTime * LerpSpeed);
			_needleTransform.localRotation = _needleBaseRotation;
			_needleTransform.Rotate(_lastAngleNeedle, 0f, 0f, Space.Self);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Open)
		{
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			}
			this.OnFurnaceOpenedEvent?.Invoke(interactable.State);
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			Ignite();
			if (GameManager.RunSimulation && Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			PlaySound(ActivateButtonHash);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 1)
		{
			ResetActivate().Forget();
		}
	}

	private async UniTaskVoid ResetActivate()
	{
		await UniTask.Delay(100);
		OnServer.Interact(base.InteractActivate, 0);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (!GameManager.RunSimulation)
		{
			ImportingThing = newChild;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (!GameManager.RunSimulation && ImportingThing == previousChild)
		{
			ImportingThing = null;
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			if (GameManager.GameState != GameState.None && !IsCursor)
			{
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture, base.InternalAtmosphere.Inflamed);
				AtmosphericEventInstance.Reset(base.InternalAtmosphere);
			}
			base.OnDestroy();
		}
	}
}
