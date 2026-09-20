using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Items;
using Objects.Rockets.UI;
using Reagents;
using UnityEngine;

namespace Objects.Rockets.Mining;

public class MineableDeposit : INetworkNullable
{
	public const float SLOWEST_DEPLOY_TIME = 30f;

	public const float FASTEST_DEPLOY_TIME = 60f;

	public const float SLOWEST_MINE_TIME = 10f;

	public const float FASTEST_MINE_TIME = 6f;

	private const float MIN_DENSITY_VALUE = 0f;

	private const float MIN_RICHNESS_VALUE = 1f;

	private const float MIN_SIZE_VALUE = 1f;

	public const float MAX_VALUE = 10f;

	private const float MIN_SIZE_MODIFIER = 0.2f;

	private const float MAX_SIZE_MODIFIER = 1f;

	private const float MIN_MINE_BASELINE = 2f;

	private const float MAX_MINE_BASELINE = 6f;

	private const float RICHNESS_EXPONENT = 1.6f;

	private const float RICHNESS_BASE_REDUCTION = 0.01f;

	private const float DENSITY_BASE_REDUCTION = 0.001f;

	public float Density;

	public float Richness;

	public float Size;

	public uint MinedQuantityTotal;

	public uint TotalOreAtLocation;

	public MineableDepositType DepositType;

	public float SpawnRatioSum;

	private DepositComposition _depositComposition;

	public SpaceMapNode Parent { get; private set; }

	public bool IsDepleted => Density <= 0f;

	public DepositComposition DepositComposition
	{
		get
		{
			return _depositComposition;
		}
		private set
		{
			_depositComposition = value;
			SetMineableDepositType();
		}
	}

	public bool TargetReached(SurveyTarget surveyTarget)
	{
		return surveyTarget switch
		{
			SurveyTarget.None => false, 
			SurveyTarget.Composition => Parent.SurveyPercent >= 10f, 
			SurveyTarget.Size => Parent.SurveyPercent >= 30f, 
			SurveyTarget.Density => Parent.SurveyPercent >= 60f, 
			SurveyTarget.Richness => Parent.SurveyPercent >= 100f, 
			SurveyTarget.TenPercentBoost => Parent.SurveyPercent >= 200f, 
			SurveyTarget.TwentyFivePercentBoost => Parent.SurveyPercent >= 1000f, 
			_ => false, 
		};
	}

	public static float RichnessReduction(float richness, float size)
	{
		return Mathf.Pow(richness / 10f + 0.75f, 5f) * 0.01f / SizeModifier(size);
	}

	public static float DensityReduction(float size)
	{
		return 0.001f / SizeModifier(size);
	}

	private static float SizeModifier(float size)
	{
		return RocketMath.MapToScale(1f, 10f, 0.2f, 1f, size);
	}

	public float TimeToMine()
	{
		return RocketMath.MapToScale(0f, 10f, 10f, 6f, Density);
	}

	public float TimeToDeploy()
	{
		return RocketMath.MapToScale(0f, 10f, 30f, 60f, Density);
	}

	public float MineBaseLine()
	{
		return RocketMath.MapToScale(1f, 10f, 2f, 6f, Size);
	}

	public static float RichnessMultiplier(float richness)
	{
		return Mathf.Pow(richness, 1.6f);
	}

	public int OreQuantity()
	{
		float num = 0f;
		if (!IsDepleted)
		{
			num = MineBaseLine() * RichnessMultiplier(Richness);
		}
		num *= DepositTypeMultiplier();
		if (TargetReached(SurveyTarget.TwentyFivePercentBoost))
		{
			num *= 1.25f;
		}
		else if (TargetReached(SurveyTarget.TenPercentBoost))
		{
			num *= 1.1f;
		}
		return Mathf.Max(Mathf.RoundToInt(num), 1);
	}

	public ReagentMixture GetDepositReagentMixture()
	{
		if (DepositType != MineableDepositType.Ore || DepositComposition.ReagentMixes.Count == 0)
		{
			return ReagentMixture.Empty;
		}
		return DepositComposition.ReagentMixes[0].Mixture;
	}

	public GasMixture GetDepositGasMixture()
	{
		if (DepositType == MineableDepositType.Gas && DepositComposition.Gasses.Count > 0)
		{
			return DepositComposition.Gasses[0].AsGasMixture();
		}
		if (DepositType == MineableDepositType.Ice && DepositComposition.FrozenGasses.Count > 0)
		{
			return DepositComposition.FrozenGasses[0].AsGasMixture();
		}
		return GasMixtureHelper.Invalid;
	}

