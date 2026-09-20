using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.UI;

public class StationpediaPage
{
	public string Key;

	public string Title;

	public string Description;

	public int SortPriority;

	public bool ImportantPage;

	public string Text = "";

	public string ConstructWithText;

	public string PrefabName;

	public int PrefabHash;

	public string PrefabHashString;

	public string PaintableText;

	public string StackSizeText;

	public int ReagentsHash;

	public string ReagentsType;

	public string UnitText;

	public string ReagentsText;

	public string SpecificHeatText;

	public string MaxLiquidTemperatureText;

	public string FreezeTemperatureText;

	public string BoilingTemperatureText;

	public string MinLiquidPressure;

	public string LatentHeatText;

	public string MolesPerLitreText;

	public string MolesPerLitreInWorldText;

	public string FlashpointText;

	public string AutoIgnitionText;

	public string ConvectionFactorText;

	public string RadiationFactorText;

	public string SolarHeatingFactorText;

	public string BasePowerDraw;

	public string PowerStorage;

	public string PowerGeneration;

	public string MaxPressure;

	public string Volume;

	public string Nutrition;

	public string NutritionQuality;

	public string FermentedGases;

	public string MoodBonus;

	public string GrowthTime;

	public string MemorySize;

	public string MemoryAccess;

	public bool HasMemory;

	public bool HasBindings;

	public string PlaceableInRocket;

	public string RocketMass;

	public string RocketEngineForce;

	public string RocketEngineEfficiency;

	public string RocketEngineExhaustVelocity;

	public string PressureBreakText;

	public string CableBreakText;

	public string InternalAtmosInfoText;

	public StationDrillHeadProperties DrillHeadProperties;

	public StationSuitProperties StationSuitInfo;

	public Chemistry.GasType GasType;

	[XmlElement("DisplayFilter")]
	public SPDAEntryType DisplayFilter;

	public Sprite CustomSpriteToUse;

	public List<string> PageCustomCategories = new List<string>();

	public List<StationSlotsInsert> SlotInserts = new List<StationSlotsInsert>();

	public List<StationBuildCostInsert> HowToBuild = new List<StationBuildCostInsert>();

	public List<StationBuildCostInsert> BuildStates = new List<StationBuildCostInsert>();

	public List<StationStructureVersionInsert> StructVersionInsert = new List<StationStructureVersionInsert>();

	public List<StationBinding> LogicBindings = new List<StationBinding>();

	public List<StationInstruction> LogicInstructions = new List<StationInstruction>();

	public List<StationLogicInsert> LogicInsert = new List<StationLogicInsert>();

	public List<StationLogicInsert> LogicSlotInsert = new List<StationLogicInsert>();

	public List<StationLogicInsert> ModeInsert = new List<StationLogicInsert>();

	public List<StationLogicInsert> ConnectionInsert = new List<StationLogicInsert>();

	public List<StationFoundInInsert> FoundInOre = new List<StationFoundInInsert>();

	public List<StationFoundInInsert> FoundInGas = new List<StationFoundInInsert>();

	public List<StationFoundInInsert> FoundInFermentation = new List<StationFoundInInsert>();

	public List<StationCategoryInsert> ConstructedThings = new List<StationCategoryInsert>();

	public List<StationCategoryInsert> ProducedThingsInserts = new List<StationCategoryInsert>();

	public List<StationCategoryInsert> ConstructedByKits = new List<StationCategoryInsert>();

	public List<StationCategoryInsert> ResourcesUsed = new List<StationCategoryInsert>();

	public List<StationCategoryInsert> UsedIn = new List<StationCategoryInsert>();

	public List<StationCombustionInsert> CombustionInserts = new List<StationCombustionInsert>();

	public List<StationLifeRequirement> LifeRequirements = new List<StationLifeRequirement>();

	private string _parsed;

	private static readonly string _worldHashes = "{WORLD_HASHES}";

	public string Parsed => _parsed;

	public StationpediaPage()
	{
	}

	public StationpediaPage(string key, string title, string text)
	{
		Key = key;
		Title = title;
		Text = text;
	}

	public StationpediaPage(string key, string title)
	{
		Key = key;
		Title = title;
	}

	public void ParsePage()
	{
		_parsed = Localization.ParseHelpText(Text);
		_parsed = _parsed.Replace('[', '<');
		_parsed = _parsed.Replace(']', '>');
		_parsed = _parsed.Replace("\t", string.Empty);
		_parsed = _parsed.TrimStart();
		foreach (string listOfAllListOfObject in Stationpedia.DataHandler.ListOfAllListOfObjects)
		{
			string text = "{LIST_OF_" + listOfAllListOfObject.ToUpper() + "}";
			if (Text.Contains(text))
			{
				PageCustomCategories.Add(listOfAllListOfObject);
			}
			if (Text.Contains(_worldHashes))
			{
				_parsed = _parsed.Replace(_worldHashes, Localization.ParseHelpText(NewWorldMenu.WorldHashes));
			}
			_parsed = _parsed.Replace(text, string.Empty);
		}
		if (string.IsNullOrEmpty(Description))
		{
			Description = _parsed;
		}
	}

	public bool IsRegexMatch(string text, string pattern)
	{
		if (text == PrefabHashString)
		{
			return true;
		}
		if (!Match(pattern, Title) && !Match(pattern, Key))
		{
			return Match(pattern, Description);
		}
		return true;
	}

	private bool Match(string pattern, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		if (value.Length > 255)
		{
			return false;
		}
		return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	}

	public void Clear()
	{
		PageCustomCategories.Clear();
	}
}
