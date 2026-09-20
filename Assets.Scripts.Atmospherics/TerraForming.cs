using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Genetics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public static class TerraForming
{
	public const float DAYS_TO_GERMINATE = 30f;

	public static Dictionary<int, GlobalPlant> GlobalPlantsLookup = new Dictionary<int, GlobalPlant>();

	public static List<GlobalPlant> GlobalPlants = new List<GlobalPlant>();

	public static bool DrawDebug = false;

	private static double _lastClutterRefreshTotal = -1.0;

	public static Dictionary<Chemistry.GasType, TerraformingGasCurveData> TerraformingGasCurves = new Dictionary<Chemistry.GasType, TerraformingGasCurveData>();

	public static void RegisterGlobalPlant(Plant plantPrefab)
	{
		int prefabHash = plantPrefab.PrefabHash;
		GlobalPlant globalPlant = new GlobalPlant(plantPrefab);
		if (prefabHash != 0 && GlobalPlantsLookup.TryAdd(prefabHash, globalPlant))
		{
			GlobalPlants.Add(globalPlant);
		}
	}

	public static void AddPlantsToWorld(Plant plant)
	{
		if (plant == null)
		{
			return;
		}
		int num = ((!(plant is Seed seed)) ? plant.PrefabHash : seed.PlantType.PrefabHash);
		int key = num;
		if (!GlobalPlantsLookup.TryGetValue(key, out var value))
		{
			return;
		}
		value.Count += plant.Quantity;
		float num2 = 1f / ((float)value.Count / 2f);
		foreach (GeneCollection stackedGeneCollection in plant.StackedGeneCollections)
		{
			value.GeneCollection.LerpTowards(stackedGeneCollection, num2);
		}
	}

	public static void ImguiDebug()
	{
		if (!DrawDebug)
		{
			return;
		}
		ImGui.Begin("Terraforming", (ImGuiWindowFlags)12687);
		Vector2 vector = new Vector2(500f, 800f);
		ImGui.SetWindowPos(new Vector2((float)Screen.width - vector.x, 0f), ImGuiCond.Always);
		ImGui.Columns(1);
		ImGui.NewLine();
		ImGui.Text("Vegetation");
		foreach (GlobalPlant globalPlant in GlobalPlants)
		{
			if (globalPlant.Count > 0)
			{
				ImGui.Text(globalPlant.PlantPrefab.PrefabName);
				ImGui.SameLine();
				ImGui.Text(" ");
				ImGui.SameLine();
				ImGui.Text(StringManager.Get(globalPlant.Count));
			}
		}
	}

	public static void UpdateGlobalVegetation(float deltaTime)
	{
		InventoryManager.Instance.IsRefreshingAllChunkClutter = true;
	}

	public static float GetGhgIndex(GlobalGasMix globalGasMix)
	{
		float num = 0f;
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (gasType != Chemistry.GasType.Undefined && TerraformingGasCurves.TryGetValue(gasType, out var value))
			{
				double milliMolesPerLitre = IdealGas.GetMilliMolesPerLitre(globalGasMix.Volume, globalGasMix.Get(gasType));
				num += value.Curve.Evaluate((float)milliMolesPerLitre);
			}
		}
		return num;
	}

	public static Vector4 GetGasGraphColor(Chemistry.GasType type)
	{
		return type switch
		{
			Chemistry.GasType.Undefined => ImGuiColor.Float4.Grey, 
			Chemistry.GasType.Oxygen => ImGuiColor.Float4.LightBlue, 
			Chemistry.GasType.Nitrogen => ImGuiColor.Float4.Green, 
			Chemistry.GasType.CarbonDioxide => ImGuiColor.Float4.Grey, 
			Chemistry.GasType.Methane => ImGuiColor.Float4.Red, 
			Chemistry.GasType.Pollutant => ImGuiColor.Float4.Magenta, 
			Chemistry.GasType.Water => ImGuiColor.Float4.Blue, 
			Chemistry.GasType.NitrousOxide => ImGuiColor.Float4.Yellow, 
			Chemistry.GasType.Steam => ImGuiColor.Float4.White, 
			Chemistry.GasType.Hydrogen => ImGuiColor.Float4.Orange, 
			_ => ImGuiColor.Float4.Grey, 
		};
	}

	public static void ExportGhgGraphs()
	{
		WorldManager.GameData gameData = new WorldManager.GameData();
		gameData.TerraformingGasCurveDatas.AddRange(TerraformingGasCurves.Values);
		string text = Application.streamingAssetsPath + "\\Data\\Terraforming_TEMP.xml";
		int num = 0;
		while (File.Exists(text))
		{
			text = $"{Application.streamingAssetsPath}\\Data\\Terraforming_TEMP_{num}.xml";
			num++;
		}
		gameData.SaveXml(text);
		ConsoleWindow.PrintAction("Saved " + text + " successfully");
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
	}

	public static void DeserializeOnJoin(RocketBinaryReader reader)
	{
	}

	public static void Serialize(RocketBinaryWriter writer)
	{
	}

	public static void Deserialize(RocketBinaryReader reader)
	{
	}

	public static TerraFormingSaveData Serialize()
	{
		TerraFormingSaveData terraFormingSaveData = new TerraFormingSaveData();
		foreach (GlobalPlant globalPlant in GlobalPlants)
		{
			terraFormingSaveData.PlantSaveDatas.Add(new GlobalPlantSaveData(globalPlant));
		}
		return terraFormingSaveData;
	}

	public static void Deserialize(XmlSaveLoad.WorldData worldData)
	{
		TerraFormingSaveData terraFormingSaveData = worldData.TerraFormingSaveData;
		if (terraFormingSaveData == null)
		{
			return;
		}
		foreach (GlobalPlantSaveData plantSaveData in terraFormingSaveData.PlantSaveDatas)
		{
			int key = Animator.StringToHash(plantSaveData.PrefabId);
			if (!GlobalPlantsLookup.TryGetValue(key, out var value))
			{
				continue;
			}
			value.Count = plantSaveData.Count;
			value.GeneCollection = new GeneCollection();
			value.GeneCollection.PlantCustomName = plantSaveData.GeneCollectionWrapper.PlantCustomName;
			value.GeneCollection.PlanterCustomName = plantSaveData.GeneCollectionWrapper.PlanterCustomName;
			foreach (GeneWrapper geneWrapper in plantSaveData.GeneCollectionWrapper.GeneWrappers)
			{
				value.GeneCollection.Lookup[geneWrapper.Gene] = geneWrapper;
			}
		}
	}

	public static void Clear()
	{
		foreach (GlobalPlant globalPlant in GlobalPlants)
		{
			globalPlant.Count = 0;
			globalPlant.GeneCollection.Reset();
		}
	}

	public static void Initialize()
	{
		foreach (Plant allPlantPrefab in Plant.AllPlantPrefabs)
		{
			RegisterGlobalPlant(allPlantPrefab);
		}
	}
}