	public float DepositTypeMultiplier()
	{
		return DepositType switch
		{
			MineableDepositType.None => 1f, 
			MineableDepositType.Ore => 1f, 
			MineableDepositType.ReagentMix => 1.25f, 
			MineableDepositType.Ice => 4f, 
			MineableDepositType.Gas => 1f, 
			MineableDepositType.Junk => 1f, 
			_ => 1f, 
		};
	}

	public void OnDepositMined()
	{
		Richness = Mathf.Max(1f, Richness - RichnessReduction(Richness, Size));
		Density = Mathf.Max(0f, Density - DensityReduction(Size));
		Parent.FlagToSendNetworkUpdate(8);
		Parent.OnMined();
	}

	private void CalculateTotalOreAtLocation()
	{
		float num = Density;
		float num2 = Richness;
		float num3 = 0f;
		for (int i = 0; i < 10000; i++)
		{
			if (num == 0f)
			{
				break;
			}
			float num4 = MineBaseLine() * Mathf.Pow(num2, 1.6f) * DepositTypeMultiplier();
			if (num4 == 0f)
			{
				break;
			}
			num3 += num4;
			num2 = Mathf.Max(1f, num2 - RichnessReduction(num2, Size));
			num = Mathf.Max(0f, num - DensityReduction(Size));
		}
		TotalOreAtLocation = (uint)RocketMath.RoundUpToLeftmostPlace(num3);
	}

	public void SetParent(SpaceMapNode parent)
	{
		Parent = parent;
	}

	public MineableDeposit()
	{
	}

	public void Apply(MineableDepositSaveData data)
	{
		Density = data.Density;
		Richness = data.Richness;
		Size = data.Size;
		MinedQuantityTotal = data.MinedQuantityTotal;
		TotalOreAtLocation = data.TotalOreAtLocation;
	}

	public void SetMineableDepositType()
	{
		if (DepositComposition.Ores.Count > 0 || DepositComposition.ReagentMixes.Count > 0)
		{
			DepositType |= MineableDepositType.Ore;
		}
		if (DepositComposition.FrozenGasses.Count > 0)
		{
			DepositType |= MineableDepositType.Ice;
		}
		if (DepositComposition.Gasses.Count > 0)
		{
			DepositType |= MineableDepositType.Gas;
		}
	}

	public MineableDeposit(MineableDepositData data)
	{
		Density = Random.Range((float)data.DensityData.Min, (float)data.DensityData.Max);
		Richness = Random.Range((float)data.RichnessData.Min, (float)data.RichnessData.Max);
		Size = Random.Range((float)data.SizeData.Min, (float)data.SizeData.Max);
		Density = Mathf.Clamp(Density, 0f, 10f);
		Richness = Mathf.Clamp(Richness, 1f, 10f);
		Size = Mathf.Clamp(Size, 1f, 10f);
		DepositComposition = data.DepositCompositionData?.ToInstance();
		MinedQuantityTotal = 0u;
		CalculateTotalOreAtLocation();
		CalculateSpawnRatioSum();
	}

	public MineableDeposit(RocketBinaryReader reader)
	{
		float density = reader.ReadSingle();
		float richness = reader.ReadSingle();
		float size = reader.ReadSingle();
		Density = density;
		Richness = richness;
		Size = size;
		DepositComposition = new DepositComposition(reader);
		MinedQuantityTotal = reader.ReadUInt32();
		TotalOreAtLocation = reader.ReadUInt32();
	}

	private void CalculateSpawnRatioSum()
	{
		if (DepositComposition == null)
		{
			return;
		}
		foreach (DepositMaterialOre ore in DepositComposition.Ores)
		{
			SpawnRatioSum += ore.Weight;
		}
		foreach (DepositMaterialReagentMix reagentMix in DepositComposition.ReagentMixes)
		{
			SpawnRatioSum += reagentMix.Weight;
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteSingle(Density);
		writer.WriteSingle(Richness);
		writer.WriteSingle(Size);
		DepositComposition.Write(writer);
		writer.WriteUInt32(MinedQuantityTotal);
		writer.WriteUInt32(TotalOreAtLocation);
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer, MineableDeposit deposit)
	{
		bool flag = deposit != null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			writer.WriteSingle(deposit.Density);
			writer.WriteSingle(deposit.Richness);
			writer.WriteUInt32(deposit.MinedQuantityTotal);
			writer.WriteUInt32(deposit.TotalOreAtLocation);
		}
	}

