using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Reagents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class ElectronicReader : Cartridge
{
	public RequirementItem RequirementPrefab;

	public GridLayoutGroup RequirementsGrid;

	public Image ThumbnailImage;

	public TextMeshProUGUI IndexText;

	public Type TargetType;

	public TextMeshProUGUI HashText;

	public TextMeshProUGUI FlashpointText;

	public TextMeshProUGUI AutoignitionText;

	public List<RequirementItem> RequirementItems = new List<RequirementItem>();

	public int CurrentIndex;

	public Thing CurrentDevice;

	public static List<RecipeReference> AllRecipes = new List<RecipeReference>();

	public static List<RecipeReference> FurnaceRecipes = new List<RecipeReference>();

	public static List<RecipeReference> AdvancedFurnaceRecipes = new List<RecipeReference>();

	public static List<RecipeReference> ArcFurnaceRecipes = new List<RecipeReference>();

	public static List<RecipeReference> MicrowaveRecipes = new List<RecipeReference>();

	public static List<RecipeReference> AutolatheRecipes = new List<RecipeReference>();

	public static List<RecipeReference> AutomatedOvenRecipes = new List<RecipeReference>();

	public static List<RecipeReference> AdvancedPackagingMachineRecipes = new List<RecipeReference>();

	public static List<RecipeReference> ElectronicsPrinterRecipes = new List<RecipeReference>();

	public static List<RecipeReference> RocketManufactoryRecipes = new List<RecipeReference>();

	public static List<RecipeReference> TerraformingManufactoryRecipes = new List<RecipeReference>();

	public static List<RecipeReference> GasCanisterRecipes = new List<RecipeReference>();

	public static List<RecipeReference> ChemistryStationRecipes = new List<RecipeReference>();

	public static List<RecipeReference> HydraulicPipeBenderRecipes = new List<RecipeReference>();

	public static List<RecipeReference> ToolManufactoryRecipes = new List<RecipeReference>();

	public static List<RecipeReference> OrganicsPrinterRecipes = new List<RecipeReference>();

	public static List<RecipeReference> SecurityPrinterRecipes = new List<RecipeReference>();

	public static List<RecipeReference> ReagentProcessorRecipes = new List<RecipeReference>();

	public static List<RecipeReference> CentrifugeRecipes = new List<RecipeReference>();

	public static List<RecipeReference> PackagingMachineRecipes = new List<RecipeReference>();

	public List<RecipeReference> CurrentRecipes = new List<RecipeReference>(AllRecipes);

	private bool _queueRedraw;

	public static Dictionary<int, List<RecipeReference>> _outputRecipeLookup = new Dictionary<int, List<RecipeReference>>();

	public static Dictionary<int, List<RecipeReference>> _makerRecipeLookup = new Dictionary<int, List<RecipeReference>>();

	private static Dictionary<int, List<IConstructionKit>> _constructKitLookup = new Dictionary<int, List<IConstructionKit>>();

	private static Dictionary<int, List<Item>> _reagentSourceLookup = new Dictionary<int, List<Item>>();

	private static Dictionary<int, List<Ore>> _gasSourceLookup = new Dictionary<int, List<Ore>>();

	public RecipeReference CurrentReference
	{
		get
		{
			if (CurrentIndex > CurrentRecipes.Count - 1 || CurrentIndex < 0)
			{
				CurrentIndex = 0;
			}
			return CurrentRecipes[CurrentIndex];
		}
	}

	public Recipe CurrentRecipe => CurrentRecipes[CurrentIndex].Recipe;

	public override void Awake()
	{
		base.Awake();
		for (int i = 0; i < 32; i++)
		{
			RequirementItem requirementItem = UnityEngine.Object.Instantiate(RequirementPrefab, RequirementsGrid.transform);
			requirementItem.Parent.SetActive(value: false);
			RequirementItems.Add(requirementItem);
		}
		Redraw();
	}

	public override void OnPreScreenUpdate()
	{
		base.OnPreScreenUpdate();
		if (!(CurrentDevice == CursorManager.Instance.FoundThing))
		{
			CurrentDevice = CursorManager.Instance.FoundThing;
			SetCurrentRecipeList();
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		if (_queueRedraw)
		{
			Redraw();
			_queueRedraw = false;
		}
	}

	public void SetCurrentRecipeList()
	{
		if (CurrentDevice is Furnace)
		{
			CurrentRecipes = FurnaceRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is ArcFurnace)
		{
			CurrentRecipes = ArcFurnaceRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is Microwave)
		{
			CurrentRecipes = MicrowaveRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is Autolathe)
		{
			CurrentRecipes.Clear();
			Autolathe autolathe = CurrentDevice as Autolathe;
			foreach (DynamicThing validDynamicThing in autolathe.ValidDynamicThings)
			{
				if (autolathe.Recipes.TryGetValue(validDynamicThing, out var value))
				{
					CurrentRecipes.Add(new RecipeReference(validDynamicThing, value, "StructureAutolathe"));
				}
			}
			_queueRedraw = true;
		}
		else if (CurrentDevice is ElectronicsPrinter)
		{
			CurrentRecipes.Clear();
			ElectronicsPrinter electronicsPrinter = CurrentDevice as ElectronicsPrinter;
			foreach (DynamicThing validDynamicThing2 in electronicsPrinter.ValidDynamicThings)
			{
				if (electronicsPrinter.Recipes.TryGetValue(validDynamicThing2, out var value2))
				{
					CurrentRecipes.Add(new RecipeReference(validDynamicThing2, value2, "StructureElectronicsPrinter"));
				}
			}
			_queueRedraw = true;
		}
		else if (CurrentDevice is SecurityPrinter)
		{
			CurrentRecipes = SecurityPrinterRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is ChemistryStation)
		{
			CurrentRecipes = ChemistryStationRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is HydraulicPipeBender)
		{
			CurrentRecipes.Clear();
			HydraulicPipeBender hydraulicPipeBender = CurrentDevice as HydraulicPipeBender;
			foreach (DynamicThing validDynamicThing3 in hydraulicPipeBender.ValidDynamicThings)
			{
				if (hydraulicPipeBender.Recipes.TryGetValue(validDynamicThing3, out var value3))
				{
					CurrentRecipes.Add(new RecipeReference(validDynamicThing3, value3, "StructureHydraulicPipeBender"));
				}
			}
			_queueRedraw = true;
		}
		else if (CurrentDevice is ToolManufactory)
		{
			CurrentRecipes.Clear();
			ToolManufactory toolManufactory = CurrentDevice as ToolManufactory;
			foreach (DynamicThing validDynamicThing4 in toolManufactory.ValidDynamicThings)
			{
				if (toolManufactory.Recipes.TryGetValue(validDynamicThing4, out var value4))
				{
					CurrentRecipes.Add(new RecipeReference(validDynamicThing4, value4, "StructureToolManufactory"));
				}
			}
			CurrentRecipes = ToolManufactoryRecipes;
			_queueRedraw = true;
		}
		else if (CurrentDevice is OrganicsPrinter)
		{
			CurrentRecipes = OrganicsPrinterRecipes;
			_queueRedraw = true;
		}
		else
		{
			CurrentRecipes = new List<RecipeReference>(AllRecipes);
			_queueRedraw = true;
		}
	}

	public static void ClearRecipe()
	{
		AllRecipes.Clear();
		FurnaceRecipes.Clear();
		ArcFurnaceRecipes.Clear();
		MicrowaveRecipes.Clear();
		AutomatedOvenRecipes.Clear();
		AutolatheRecipes.Clear();
		ElectronicsPrinterRecipes.Clear();
		SecurityPrinterRecipes.Clear();
		ChemistryStationRecipes.Clear();
		HydraulicPipeBenderRecipes.Clear();
		ToolManufactoryRecipes.Clear();
		OrganicsPrinterRecipes.Clear();
		ReagentProcessorRecipes.Clear();
		CentrifugeRecipes.Clear();
		PackagingMachineRecipes.Clear();
		RocketManufactoryRecipes.Clear();
		TerraformingManufactoryRecipes.Clear();
	}

	public override void OnTabletScrollUp()
	{
		base.OnTabletScrollUp();
		CurrentIndex++;
		if (CurrentRecipes != null && CurrentIndex >= CurrentRecipes.Count)
		{
			CurrentIndex = 0;
		}
		if (Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollUpHash);
		}
		Redraw();
	}

	public override void OnTabletScrollDown()
	{
		base.OnTabletScrollDown();
		CurrentIndex--;
		if (CurrentRecipes != null && CurrentIndex < 0)
		{
			CurrentIndex = CurrentRecipes.Count - 1;
		}
		if (Tablet.OnOff && Tablet.Powered)
		{
			Tablet.PlaySound(Tablet.ScrollDownHash);
		}
		Redraw();
	}

	public void SetRequirement(int index, string value, string displayName)
	{
		if (!string.IsNullOrEmpty(value))
		{
			RequirementItems[index].Parent.SetActive(value: true);
			RequirementItems[index].DisplayName.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].Requirement.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].DisplayName.text = displayName;
			RequirementItems[index].Requirement.text = $"<color=green>{value}</color>";
		}
		else
		{
			RequirementItems[index].Parent.SetActive(value: false);
		}
	}

	public void SetRequirement(int index, double value, string displayName, string unit)
	{
		if (value > 0.0)
		{
			RequirementItems[index].Parent.SetActive(value: true);
			RequirementItems[index].DisplayName.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].Requirement.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].DisplayName.text = displayName;
			RequirementItems[index].Requirement.text = $"<color=yellow>{value}{unit}</color>";
		}
		else
		{
			RequirementItems[index].Parent.SetActive(value: false);
		}
	}

	public void SetRequirement(int index, Reagent reagent)
	{
		if (reagent.Quantity > 0.0)
		{
			RequirementItems[index].Parent.SetActive(value: true);
			RequirementItems[index].DisplayName.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].Requirement.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].DisplayName.text = reagent.DisplayName;
			RequirementItems[index].Requirement.text = $"<color=yellow>{reagent.Quantity}{reagent.Unit}</color>";
		}
		else
		{
			RequirementItems[index].Parent.SetActive(value: false);
		}
	}

	public void SetRequirement(int index, float valueStart, float valueStop, string displayName, string unit, float defaultStart, float defaultStop)
	{
		if (!Mathf.Approximately(valueStart, defaultStart) || !Mathf.Approximately(valueStop, defaultStop))
		{
			RequirementItems[index].Parent.SetActive(value: true);
			RequirementItems[index].DisplayName.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].Requirement.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			RequirementItems[index].DisplayName.text = displayName;
			RequirementItems[index].Requirement.text = string.Format("{0} to {1}", valueStart.ToStringPrefix(unit, "yellow"), valueStop.ToStringPrefix(unit, "yellow"));
		}
		else
		{
			RequirementItems[index].Parent.SetActive(value: false);
		}
	}

	public void Redraw()
	{
		if (CurrentRecipes.Count != 0)
		{
			SelectedTitle.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			IndexText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			HashText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			FlashpointText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			AutoignitionText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
			SelectedTitle.text = CurrentReference.DynamicThing.DisplayName;
			ThumbnailImage.sprite = CurrentReference.DynamicThing.Thumbnail;
			IndexText.text = $"{CurrentIndex + 1} out of {CurrentRecipes.Count}";
			HashText.text = CurrentReference.DynamicThing.PrefabHash.ToString();
			FlashpointText.text = CurrentReference.DynamicThing.FlashPointTemperature.ToFloat().ToStringPrefix("K");
			AutoignitionText.text = CurrentReference.DynamicThing.AutoignitionTemperature.ToFloat().ToStringPrefix("K");
			SetRequirement(0, (CurrentReference.Creator != null) ? CurrentReference.Creator.DisplayName : "Unknown", "Machine");
			SetRequirement(1, CurrentRecipe.Time, Localization.GetInterface("ElectronicReaderTime"), Localization.GetInterface("ElectronicReaderSeconds"));
			SetRequirement(2, CurrentRecipe.Energy, Localization.GetInterface("ElectronicReaderEnergy"), Localization.GetInterface("WattEnergyMeasurement"));
			SetRequirement(3, CurrentRecipe.Pressure.Start * 1000f, CurrentRecipe.Pressure.Stop * 1000f, Localization.GetInterface("ElectronicReaderPressure"), Localization.GetInterface("ElectronicReaderPressureSym"), 0f, 1E+09f);
			SetRequirement(4, CurrentRecipe.Temperature.Start, CurrentRecipe.Temperature.Stop, Localization.GetInterface("ElectronicReaderTemperature"), Localization.GetInterface("ElectronicReaderKelvinSymbol"), 1f, 80000f);
			SetRequirement(5, CurrentRecipe.Egg, Localization.GetInterface("ElectronicReaderEgg"), Localization.GetInterface("ElectronicReadereggs"));
			SetRequirement(6, CurrentRecipe.Flour, Localization.GetInterface("ElectronicReaderFlour"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(7, CurrentRecipe.Milk, Localization.GetInterface("ElectronicReaderMilk"), Localization.GetInterface("ElectronicReaderLiquidMeasurement"));
			SetRequirement(8, CurrentRecipe.Iron, Localization.GetInterface("ElectronicReaderIron"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(9, CurrentRecipe.Gold, Localization.GetInterface("ElectronicReaderGold"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(10, CurrentRecipe.Carbon, Localization.GetInterface("ElectronicReaderCarbon"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(11, CurrentRecipe.Copper, Localization.GetInterface("ElectronicReaderCopper"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(12, CurrentRecipe.Steel, Localization.GetInterface("ElectronicReaderSteel"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(13, CurrentRecipe.Uranium, Localization.GetInterface("ElectronicReaderUranium"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(14, CurrentRecipe.Hydrocarbon, Localization.GetInterface("ElectronicReaderHydrocarbon"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(15, CurrentRecipe.Silver, Localization.GetInterface("ElectronicReaderSilver"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(16, CurrentRecipe.Nickel, Localization.GetInterface("ElectronicReaderNickel"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(17, CurrentRecipe.Lead, Localization.GetInterface("ElectronicReaderLead"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(18, CurrentRecipe.Electrum, Localization.GetInterface("ElectronicReaderElectrum"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(19, CurrentRecipe.Invar, Localization.GetInterface("ElectronicReaderInvar"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(20, CurrentRecipe.Constantan, Localization.GetInterface("ElectronicReaderConstantan"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(21, CurrentRecipe.Solder, Localization.GetInterface("ElectronicReaderSolder"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(22, CurrentRecipe.Silicon, Localization.GetInterface("ElectronicReaderSilicon"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(23, CurrentRecipe.SalicylicAcid, Localization.GetInterface("ElectronicReaderSalicylicAcid"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(24, CurrentRecipe.Alcohol, Localization.GetInterface("ElectronicReaderAlcohol"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(25, CurrentRecipe.Oil, Localization.GetInterface("ElectronicReaderOil"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(26, CurrentRecipe.Fenoxitone, Localization.GetInterface("ElectronicReaderFenoxitone"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(27, CurrentRecipe.ColorRed, Localization.GetInterface("ElectronicReaderColorRed"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(28, CurrentRecipe.ColorBlue, Localization.GetInterface("ElectronicReaderColorBlue"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(29, CurrentRecipe.ColorYellow, Localization.GetInterface("ElectronicReaderColorYellow"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(30, CurrentRecipe.ColorOrange, Localization.GetInterface("ElectronicReaderColorOrange"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
			SetRequirement(31, CurrentRecipe.ColorGreen, Localization.GetInterface("ElectronicReaderColorGreen"), Localization.GetInterface("ElectronicReaderWeightSymbol"));
		}
	}

	public static void GenerateRecipieList()
	{
		Ingot.AllAlloyPrefabs.Clear();
		foreach (KeyValuePair<Recipe, IQuantity> recipe in Furnace.RecipeComparable.Recipes)
		{
			FurnaceRecipes.Add(new RecipeReference(recipe, "StructureFurnace"));
			Ingot ingot = recipe.Value as Ingot;
			if (ingot != null && recipe.Key.CountTypes > 1 && !Ingot.AllAlloyPrefabs.Contains(ingot))
			{
				Ingot.AllAlloyPrefabs.Add(ingot);
				ingot.IngotType = IngotType.Alloy;
			}
		}
		AllRecipes.AddRange(FurnaceRecipes);
		Ingot.AllSuperAlloyPrefabs.Clear();
		foreach (KeyValuePair<Recipe, IQuantity> recipe2 in AdvancedFurnace.RecipeComparable.Recipes)
		{
			AdvancedFurnaceRecipes.Add(new RecipeReference(recipe2, "StructureAdvancedFurnace"));
			Ingot ingot2 = recipe2.Value as Ingot;
			if (ingot2 != null && recipe2.Key.CountTypes > 2 && !Ingot.AllSuperAlloyPrefabs.Contains(ingot2))
			{
				Ingot.AllSuperAlloyPrefabs.Add(ingot2);
				ingot2.IngotType = IngotType.SuperAlloy;
			}
		}
		AllRecipes.AddRange(AdvancedFurnaceRecipes);
		foreach (KeyValuePair<Recipe, IQuantity> recipe3 in ArcFurnace.RecipeComparable.Recipes)
		{
			ArcFurnaceRecipes.Add(new RecipeReference(recipe3, "StructureArcFurnace"));
		}
		AllRecipes.AddRange(ArcFurnaceRecipes);
		foreach (KeyValuePair<Recipe, Item> recipe4 in Microwave.RecipeComparable.Recipes)
		{
			MicrowaveRecipes.Add(new RecipeReference(recipe4, "ApplianceMicrowave"));
		}
		AllRecipes.AddRange(MicrowaveRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> recipe5 in Autolathe.RecipeComparable.Recipes)
		{
			AutolatheRecipes.Add(new RecipeReference(recipe5, "StructureAutolathe"));
		}
		AllRecipes.AddRange(AutolatheRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> recipe6 in AutomatedOven.RecipeComparable.Recipes)
		{
			AutomatedOvenRecipes.Add(new RecipeReference(recipe6, "StructureAutomatedOven"));
		}
		AllRecipes.AddRange(AutomatedOvenRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe in ElectronicsPrinter.RecipeComparable.AllRecipes)
		{
			ElectronicsPrinterRecipes.Add(new RecipeReference(allRecipe, "StructureElectronicsPrinter"));
		}
		AllRecipes.AddRange(ElectronicsPrinterRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe2 in SecurityPrinter.RecipeComparable.AllRecipes)
		{
			SecurityPrinterRecipes.Add(new RecipeReference(allRecipe2, "StructureSecurityPrinter"));
		}
		AllRecipes.AddRange(SecurityPrinterRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe3 in RocketManufactory.RecipeComparable.AllRecipes)
		{
			RocketManufactoryRecipes.Add(new RecipeReference(allRecipe3, "StructureRocketManufactory"));
		}
		AllRecipes.AddRange(RocketManufactoryRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe4 in TerraformingManufactory.RecipeComparable.AllRecipes)
		{
			TerraformingManufactoryRecipes.Add(new RecipeReference(allRecipe4, "StructureTerraformingManufactory"));
		}
		AllRecipes.AddRange(TerraformingManufactoryRecipes);
		foreach (KeyValuePair<Recipe, Item> allRecipe5 in ChemistryStation.RecipeComparable.AllRecipes)
		{
			ChemistryStationRecipes.Add(new RecipeReference(allRecipe5, "ApplianceChemistryStation"));
		}
		AllRecipes.AddRange(ChemistryStationRecipes);
		foreach (KeyValuePair<int, Item> recipe7 in ReagentProcessor.RecipeComparable.Recipes)
		{
			ReagentProcessorRecipes.Add(new RecipeReference(recipe7, "ApplianceReagentProcessor"));
		}
		AllRecipes.AddRange(ReagentProcessorRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe6 in HydraulicPipeBender.RecipeComparable.AllRecipes)
		{
			HydraulicPipeBenderRecipes.Add(new RecipeReference(allRecipe6, "StructureHydraulicPipeBender"));
		}
		AllRecipes.AddRange(HydraulicPipeBenderRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe7 in ToolManufactory.RecipeComparable.AllRecipes)
		{
			ToolManufactoryRecipes.Add(new RecipeReference(allRecipe7, "StructureToolManufactory"));
		}
		AllRecipes.AddRange(ToolManufactoryRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe8 in OrganicsPrinter.RecipeComparable.AllRecipes)
		{
			OrganicsPrinterRecipes.Add(new RecipeReference(allRecipe8, "StructureOrganicsPrinter"));
		}
		AllRecipes.AddRange(OrganicsPrinterRecipes);
		foreach (KeyValuePair<Recipe, Ore> allRecipe9 in Centrifuge.RecipeComparable.AllRecipes)
		{
			CentrifugeRecipes.Add(new RecipeReference(allRecipe9, "StructureCentrifuge"));
		}
		AllRecipes.AddRange(CentrifugeRecipes);
		foreach (KeyValuePair<Recipe, Item> allRecipe10 in BasicPackagingMachine.RecipeComparable.AllRecipes)
		{
			Recipe key = allRecipe10.Key;
			KeyValuePair<Recipe, Item> record = new KeyValuePair<Recipe, Item>(key, allRecipe10.Value);
			PackagingMachineRecipes.Add(new RecipeReference(record, "AppliancePackagingMachine"));
		}
		AllRecipes.AddRange(PackagingMachineRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> recipe8 in AdvancedPackagingMachine.RecipeComparable.Recipes)
		{
			Recipe value = recipe8.Value;
			KeyValuePair<DynamicThing, Recipe> record2 = new KeyValuePair<DynamicThing, Recipe>(recipe8.Key, value);
			AdvancedPackagingMachineRecipes.Add(new RecipeReference(record2, "StructureAdvancedPackagingMachine"));
		}
		AllRecipes.AddRange(AdvancedPackagingMachineRecipes);
		_outputRecipeLookup.Clear();
		_makerRecipeLookup.Clear();
		foreach (RecipeReference allRecipe11 in AllRecipes)
		{
			if (!(allRecipe11.Creator == null) && !(allRecipe11.DynamicThing == null))
			{
				AddToLookup(allRecipe11.DynamicThing, allRecipe11, ref _outputRecipeLookup);
				AddToLookup(allRecipe11.Creator, allRecipe11, ref _makerRecipeLookup);
			}
		}
		foreach (List<RecipeReference> value2 in _outputRecipeLookup.Values)
		{
			value2.Sort((RecipeReference a, RecipeReference b) => string.Compare(a.Creator.DisplayName, b.Creator.DisplayName, StringComparison.Ordinal));
		}
		foreach (List<RecipeReference> value3 in _makerRecipeLookup.Values)
		{
			value3.Sort((RecipeReference a, RecipeReference b) => string.Compare(a.DynamicThing.DisplayName, b.DynamicThing.DisplayName, StringComparison.Ordinal));
		}
		GC.Collect();
	}

	public static void AddToLookup(IConstructionKit creator)
	{
		foreach (Thing constructedPrefab in creator.GetConstructedPrefabs())
		{
			if (constructedPrefab == null)
			{
				break;
			}
			AddToLookup(creator, constructedPrefab);
		}
	}

	public static void AddToLookup(Item reagentSource)
	{
		if (reagentSource.CreatedReagentMixture.TotalReagents <= 0.0)
		{
			return;
		}
		foreach (Reagent allReagent in Reagent.AllReagents)
		{
			if (!(reagentSource.CreatedReagentMixture.Get(allReagent) <= 0.0))
			{
				AddToLookup(allReagent, reagentSource);
			}
		}
	}

	public static void AddToLookup(Ore reagentSource)
	{
		foreach (SpawnGas spawnContent in reagentSource.SpawnContents)
		{
			if (!(spawnContent.Quantity <= 0f))
			{
				AddToLookup(spawnContent.Type, reagentSource);
			}
		}
	}

	private static void AddToLookup(Chemistry.GasType created, Ore creator)
	{
		_gasSourceLookup.TryGetValue((int)created, out var value);
		if (value == null)
		{
			value = new List<Ore> { creator };
			_gasSourceLookup.Add(created.GetHashCode(), value);
		}
		else
		{
			value.Add(creator);
		}
	}

	private static void AddToLookup(Reagent created, Item creator)
	{
		if (created != null)
		{
			Dictionary<int, List<Item>> reagentSourceLookup = _reagentSourceLookup;
			reagentSourceLookup.TryGetValue(created.GetHashCode(), out var value);
			if (value == null)
			{
				value = new List<Item> { creator };
				reagentSourceLookup.Add(created.GetHashCode(), value);
			}
			else
			{
				value.Add(creator);
			}
		}
	}

	private static void AddToLookup(IConstructionKit creator, Thing created)
	{
		if (!(created == null))
		{
			_constructKitLookup.TryGetValue(created.PrefabHash, out var value);
			if (value == null)
			{
				value = new List<IConstructionKit> { creator };
				_constructKitLookup.Add(created.PrefabHash, value);
			}
			else
			{
				value.Add(creator);
			}
		}
	}

	private static void AddToLookup(Thing key, RecipeReference recipeReference, ref Dictionary<int, List<RecipeReference>> dictionary)
	{
		dictionary.TryGetValue(key.PrefabHash, out var value);
		if (value == null)
		{
			value = new List<RecipeReference> { recipeReference };
			dictionary.Add(key.PrefabHash, value);
		}
		else
		{
			value.Add(recipeReference);
		}
	}

	public static List<RecipeReference> GetAllMyCreators(DynamicThing dynamicThing)
	{
		_outputRecipeLookup.TryGetValue(dynamicThing.PrefabHash, out var value);
		return value;
	}

	public static List<RecipeReference> GetAllRecipies(Thing thing)
	{
		_makerRecipeLookup.TryGetValue(thing.PrefabHash, out var value);
		return value;
	}

	public static List<IConstructionKit> GetAllConstructors(Thing thing)
	{
		_constructKitLookup.TryGetValue(thing.PrefabHash, out var value);
		return value;
	}

	public static List<Item> GetAllSources(Reagent reagentType)
	{
		_reagentSourceLookup.TryGetValue(reagentType.GetHashCode(), out var value);
		return value;
	}

	public static List<Ore> GetAllSources(Chemistry.GasType gasType)
	{
		_gasSourceLookup.TryGetValue((int)gasType, out var value);
		return value;
	}
}
