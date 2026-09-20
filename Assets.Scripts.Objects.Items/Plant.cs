using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Genetics;
using Objects.Electrical;
using Objects.Items;
using Reagents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Plant : Stackable, INutrition, IProcessable, IIngredient, ISpatial, IPhysical, IProfile, IDensePoolable, ICompostable, IFermentable
{
	public delegate void OnGrowthStateChange(Plant plant);

	public static readonly List<Plant> AllPlants = new List<Plant>();

	public static readonly List<Plant> AllPlantPrefabs = new List<Plant>();

	public static readonly List<IAnimalFood> AllEdibles = new List<IAnimalFood>();

	private static readonly float _entityMolePerBreath = 0.0012f;

	public static bool CustomPlantGrowthSpeed = false;

	[Header("Plant")]
	[FormerlySerializedAs("LifeData")]
	[SerializeField]
	public PlantLifeRequirements lifeRequirements = new PlantLifeRequirements();

	public string LifeRequirementsId;

	public float MoodBonusValue;

	public Thing FruitObject;

	public int HarvestQuantityMax;

	public float NutritionValue;

	private ushort _growStatusFlags;

	private string _planterName;

	public PlantStatus PlantStatus = new PlantStatus();

	public PlantRecord PlantRecord = new PlantRecord();

	[ReadOnly]
	public int MatureIndex = -1;

	[ReadOnly]
	public int SeedingIndex = -1;

	private int _harvestQuantity;

	private int _seedQuantity = 1;

	private float _fertilizerBoost = 1f;

	public Seed SeedObject;

	public bool IsFertilized;

	public List<PlantStage> GrowthStates = new List<PlantStage>();

	public IGrower ParentTray;

	[Tooltip("How much energy in Joules is the plant able to Add/remove from the atmosphere")]
	public float ThermalPlantEnergy;

	[Tooltip("Rather than drinking water this plant adds to the water supply")]
	public bool AddsToWater;

	[SerializeField]
	[ReadOnly]
	private float _stageTime;

	[SerializeField]
	[ReadOnly]
	private int _stage;

	[Tooltip("Set to true if plant can be harvested multiple times")]
	[SerializeField]
	private bool _isPerennial;

	private ushort _growthEfficiencyPercent;

	private bool _refreshingState;

	private static readonly int _potatoPrefabHash = Animator.StringToHash("ItemPotato");

	private System.Random EfficiencyRandom;

	private float _growthEfficiencyRng = 1f;

	private float _growthEfficiencyRngRange = 0.02f;

	private static float VolatileRatio = 2f;

	private static float OxygenRatio = 1f;

	private static float DamageScale = 0.25f;

	[SerializeField]
	private SpawnGas[] _plantSpawnGasList = Array.Empty<SpawnGas>();

	public float MoodBonus => MoodBonusValue;

	public float WaterMoles => 0f;

	public ushort GrowStatusFlags
	{
		get
		{
			return _growStatusFlags;
		}
		set
		{
			_growStatusFlags = value;
			if (NetworkManager.IsServer && _growStatusFlags != 0)
			{
				base.NetworkUpdateFlags |= 32768;
			}
		}
	}

	public string PlanterName
	{
		get
		{
			if (GameManager.RunSimulation)
			{
				List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
				if (stackedGeneCollections != null && stackedGeneCollections.Count > 0)
				{
					List<GeneCollection> stackedGeneCollections2 = StackedGeneCollections;
					return stackedGeneCollections2[stackedGeneCollections2.Count - 1].PlanterCustomName;
				}
			}
			_ = PrefabHash;
			return _planterName;
		}
		set
		{
			if (GameManager.RunSimulation && StackedGeneCollections != null && StackedGeneCollections.Count > 0)
			{
				List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
				stackedGeneCollections[stackedGeneCollections.Count - 1].PlanterCustomName = value;
			}
			_planterName = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32;
			}
		}
	}

	public override string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(PlanterName))
			{
				return base.DisplayName;
			}
			return base.DisplayName + "_" + PlanterName;
		}
	}

	public override string CustomName
	{
		get
		{
			if (GameManager.RunSimulation)
			{
				List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
				if (stackedGeneCollections != null && stackedGeneCollections.Count > 0)
				{
					List<GeneCollection> stackedGeneCollections2 = StackedGeneCollections;
					return stackedGeneCollections2[stackedGeneCollections2.Count - 1].PlantCustomName;
				}
			}
			return base.CustomName;
		}
		set
		{
			base.CustomName = value;
			if (GameManager.RunSimulation && StackedGeneCollections != null && StackedGeneCollections.Count > 0)
			{
				List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
				stackedGeneCollections[stackedGeneCollections.Count - 1].PlantCustomName = value;
			}
		}
	}

	public bool IsDead => CurrentStage?.Dead ?? false;

	[ByteArraySync]
	public int HarvestQuantity
	{
		get
		{
			return _harvestQuantity;
		}
		set
		{
			_harvestQuantity = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	[ByteArraySync]
	public int SeedQuantity
	{
		get
		{
			return _seedQuantity;
		}
		private set
		{
			_seedQuantity = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	[ByteArraySync]
	private float FertilizerBoost
	{
		get
		{
			return _fertilizerBoost;
		}
		set
		{
			_fertilizerBoost = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public float FertilizerHarvestQuantityBoost { get; private set; }

	private AnimationCurve TemperatureEfficiencyCurve => lifeRequirements.TemperatureCurve;

	private AnimationCurve PressureEfficiencyCurve => lifeRequirements.PressureCurve;

	public float BiomassValue => 1f;

	private bool IsEndothermicPlant => ThermalPlantEnergy < 0f;

	public ushort GrowthEfficiencyPercent
	{
		get
		{
			return _growthEfficiencyPercent;
		}
		private set
		{
			if (value != GrowthEfficiencyPercent)
			{
				if (NetworkManager.IsServer)
				{
					GrowStatusFlags |= 1;
				}
				_growthEfficiencyPercent = value;
			}
		}
	}

	public int Stage
	{
		get
		{
			return _stage;
		}
		private set
		{
			if (GrowthStates != null && GrowthStates.Count != 0)
			{
				if (value < 0 || value >= GrowthStates.Count)
				{
					ConsoleWindow.PrintError($"Error! Tried to set {DisplayName} growth stage to an out of range value: {value}.");
					value = Mathf.Clamp(value, 0, GrowthStates.Count - 1);
				}
				_stage = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
				if (!_refreshingState)
				{
					StageChanged().Forget();
				}
			}
		}
	}

	public bool IsPlanted => ParentTray != null;

	public float CurrentLightExposure => ParentTray?.CurrentLightExposure ?? 0f;

	public float GrowthEfficiencyRNG => _growthEfficiencyRng;

	private PlantStage CurrentStage
	{
		get
		{
			if (GrowthStates == null || GrowthStates.Count == 0)
			{
				return null;
			}
			return GrowthStates[Mathf.Clamp(Stage, 0, GrowthStates.Count - 1)];
		}
	}

	public bool IsMature => CurrentStage?.Mature ?? false;

	public bool IsSeeding
	{
		get
		{
			if (CurrentStage != null)
			{
				return CurrentStage.Seed;
			}
			return false;
		}
	}

	public Atmosphere BreathingAtmosphere
	{
		get
		{
			IGrower parentTray = ParentTray;
			if (parentTray == null || parentTray.BreathingAtmosphere == null)
			{
				return base.WorldAtmosphere;
			}
			return ParentTray.BreathingAtmosphere;
		}
	}

	private MoleQuantity MolesDrunkLastTick { get; set; }

	public GeneCollection Genes
	{
		get
		{
			if (StackedGeneCollections.Count == 0)
			{
				StackedGeneCollections.Add(new GeneCollection());
			}
			List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
			return stackedGeneCollections[stackedGeneCollections.Count - 1];
		}
		private set
		{
			if (StackedGeneCollections.Count == 0)
			{
				StackedGeneCollections.Add(new GeneCollection());
			}
			List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
			stackedGeneCollections[stackedGeneCollections.Count - 1] = value;
		}
	}

	public CompostType CompostType => CompostType.HarvestQuantity;

	public float ProcessTime => 2.5f;

	public SpawnGas[] SpawnGasList => _plantSpawnGasList;

	public double MaturityRatio { get; private set; }

	public double SeedingRatio { get; private set; }

	public static event OnGrowthStateChange OnGrowthState;

	public bool Equals(Recipe recipe)
	{
		return CreatedReagentMixture.Equals(recipe);
	}

	public float GetNutritionalValue()
	{
		return NutritionValue * (float)base.Quantity;
	}

	public FoodQuality GetFoodQuality()
	{
		return FoodQuality.Raw;
	}

	private static void GrowthStateChanged(Plant plant)
	{
		Plant.OnGrowthState?.Invoke(plant);
	}

	public override void OnPrefabLoad()
	{
		for (int i = 0; i < GrowthStates.Count; i++)
		{
			PlantStage plantStage = GrowthStates[i];
			if (plantStage.Mature)
			{
				MatureIndex = i;
			}
			if (plantStage.Seed)
			{
				SeedingIndex = i;
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		PlantStatus = new PlantStatus(this);
		lifeRequirements.SetOwnerPlant(this);
	}

	private void InheritTraits(Plant parentPlant)
	{
		GeneCollection genes = parentPlant.Genes.CreateSimilar(parentPlant);
		Genes = genes;
		lifeRequirements.UpdateTemperaturePressureCurves();
	}

	public void ApplySeedTraits(GeneCollection genes)
	{
		PlantRecord = new PlantRecord(this);
		Genes = genes;
		lifeRequirements.UpdateTemperaturePressureCurves();
		PlanterName = ParentTray?.CustomName;
	}

	public void SetIsPerennial(bool value)
	{
		_isPerennial = value;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteByte((byte)Stage);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			ushort growStatusFlags = GrowStatusFlags;
			GrowStatusFlags = 0;
			writer.WriteUInt16(growStatusFlags);
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 1))
			{
				writer.WriteUInt16(GrowthEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 2))
			{
				writer.WriteByte(PlantStatus.BreathingEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 4))
			{
				writer.WriteByte(PlantStatus.TemperatureEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 8))
			{
				writer.WriteByte(PlantStatus.LightEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 16))
			{
				writer.WriteByte(PlantStatus.PressureEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 32))
			{
				writer.WriteByte(PlantStatus.HydrationEfficiencyPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 64))
			{
				writer.WriteByte(PlantStatus.CurrentLightExposurePercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 128))
			{
				writer.WriteUInt16(PlantStatus.LightStressPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 256))
			{
				writer.WriteByte(PlantStatus.LightPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 512))
			{
				writer.WriteByte(PlantStatus.DarknessPercent);
			}
			if (Thing.IsNetworkUpdateRequired(growStatusFlags, 1024))
			{
				writer.WriteUInt16(PlantStatus.PackedStates);
			}
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte((byte)HarvestQuantity);
			writer.WriteByte((byte)SeedQuantity);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteSingle(FertilizerBoost);
		}
		if (Thing.IsNetworkUpdateRequired(32u, networkUpdateType))
		{
			writer.WriteString(PlanterName);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Genes.Write(writer);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			Stage = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			ushort toCheck = reader.ReadUInt16();
			if (Thing.IsNetworkUpdateRequired(toCheck, 1))
			{
				GrowthEfficiencyPercent = reader.ReadUInt16();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 2))
			{
				PlantStatus.BreathingEfficiencyPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 4))
			{
				PlantStatus.TemperatureEfficiencyPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 8))
			{
				PlantStatus.LightEfficiencyPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 16))
			{
				PlantStatus.PressureEfficiencyPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 32))
			{
				PlantStatus.HydrationEfficiencyPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 64))
			{
				PlantStatus.CurrentLightExposurePercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 128))
			{
				PlantStatus.LightStressPercent = reader.ReadUInt16();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 256))
			{
				PlantStatus.LightPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 512))
			{
				PlantStatus.DarknessPercent = reader.ReadByte();
			}
			if (Thing.IsNetworkUpdateRequired(toCheck, 1024))
			{
				PlantStatus.UnpackCurrentStates(reader.ReadUInt16());
			}
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			HarvestQuantity = reader.ReadByte();
			SeedQuantity = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			FertilizerBoost = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(32u, networkUpdateType))
		{
			PlanterName = reader.ReadString();
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Genes.Read(reader);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16(GrowthEfficiencyPercent);
		writer.WriteByte(PlantStatus.BreathingEfficiencyPercent);
		writer.WriteByte(PlantStatus.TemperatureEfficiencyPercent);
		writer.WriteByte(PlantStatus.LightEfficiencyPercent);
		writer.WriteByte(PlantStatus.PressureEfficiencyPercent);
		writer.WriteByte(PlantStatus.HydrationEfficiencyPercent);
		writer.WriteByte(PlantStatus.CurrentLightExposurePercent);
		writer.WriteUInt16(PlantStatus.LightStressPercent);
		writer.WriteByte(PlantStatus.LightPercent);
		writer.WriteByte(PlantStatus.DarknessPercent);
		writer.WriteUInt16(PlantStatus.PackedStates);
		writer.WriteByte((byte)Stage);
		writer.WriteByte((byte)HarvestQuantity);
		writer.WriteByte((byte)SeedQuantity);
		writer.WriteSingle(FertilizerBoost);
		writer.WriteString(PlanterName);
		Genes.Write(writer);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		GrowthEfficiencyPercent = reader.ReadUInt16();
		PlantStatus.BreathingEfficiencyPercent = reader.ReadByte();
		PlantStatus.TemperatureEfficiencyPercent = reader.ReadByte();
		PlantStatus.LightEfficiencyPercent = reader.ReadByte();
		PlantStatus.PressureEfficiencyPercent = reader.ReadByte();
		PlantStatus.HydrationEfficiencyPercent = reader.ReadByte();
		PlantStatus.CurrentLightExposurePercent = reader.ReadByte();
		PlantStatus.LightStressPercent = reader.ReadUInt16();
		PlantStatus.LightPercent = reader.ReadByte();
		PlantStatus.DarknessPercent = reader.ReadByte();
		PlantStatus.UnpackCurrentStates(reader.ReadUInt16());
		Stage = reader.ReadByte();
		HarvestQuantity = reader.ReadByte();
		SeedQuantity = reader.ReadByte();
		FertilizerBoost = reader.ReadSingle();
		PlanterName = reader.ReadString();
		Genes.Read(reader);
	}

	public void SetStateMessage(DelayedActionInstance result)
	{
		string text = ToTooltip();
		result.AppendStateMessage(GameStrings.PlantHoldToClear, KeyMap._QuantityModifier.Key.ToString(), text);
		if (IsDead)
		{
			result.AppendStateMessage(GameStrings.PlantDead, text);
			return;
		}
		if (!IsMature && !IsSeeding)
		{
			ushort growthEfficiencyPercent = GrowthEfficiencyPercent;
			Assets.Scripts.Localization2.GameString gameString = ((growthEfficiencyPercent < 50) ? ((growthEfficiencyPercent <= 1) ? GameStrings.PlantNotGrowingMature : ((growthEfficiencyPercent >= 25) ? GameStrings.PlantGrowingPoorlyMature : GameStrings.PlantBarelyGrowingMature)) : ((growthEfficiencyPercent >= 80) ? GameStrings.PlantThrivingMature : GameStrings.PlantGrowingModeratelyMature));
			result.AppendStateMessage(gameString, text);
		}
		else if (!IsSeeding)
		{
			ushort growthEfficiencyPercent = GrowthEfficiencyPercent;
			Assets.Scripts.Localization2.GameString gameString = ((growthEfficiencyPercent < 50) ? ((growthEfficiencyPercent <= 1) ? GameStrings.PlantNotGrowingSeed : ((growthEfficiencyPercent >= 25) ? GameStrings.PlantGrowingPoorlySeed : GameStrings.PlantBarelyGrowingSeed)) : ((growthEfficiencyPercent >= 80) ? GameStrings.PlantThrivingSeed : GameStrings.PlantGrowingModeratelySeed));
			result.AppendStateMessage(gameString, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.HighTemperature))
		{
			result.AppendStateMessage(GameStrings.PlantAtmosphereTooHot, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.LowTemperature))
		{
			result.AppendStateMessage(GameStrings.PlantAtmosphereTooCold, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.HighWaterTemperature))
		{
			result.AppendStateMessage(GameStrings.PlantWaterTooHot, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.LowWaterTemperature))
		{
			result.AppendStateMessage(GameStrings.PlantWaterTooCold, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.Dehydrated))
		{
			result.AppendStateMessage(GameStrings.PlantWaterNeeds, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.Suffocated))
		{
			result.AppendStateMessage(GameStrings.PlantNotCorrectAtmosphere, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.UnDesiredGas))
		{
			result.AppendStateMessage(GameStrings.PlantIsPollutedByAir, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.Darkness))
		{
			result.AppendStateMessage(GameStrings.PlantIsInDarkness, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.LowPressure))
		{
			result.AppendStateMessage(GameStrings.PlantIsInLowPressureEnvironment, text);
		}
		if (PlantStatus.GetCurrentState(PlantStatusType.HighPressure))
		{
			result.AppendStateMessage(GameStrings.PlantIsInHighPressureEnvironment, text);
		}
		if (IsSeeding && SeedQuantity > 0)
		{
			result.AppendStateMessage(GameStrings.PlantCreateSeeds, StringManager.Get(SeedQuantity), text);
		}
		else if (IsMature && HarvestQuantity > 0)
		{
			result.AppendStateMessage(GameStrings.PlantCreate, StringManager.Get(HarvestQuantity), text);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Plants);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.Plants;
	}

	private async UniTaskVoid StageChanged()
	{
		_refreshingState = true;
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread(cancelToken);
		}
		while (!GameManager.IsRunning)
		{
			await UniTask.NextFrame(cancelToken);
		}
		await UniTask.NextFrame(cancelToken);
		if (!cancelToken.IsCancellationRequested && !base.BeingDestroyed)
		{
			RefreshVisualizers();
			if (PrefabHash == _potatoPrefabHash)
			{
				Achievements.AssessMarkWatney(this);
				Achievements.AssessStillMarkWatney(this);
			}
			_refreshingState = false;
		}
	}

	public override void InitializeDamageState()
	{
		DamageState = new OrganicDamageState(this);
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		RefreshVisualizers();
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		RefreshVisualizers();
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		StackedGeneCollections = new List<GeneCollection>(MaxQuantity);
		EfficiencyRandom = new System.Random((int)base.ReferenceId);
		float num = 1f - _growthEfficiencyRngRange;
		float num2 = 1f + _growthEfficiencyRngRange;
		_growthEfficiencyRng = (float)(EfficiencyRandom.NextDouble() * (double)(num2 - num) + (double)num);
		lifeRequirements.UpdateTemperaturePressureCurves();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PlantSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		if (savedData is PlantSaveData plantSaveData)
		{
			Stage = plantSaveData.Stage;
			_stageTime = plantSaveData.StageTime;
			PlantRecord = ((plantSaveData.PlantRecord != null) ? new PlantRecord(plantSaveData.PlantRecord) : new PlantRecord(this));
			if (plantSaveData.AggregateStates != null)
			{
				PlantStatus.ApplySerializedAggregateStates(plantSaveData.AggregateStates);
			}
			if (plantSaveData.CurrentStates != null)
			{
				PlantStatus.ApplySerializedCurrentStates(plantSaveData.CurrentStates);
			}
			if (plantSaveData.HarvestQuantity >= 0)
			{
				HarvestQuantity = plantSaveData.HarvestQuantity;
			}
			if (plantSaveData.SeedQuantity >= 0)
			{
				SeedQuantity = plantSaveData.SeedQuantity;
			}
			FertilizerBoost = plantSaveData.FertilizerBoost;
			IsFertilized = plantSaveData.IsFertilized;
			if (plantSaveData.StackedGeneCollectionWrappers.Count != plantSaveData.Quantity)
			{
				for (int i = 0; i < plantSaveData.Quantity; i++)
				{
					StackedGeneCollections.Add(new GeneCollection());
				}
			}
			else
			{
				StackedGeneCollections = new List<GeneCollection>(plantSaveData.StackedGeneCollectionWrappers.Count);
				foreach (GeneCollectionWrapper stackedGeneCollectionWrapper in plantSaveData.StackedGeneCollectionWrappers)
				{
					GeneCollection geneCollection = new GeneCollection();
					geneCollection.PlantCustomName = stackedGeneCollectionWrapper.PlantCustomName;
					geneCollection.PlanterCustomName = stackedGeneCollectionWrapper.PlanterCustomName;
					foreach (GeneWrapper geneWrapper in stackedGeneCollectionWrapper.GeneWrappers)
					{
						geneCollection.Lookup[geneWrapper.Gene] = geneWrapper;
					}
					StackedGeneCollections.Add(geneCollection);
				}
			}
			PlanterName = plantSaveData.PlanterName;
		}
		base.DeserializeSave(savedData);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is PlantSaveData plantSaveData))
		{
			return;
		}
		if (PlantRecord != null)
		{
			plantSaveData.PlantRecord = new PlantRecord(PlantRecord);
		}
		PlantStatus.SerializeAggregateStates(plantSaveData.AggregateStates);
		PlantStatus.SerializeCurrentStates(plantSaveData.CurrentStates);
		plantSaveData.Stage = Stage;
		plantSaveData.StageTime = _stageTime;
		plantSaveData.HarvestQuantity = HarvestQuantity;
		plantSaveData.SeedQuantity = SeedQuantity;
		plantSaveData.FertilizerBoost = FertilizerBoost;
		plantSaveData.IsFertilized = IsFertilized;
		plantSaveData.PlanterName = PlanterName;
		plantSaveData.StackedGeneCollectionWrappers = new List<GeneCollectionWrapper>(StackedGeneCollections.Count);
		for (int i = 0; i < StackedGeneCollections.Count; i++)
		{
			GeneCollection geneCollection = StackedGeneCollections[i];
			GeneCollectionWrapper geneCollectionWrapper = new GeneCollectionWrapper
			{
				PlantCustomName = geneCollection.PlantCustomName,
				PlanterCustomName = geneCollection.PlanterCustomName
			};
			foreach (GeneWrapper value in geneCollection.Lookup.Values)
			{
				geneCollectionWrapper.GeneWrappers.Add(new GeneWrapper(value));
			}
			plantSaveData.StackedGeneCollectionWrappers.Add(geneCollectionWrapper);
		}
	}

	public void Planted(IGrower tray)
	{
		AtmosphericsManager.Instance.Register(this);
		AllPlants.Add(this);
		RefreshVisualizers();
		ParentTray = tray;
		ThingTransform.Rotate(Vector3.up, UnityEngine.Random.Range(0, 359));
	}

	public void ApplyFertilizer(Fertiliser fertilizer)
	{
		FertilizerHarvestQuantityBoost = (int)Math.Round(fertilizer.HarvestBoost);
		FertilizerBoost = fertilizer.GrowthSpeed;
		IsFertilized = true;
	}

	public override bool CanItemDecay()
	{
		if (base.CanItemDecay() && (IsMature || Stage == 0))
		{
			return !IsPlanted;
		}
		return false;
	}

	public virtual void Harvest(Thing harvester, Slot sourceSlot, bool seed)
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		if (!GameManager.RunSimulation)
		{
			if (!GameManager.IsBatchMode && !seed)
			{
				Achievements.Increment(Achievements.Stat.TotalPlantsHarvested, 1);
			}
		}
		else if (!(harvester == null) && sourceSlot != null)
		{
			if (seed)
			{
				HarvestSeed(sourceSlot);
				SeedQuantity--;
			}
			else
			{
				HarvestPlant(sourceSlot);
				HarvestQuantity--;
				Achievements.Increment(Achievements.Stat.TotalPlantsHarvested, 1);
			}
			CheckRemainingHarvest();
		}
	}

	protected virtual void HarvestPlant(Slot sourceSlot)
	{
		if (FruitObject is Plant)
		{
			Plant plant = sourceSlot.Occupant as Plant;
			Plant plant2 = OnServer.Create<Plant>(FruitObject, sourceSlot);
			plant2.InheritTraits(this);
			if ((bool)plant)
			{
				plant.Merge(plant2);
			}
		}
		else if (FruitObject is Stackable)
		{
			Stackable stackable = sourceSlot.Occupant as Stackable;
			Stackable mergeable = OnServer.Create<Stackable>(FruitObject, sourceSlot);
			if ((bool)stackable)
			{
				stackable.Merge(mergeable);
			}
		}
	}

	private void HarvestSeed(Slot sourceSlot)
	{
		Seed seed = sourceSlot.Occupant as Seed;
		Seed seed2 = OnServer.Create<Seed>(SeedObject, sourceSlot);
		seed2.InheritTraits(this);
		if ((bool)seed)
		{
			seed.Merge(seed2);
		}
	}

	private void CheckRemainingHarvest()
	{
		if (HarvestQuantity <= 0 && (!IsSeeding || SeedQuantity <= 0))
		{
			if (_isPerennial)
			{
				ResetPerennialStage();
			}
			else
			{
				OnServer.Destroy(this);
			}
		}
	}

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		RefreshVisualizersFromThread().Forget();
	}

	private async UniTaskVoid RefreshVisualizersFromThread()
	{
		await UniTask.SwitchToMainThread();
		RefreshVisualizers();
	}

	private void RefreshVisualizers()
	{
		if (!IsPlanted || base.BeingDestroyed)
		{
			return;
		}
		GrowthStates[0].Visualizer.enabled = Stage == 0 && !IsOccluded;
		for (int i = 1; i < GrowthStates.Count; i++)
		{
			if (!(GrowthStates[i].Visualizer == null) && i != Stage)
			{
				GrowthStates[i].Visualizer.gameObject.SetActive(value: false);
			}
		}
		if (GrowthStates[Stage].Visualizer != null && Stage > 0)
		{
			GrowthStates[Stage].Visualizer.gameObject.SetActive(!IsOccluded);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		RefreshVisualizers();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AllPlants.Remove(this);
		if ((bool)AtmosphericsManager.Instance)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void OnDamageDestroyed()
	{
		if (base.Quantity > 1)
		{
			base.OnDamageDestroyed();
		}
		if (GameManager.RunSimulation && !IsDead)
		{
			if (IsMature && !IsDecayed)
			{
				Stage = GrowthStates.Count - 1;
			}
			else
			{
				OnServer.Destroy(this);
			}
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (attack.SourceItem is Labeller labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		if (attack.SourceItem is PlantSampler plantSampler)
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = GameStrings.PlantSamplerAction.AsString(ToTooltip())
			};
			if (!plantSampler.OnOff)
			{
				return delayedActionInstance2.Fail(GameStrings.DeviceNotOn);
			}
			if (!plantSampler.Powered)
			{
				return delayedActionInstance2.Fail(GameStrings.DeviceNoPower);
			}
			if (doAction)
			{
				plantSampler.AddPlantSample(this);
			}
			return delayedActionInstance2.Succeed();
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		for (int i = 0; i < StackedGeneCollections.Count - 1; i++)
		{
			StackedGeneCollections[i].PlantCustomName = CustomName;
		}
	}

	private void TakePlantBreath()
	{
		if (IsDead)
		{
			return;
		}
		ParentTray = base.ParentSlot?.Parent as IGrower;
		if (BreathingAtmosphere == null || BreathingAtmosphere.BeingDestroyed)
		{
			PlantStatus.TemperatureEfficiency = 0f;
			PlantStatus.BreathingEfficiency = 0f;
			return;
		}
		PlantStatus.TemperatureEfficiency = TemperatureEfficiencyCurve.Evaluate(RocketMath.KelvinToCelsius(BreathingAtmosphere.Temperature));
		PlantStatus.PressureEfficiency = PressureEfficiencyCurve.Evaluate(BreathingAtmosphere.PressureGasses.ToFloat());
		PlantStatus.SetCurrentState(PlantStatusType.LowTemperature, BreathingAtmosphere.Temperature < RocketMath.CelsiusToKelvin(lifeRequirements.GrowTemperatureC.Min()));
		PlantStatus.SetCurrentState(PlantStatusType.HighTemperature, BreathingAtmosphere.Temperature > RocketMath.CelsiusToKelvin(lifeRequirements.GrowTemperatureC.Max()));
		PlantStatus.SetCurrentState(PlantStatusType.LowPressure, BreathingAtmosphere.PressureGasses.ToFloat() < lifeRequirements.GrowPressure.Min());
		PlantStatus.SetCurrentState(PlantStatusType.HighPressure, BreathingAtmosphere.PressureGasses.ToFloat() > lifeRequirements.GrowPressure.Max());
		GasMixture gasMixture = GasMixtureHelper.Create();
		double num = 0.0;
		foreach (GasQuantityRatioData inhaledGase in lifeRequirements.Data.InhaledGases)
		{
			num += (double)inhaledGase.Quantity;
			float gasTypeRatio = BreathingAtmosphere.GasMixture.GetGasTypeRatio(inhaledGase.Type);
			MoleQuantity moleQuantity = new MoleQuantity(inhaledGase.Quantity * RocketMath.MapToScaleClamp(0.001f, inhaledGase.Ratio, 0f, 1f, gasTypeRatio) * (float)lifeRequirements.GasProduction);
			MoleEnergy energy = IdealGas.Energy(BreathingAtmosphere.Temperature, Mole.SpecificHeat(inhaledGase.Type), moleQuantity) * (float)lifeRequirements.GasProduction;
			gasMixture.Add(new GasMixture(new Mole(inhaledGase.Type, moleQuantity, energy)));
		}
		float num2 = (BreathingAtmosphere.Remove(gasMixture, AtmosphereHelper.MatterState.All).GetTotalMolesGassesAndLiquids / num).ToFloat();
		if (!IsEndothermicPlant)
		{
			DoPlantBreathOut(num2);
		}
		else
		{
			DoPlantBreathOutEndothermic(num2);
		}
		PlantStatus.BreathingEfficiency = num2;
	}

	private MoleQuantity TakePlantDrink()
	{
		if (IsDead)
		{
			return MoleQuantity.Zero;
		}
		if (ParentTray?.WaterAtmosphere == null || ParentTray.WaterAtmosphere.TotalMolesLiquids < MoleQuantity.One)
		{
			return MoleQuantity.Zero;
		}
		MoleQuantity waterPerTick = lifeRequirements.WaterPerTick;
		if (AddsToWater)
		{
			MoleEnergy energy = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, Mole.SpecificHeat(Chemistry.GasType.Water), waterPerTick);
			ParentTray.WaterAtmosphere.Add(new GasMixture(new Mole(Chemistry.GasType.Water, waterPerTick, energy)));
			return waterPerTick;
		}
		waterPerTick = ParentTray.WaterAtmosphere.Remove(waterPerTick, Chemistry.GasType.Water).GetTotalMolesLiquids;
		PlantStatus.SetCurrentState(PlantStatusType.LowWaterTemperature, ParentTray.WaterAtmosphere.Temperature < RocketMath.CelsiusToKelvin(lifeRequirements.GrowTemperatureC.Min()));
		PlantStatus.SetCurrentState(PlantStatusType.HighWaterTemperature, ParentTray.WaterAtmosphere.Temperature > RocketMath.CelsiusToKelvin(lifeRequirements.GrowTemperatureC.Max()));
		return waterPerTick;
	}

	private void DoPlantBreathOut(float breathedRatio)
	{
		GasMixture gasMixture = GasMixtureHelper.Create();
		foreach (GasQuantityData exhaledGase in lifeRequirements.Data.ExhaledGases)
		{
			gasMixture.Add(new Mole(quantity: new MoleQuantity(exhaledGase.Quantity * breathedRatio * (float)lifeRequirements.GasProduction), gasType: exhaledGase.Type, energy: MoleEnergy.Zero));
		}
		gasMixture.AddEnergy(IdealGas.Energy(gasMixture.HeatCapacity, BreathingAtmosphere.Temperature));
		BreathingAtmosphere.Add(gasMixture);
	}

	private void DoPlantBreathOutEndothermic(float minBreathedRatio)
	{
		MoleQuantity zero = MoleQuantity.Zero;
		GasMixture gasMixture = BreathingAtmosphere.GasMixture;
		MoleEnergy moleEnergy = new MoleEnergy((double)((gasMixture.Methane.SpecificHeat().ToFloat() * VolatileRatio + gasMixture.Oxygen.SpecificHeat().ToFloat() * OxygenRatio) * BreathingAtmosphere.Temperature.ToFloat()) + gasMixture.Methane.Enthalpy() * (double)VolatileRatio);
		if (!CurrentStage.Mature || ParentTray?.WaterAtmosphere == null || !ParentTray.WaterAtmosphere.GasMixture.IsValid)
		{
			foreach (GasQuantityData exhaledGase in lifeRequirements.Data.ExhaledGases)
			{
				zero += new MoleQuantity(exhaledGase.Quantity * minBreathedRatio * (float)lifeRequirements.GasProduction);
			}
			return;
		}
		Mole water = ParentTray.WaterAtmosphere.GasMixture.Water;
		MoleEnergy moleEnergy2 = IdealGas.Energy(water.Temperature, water.SpecificHeat(), MoleQuantity.One);
		MoleEnergy moleEnergy3 = moleEnergy - moleEnergy2;
		float num = Mathf.Abs(ThermalPlantEnergy) * PlantStatus.TemperatureEfficiency * minBreathedRatio;
		MoleQuantity val = ((moleEnergy3 > MoleEnergy.Zero) ? new MoleQuantity(num / moleEnergy3.ToFloat()) : MoleQuantity.MaxValue);
		val = RocketMath.Min(val, MolesDrunkLastTick);
		GasMixture gasMixture2 = GasMixtureHelper.Create();
		foreach (GasQuantityData exhaledGase2 in lifeRequirements.Data.ExhaledGases)
		{
			float num2 = 1f;
			switch (exhaledGase2.Type)
			{
			case Chemistry.GasType.Oxygen:
				num2 = OxygenRatio;
				break;
			case Chemistry.GasType.Methane:
				num2 = VolatileRatio;
				break;
			}
			MoleQuantity moleQuantity = val * num2 * (float)lifeRequirements.GasProduction;
			gasMixture2.Add(new Mole(exhaledGase2.Type, moleQuantity, MoleEnergy.Zero));
			zero += moleQuantity;
		}
		gasMixture2.AddEnergy(IdealGas.Energy(gasMixture2.HeatCapacity, BreathingAtmosphere.Temperature));
		BreathingAtmosphere.Add(gasMixture2);
		BreathingAtmosphere.GasMixture.RemoveEnergy(new MoleEnergy(num));
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (BreathingAtmosphere == null || BreathingAtmosphere.BeingDestroyed)
		{
			return;
		}
		TakePlantBreath();
		MolesDrunkLastTick = TakePlantDrink();
		PlantStatus.HydrationEfficiency = (MolesDrunkLastTick / lifeRequirements.WaterPerTick).ToFloat();
		bool currentState = PlantStatus.GetCurrentState(PlantStatusType.Lit);
		PlantStatus.SetCurrentState(PlantStatusType.Dehydrated, (MolesDrunkLastTick / lifeRequirements.WaterPerTick).ToFloat() < 0.5f);
		bool flag = CurrentLightExposure > 0.01f;
		PlantStatus.SetCurrentState(PlantStatusType.Lit, flag);
		PlantStatus.SetCurrentState(PlantStatusType.Darkness, !flag);
		if (flag != currentState)
		{
			PlantRecord.LightStress += 0.01f;
		}
		PressurekPa zero = PressurekPa.Zero;
		foreach (GasPressureData harmfulGase in lifeRequirements.Data.HarmfulGases)
		{
			float num = harmfulGase.PartialPressure * (float)lifeRequirements.UndesiredGasResistance;
			if (BreathingAtmosphere.PartialPressure(harmfulGase.Type).ToFloat() > num)
			{
				zero += BreathingAtmosphere.PartialPressure(harmfulGase.Type);
			}
		}
		PlantStatus.SetCurrentState(PlantStatusType.Suffocated, PlantStatus.BreathingEfficiency < 0.5f);
		PlantStatus.SetCurrentState(PlantStatusType.UnDesiredGas, !zero.IsDenormalOrZero());
		if (PlantStatus.WillDamage(PlantStatusType.Dehydrated, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, (1f - PlantStatus.HydrationEfficiency) * DamageScale, DamageUpdateType.Hydration);
		}
		if (PlantStatus.WillDamage(PlantStatusType.HighTemperature, this) || PlantStatus.WillDamage(PlantStatusType.HighWaterTemperature, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, DamageScale, DamageUpdateType.Burn);
		}
		if (PlantStatus.WillDamage(PlantStatusType.LowTemperature, this) || PlantStatus.WillDamage(PlantStatusType.LowWaterTemperature, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, DamageScale, DamageUpdateType.Burn);
		}
		if (PlantStatus.WillDamage(PlantStatusType.Darkness, this) || PlantStatus.WillDamage(PlantStatusType.Lit, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, DamageScale, DamageUpdateType.Burn);
		}
		if (PlantStatus.WillDamage(PlantStatusType.Suffocated, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, DamageScale, DamageUpdateType.Oxygen);
		}
		if (PlantStatus.WillDamage(PlantStatusType.UnDesiredGas, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, zero.ToFloat() * DamageScale, DamageUpdateType.Toxic);
		}
		if (PlantStatus.WillDamage(PlantStatusType.LowPressure, this) || PlantStatus.WillDamage(PlantStatusType.HighPressure, this))
		{
			DamageState.Damage(ChangeDamageType.Increment, DamageScale, DamageUpdateType.Brute);
		}
		PlantStatus.PrepareStateNetworkMessage();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (IsPlanted && Stage == 0)
		{
			RefreshVisualizers();
		}
	}

	private void SetGrowthEfficiencyTooltip()
	{
		if (GameManager.RunSimulation)
		{
			GrowthEfficiencyPercent = (ushort)Mathf.RoundToInt(lifeRequirements.GrowthEfficiency() * 100f);
		}
	}

	public void OnLifeTick(float deltaTime)
	{
		if (GrowthStates.Count == 0)
		{
			return;
		}
		SetGrowthEfficiencyTooltip();
		float num = lifeRequirements.GrowthEfficiency();
		if (DamageState.Total > 0f && PlantStatus.CanHeal(this))
		{
			DamageState.Heal(0.25f * num * FertilizerBoost);
		}
		PlantRecord.UpdateRecord(this, deltaTime);
		if (MatureIndex >= 0)
		{
			MaturityRatio = Mathf.Clamp01((float)Stage / (float)MatureIndex);
		}
		if (SeedingIndex >= 0)
		{
			SeedingRatio = Mathf.Clamp01((float)Stage / (float)SeedingIndex);
		}
		if (!(CurrentStage.Length < 0f))
		{
			_stageTime += deltaTime * num * FertilizerBoost;
			if (float.IsNaN(_stageTime))
			{
				_stageTime = 0f;
			}
			if (_stageTime > CurrentStage.Length)
			{
				SetNextStage();
			}
			if (CustomPlantGrowthSpeed)
			{
				SetCustomPlantGrowth();
			}
		}
	}

	private void ResetPerennialStage()
	{
		int stage = 0;
		for (int i = 0; i < GrowthStates.Count; i++)
		{
			if (GrowthStates[i].Mature)
			{
				stage = i - 1;
				break;
			}
		}
		Stage = stage;
		_stageTime = 0f;
		if (GetFertiliserForPlant(out var fertiliser))
		{
			ApplyFertilizer(fertiliser);
			fertiliser.UseOneCycle();
		}
		else
		{
			FertilizerBoost = 1f;
			FertilizerHarvestQuantityBoost = 0f;
		}
		GrowthStateChanged(this);
	}

	private bool GetFertiliserForPlant(out Fertiliser fertiliser)
	{
		fertiliser = ParentTray.PlantToFertiliserSlotMapping(base.ParentSlot.Action).FertiliserSlot.Occupant as Fertiliser;
		return (object)fertiliser != null;
	}

	public void SetNextStage()
	{
		int num = 0;
		num = Mathf.Clamp(Stage + 1, 0, GrowthStates.Count - 1);
		if (num >= GrowthStates.Count - 1)
		{
			AllPlants.Remove(this);
		}
		Stage = num;
		_stageTime = 0f;
		if (IsMature && !IsSeeding)
		{
			lifeRequirements.SetHarvestQuantityOnMature();
		}
		if (IsSeeding)
		{
			SeedQuantity = 1;
		}
		GrowthStateChanged(this);
	}

	private void SetCustomPlantGrowth()
	{
		if (CurrentStage.Length > 0f)
		{
			int num = Mathf.Clamp(Stage + 1, 0, GrowthStates.Count - 1);
			if (num >= GrowthStates.Count - 1)
			{
				AllPlants.Remove(this);
			}
			Stage = num;
			_stageTime = 0f;
			GrowthStateChanged(this);
		}
	}

	public float EatAmount(Entity eater)
	{
		return 1f;
	}

	public float Nutrition(float useAmount)
	{
		return NutritionValue * useAmount;
	}

	public float EatTime(float quantityToEat)
	{
		return 1f * quantityToEat;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this || NutritionValue <= 0f)
		{
			return base.OnUseSecondary(doAction);
		}
		if (RootParentHuman != null && (RootParentHuman.GetNutritionStorage() <= RootParentHuman.Nutrition || !RootParentHuman.CanEat()))
		{
			return null;
		}
		float quantityToEat = EatAmount(RootParent as Entity);
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = EatTime(quantityToEat),
			ActionMessage = ActionStrings.Consume,
			ActionSoundHash = Item.EatingHash,
			ActionCompleteSoundHash = Item.EatingFinishedHash
		};
		if (!doAction)
		{
			return result;
		}
		if (actionCompletedRatio >= 1f)
		{
			OnUseItem(actionCompletedRatio, RootParent);
		}
		return result;
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override int RemoveQuantity(int quantityToRemove)
	{
		int num = base.RemoveQuantity(quantityToRemove);
		if (num > 0)
		{
			StackedGeneCollections.RemoveRange(StackedGeneCollections.Count - num, num);
		}
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 32;
		}
		return num;
	}

	public override bool DecrementQuantity()
	{
		bool num = base.DecrementQuantity();
		if (num)
		{
			StackedGeneCollections.RemoveAt(StackedGeneCollections.Count - 1);
		}
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 32;
		}
		return num;
	}

	public override void SetQuantity(int newQuantity)
	{
		base.SetQuantity(newQuantity);
		if (StackedGeneCollections.Count > base.Quantity)
		{
			StackedGeneCollections.RemoveRange(base.Quantity, StackedGeneCollections.Count);
		}
		while (StackedGeneCollections.Count < base.Quantity)
		{
			StackedGeneCollections.Add(new GeneCollection());
		}
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 32;
		}
	}

	public void SplitStackIntoEmptySlot(Slot slot, int quantity)
	{
		if (GameManager.RunSimulation && quantity <= base.Quantity && !slot.Occupant)
		{
			base.Quantity -= quantity;
			Plant plant = OnServer.Create<Plant>(base.SourcePrefab, slot);
			plant.Quantity = Mathf.Min(quantity, plant.MaxQuantity);
			plant.DamageState.Copy(DamageState);
			OnSplitStack(plant);
			List<GeneCollection> range = StackedGeneCollections.GetRange(base.Quantity, plant.Quantity);
			plant.StackedGeneCollections = range;
			StackedGeneCollections.RemoveRange(base.Quantity, plant.Quantity);
		}
	}

	protected override void SplitStack(Interaction interaction, int quantity)
	{
		if (!GameManager.RunSimulation || quantity > base.Quantity)
		{
			return;
		}
		Human rootParentHuman = interaction.SourceSlot.Parent.RootParentHuman;
		if (rootParentHuman == null)
		{
			return;
		}
		base.Quantity -= quantity;
		Vector3 vector = (rootParentHuman.AimIk ? rootParentHuman.HelmetSlot.Location.position : RootParent.CenterPosition);
		Vector3 vector2 = (rootParentHuman.AimIk ? rootParentHuman.HelmetSlot.Location.forward : RootParent.ThingTransform.forward);
		Vector3 safeDropPosition = GetSafeDropPosition(vector + vector2 * 0.1f, vector2, 0.5f);
		Plant plant = OnServer.Create<Plant>(base.SourcePrefab, safeDropPosition, interaction.SourceThing.ThingTransform.rotation);
		if (!(plant == null))
		{
			if (CustomColor.IsSet)
			{
				OnServer.SetCustomColor(plant, CustomColor.Index);
			}
			plant.Quantity = Mathf.Min(quantity, plant.MaxQuantity);
			plant.DamageState.Copy(DamageState);
			OnSplitStack(plant);
			SplitIntoHand(interaction, rootParentHuman, plant);
			List<GeneCollection> range = StackedGeneCollections.GetRange(base.Quantity, plant.Quantity);
			plant.StackedGeneCollections = range;
			StackedGeneCollections.RemoveRange(base.Quantity, plant.Quantity);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32;
			}
		}
	}

	public override Stackable SplitStack(int splitQuantity, Slot slot)
	{
		base.Quantity -= splitQuantity;
		Stackable stackable = OnServer.Create<Stackable>(base.SourcePrefab, slot);
		if (stackable == null)
		{
			return null;
		}
		if (CustomColor.IsSet)
		{
			OnServer.SetCustomColor(stackable, CustomColor.Index);
		}
		stackable.Quantity = Mathf.Min(splitQuantity, stackable.MaxQuantity);
		stackable.DamageState.Copy(DamageState);
		OnSplitStack(stackable);
		List<GeneCollection> range = StackedGeneCollections.GetRange(base.Quantity, stackable.Quantity);
		stackable.StackedGeneCollections = range;
		StackedGeneCollections.RemoveRange(base.Quantity, stackable.Quantity);
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 32;
		}
		return stackable;
	}

	public override void Merge(IMergeable mergeable)
	{
		if (mergeable is Plant plant)
		{
			int num = Mathf.Min(plant.Quantity + base.Quantity, MaxQuantity) - base.Quantity;
			OnMergeStack(plant, num);
			plant.Quantity -= num;
			base.Quantity += num;
			if (DamageState.Total > plant.DamageState.Total)
			{
				plant.DamageState.Copy(DamageState);
			}
			else
			{
				DamageState.Copy(plant.DamageState);
			}
			List<GeneCollection> range = plant.StackedGeneCollections.GetRange(0, plant.Quantity);
			while (plant.Quantity + num > plant.StackedGeneCollections.Count)
			{
				plant.StackedGeneCollections.Add(new GeneCollection());
			}
			List<GeneCollection> range2 = plant.StackedGeneCollections.GetRange(plant.Quantity, num);
			plant.StackedGeneCollections = range;
			StackedGeneCollections.AddRange(range2);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32;
			}
			if (plant.Quantity == 0 && GameManager.RunSimulation)
			{
				OnServer.Destroy(plant);
			}
		}
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		PlantStatus.PrintDebugInfo();
		ConsoleWindow.Print("\n");
		PlantRecord.PrintDebugInfo();
		ConsoleWindow.Print("\n");
		lifeRequirements.PrintDebugInfo();
		ConsoleWindow.Print("\n");
		PrintGeneInformation();
	}

	private void PrintGeneInformation()
	{
		ConsoleWindow.Print("\n");
		ConsoleWindow.Print("Gene Information");
		foreach (GeneCollection stackedGeneCollection in StackedGeneCollections)
		{
			ConsoleWindow.Print("\n");
			ConsoleWindow.Print($"Gene Collection {StackedGeneCollections.IndexOf(stackedGeneCollection)}");
			foreach (KeyValuePair<Gene, GeneWrapper> item in stackedGeneCollection.Lookup)
			{
				ConsoleWindow.Print($"{Enum.GetName(typeof(Gene), item.Key)} Value/Stability: {item.Value.Value}/{item.Value.Stability}");
			}
		}
	}

	public void GenerateSpawnGases(float alcoholAmount)
	{
		_plantSpawnGasList = new SpawnGas[2]
		{
			new SpawnGas(Chemistry.GasType.LiquidAlcohol, alcoholAmount, TemperatureKelvin.FromCelsius(29f)),
			new SpawnGas(Chemistry.GasType.PollutedWater, alcoholAmount / 20f, TemperatureKelvin.FromCelsius(29f))
		};
	}

	public static void RefreshAll()
	{
		foreach (Plant allPlant in AllPlants)
		{
			allPlant.GameObject.SetActive(value: true);
		}
		foreach (Plant allPlant2 in AllPlants)
		{
			allPlant2.SetOcclusion();
		}
	}

	public bool WillHarvest(out Thing thing)
	{
		if (IsSeeding)
		{
			if (SeedQuantity > 0)
			{
				thing = SeedObject;
				return true;
			}
			if (HarvestQuantity > 0)
			{
				thing = FruitObject;
				return true;
			}
		}
		if (IsMature && HarvestQuantity > 0)
		{
			thing = FruitObject;
			return true;
		}
		thing = null;
		return false;
	}
}