	public static void DeSerializeDeltaState(RocketBinaryReader reader, MineableDeposit deposit)
	{
		if (reader.ReadBoolean())
		{
			float density = reader.ReadSingle();
			float richness = reader.ReadSingle();
			uint minedQuantityTotal = reader.ReadUInt32();
			uint totalOreAtLocation = reader.ReadUInt32();
			if (deposit != null)
			{
				deposit.Density = density;
				deposit.Richness = richness;
				deposit.MinedQuantityTotal = minedQuantityTotal;
				deposit.TotalOreAtLocation = totalOreAtLocation;
			}
		}
	}

	private void IncrementMinedQuantityTotal(int quantity)
	{
		MinedQuantityTotal += (uint)quantity;
	}

	public GasMixture GetTotalOreAsGasMixture()
	{
		GasMixture result = GasMixtureHelper.Create();
		foreach (DepositMaterialGas frozenGass in DepositComposition.FrozenGasses)
		{
			result.Add(frozenGass.AsGasMixture());
		}
		result.Scale(TotalOreAtLocation);
		return result;
	}

	public int MineDeposit(out Thing prefab, RocketMiningDrillHead drillHead)
	{
		float num = Random.Range(0f, SpawnRatioSum);
		float num2 = 0f;
		OnDepositMined();
		foreach (DepositMaterialOre ore in DepositComposition.Ores)
		{
			num2 += ore.Weight;
			if (num < num2)
			{
				prefab = ore.OrePrefab;
				int num3 = OreQuantity();
				IncrementMinedQuantityTotal(num3);
				return num3;
			}
		}
		foreach (DepositMaterialReagentMix reagentMix in DepositComposition.ReagentMixes)
		{
			num2 += reagentMix.Weight;
			if (num < num2)
			{
				prefab = reagentMix.MixPrefab;
				int num4 = OreQuantity();
				num4 = Mathf.RoundToInt((float)num4 * drillHead.ReagentYieldMultiplier);
				IncrementMinedQuantityTotal(num4);
				return num4;
			}
		}
		foreach (DepositMaterialGas frozenGass in DepositComposition.FrozenGasses)
		{
			num2 += frozenGass.Weight;
			if (num < num2)
			{
				prefab = frozenGass.MixPrefab;
				int num5 = OreQuantity();
				num5 = Mathf.RoundToInt((float)num5 * drillHead.IceYieldMultiplier);
				IncrementMinedQuantityTotal(num5);
				return num5;
			}
		}
		prefab = null;
		return 0;
	}

	public bool SetPrefabValues(Thing thing)
	{
		if (!(thing is Ice ice))
		{
			if (thing is Slag slag)
			{
				DepositMaterialReagentMix depositMaterialReagentMix = DepositComposition.ReagentMixes[0];
				slag.CreatedReagentMixture = new ReagentMixture(depositMaterialReagentMix.Mixture);
				return true;
			}
			return false;
		}
		ice.SpawnContents.Clear();
		DepositMaterialGas depositMaterialGas = DepositComposition.FrozenGasses[0];
		ice.SpawnContents.AddRange(depositMaterialGas.SpawnGasses);
		ice.temperature = depositMaterialGas.Temperature;
		ice.meltTemperature = depositMaterialGas.Temperature;
		return true;
	}

	public void AddToTree(TreeString parentString)
	{
		TreeString treeString = TreeString.Node($"Deposit {DepositType}", parentString);
		TreeString.Variable($"Richness: {Richness:F4}", treeString);
		TreeString.Variable($"Density: {Density:F4}", treeString);
		TreeString.Variable($"Size: {Size:F4}", treeString);
		if (DepositComposition != null)
		{
			DepositComposition.AddToTree(treeString);
		}
	}

	public GasMixture CollectGas()
	{
		if (DepositType != MineableDepositType.Gas || DepositComposition.Gasses.Count == 0)
		{
			return GasMixtureHelper.Invalid;
		}
		GasMixture result = DepositComposition.Gasses[0].AsGasMixture();
		result.Scale(OreQuantity());
		return result;
	}
}
