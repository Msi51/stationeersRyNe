using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class UniversalPage : UserInterfaceBase
{
	public Image PageImage;

	public TextMeshProUGUI PageTitle;

	public TextMeshProUGUI PageDescription;

	public RectTransform Content;

	[Header("Text Info")]
	public TextMeshProUGUI ConstructWithText;

	public TextMeshProUGUI PrefabNameText;

	public TextMeshProUGUI PrefabHashText;

	public TextMeshProUGUI PaintableText;

	public TextMeshProUGUI StackSizeInfo;

	public TextMeshProUGUI ReagentHashText;

	public TextMeshProUGUI ReagentTypeText;

	public TextMeshProUGUI UnitText;

	public TextMeshProUGUI ReagentsText;

	public TextMeshProUGUI SpecificHeat;

	[FormerlySerializedAs("BoilingTemperature")]
	public TextMeshProUGUI FreezeTemperature;

	[FormerlySerializedAs("BoilingPressure")]
	public TextMeshProUGUI MaxLiquidTemperature;

	public TextMeshProUGUI BoilingTemperature;

	[FormerlySerializedAs("MinLiquidPressureRange")]
	public TextMeshProUGUI MinLiquidPressure;

	public TextMeshProUGUI LatentHeat;

	public TextMeshProUGUI MolesPerLitre;

	public TextMeshProUGUI MolesPerLitreInWorld;

	public TextMeshProUGUI FlashPointText;

	public TextMeshProUGUI AutoIgniteText;

	public TextMeshProUGUI HeatTransferConvectionText;

	public TextMeshProUGUI HeatTransferRadiationText;

	public TextMeshProUGUI SolarHeatingFactorText;

	public TextMeshProUGUI DeviceBasePower;

	public TextMeshProUGUI DevicePowerStorage;

	public TextMeshProUGUI DevicePowerGeneration;

	public TextMeshProUGUI MaxPressure;

	public TextMeshProUGUI Volume;

	public TextMeshProUGUI Nutrition;

	public TextMeshProUGUI NutritionQuality;

	public TextMeshProUGUI MoodBonus;

	public TextMeshProUGUI FermentationGases;

	public TextMeshProUGUI GrowthTime;

	public TextMeshProUGUI PlaceableInRocket;

	public TextMeshProUGUI RocketMass;

	public TextMeshProUGUI PressureBreakPipeText;

	public TextMeshProUGUI CableBreakPointText;

	public TextMeshProUGUI InternalAtmosphereText;

	public TextMeshProUGUI MemorySize;

	public TextMeshProUGUI MemoryAccess;

	[Header("Mining Drill Head")]
	public TextMeshProUGUI SpeedMultiplier;

	public TextMeshProUGUI ReagentYieldMultiplier;

	public TextMeshProUGUI IceYieldMultiplier;

	public TextMeshProUGUI HealthMultiplier;

	public TextMeshProUGUI PowerConsumptionMultiplier;

	[Header("Other")]
	public StationpediaCategory SlotContents;

	public StationpediaCategory CostToPrintContents;

	public StationpediaCategory BuildStateContents;

	public StationpediaCategory StructureVersionContents;

	public StationpediaCategory LogicContents;

	public StationpediaCategory LogicBindings;

	public StationpediaCategory LogicInstructions;

	public StationpediaCategory LogicSlotContents;

	public StationpediaCategory ModeContents;

	public StationpediaCategory ConnectionContents;

	public StationpediaCategory LifeRequirements;

	public StationpediaCategory FoundInOreContents;

	public StationpediaCategory FoundInGasContents;

	public StationpediaCategory FoundInFermentationContents;

	public StationpediaCategory ConstructedThingsContents;

	public StationpediaCategory ProducedThingsContents;

	public StationpediaCategory ConstructedByKitsContents;

	public StationpediaCategory ResourcesUsed;

	public StationpediaCategory UsedIn;

	public StationpediaCategory CombustionInfo;

	public GameObject MetabolicSuspension;

	public GameObject CryogenicRegeneration;

	public GameObject ThermalLimitsPanel;

	public GameObject ReagentPanel;

	public GameObject GasPropertiesPanel;

	public GameObject PhaseDiagramPanel;

	public GameObject GeneralInfoPanel;

	public GameObject ConstructWithPanel;

	public GameObject DeviceInformationPanel;

	public GameObject GasCanisterInformation;

	public GameObject AtmopshericsInformation;

	public GameObject NutritionInformation;

	public GameObject GrowthTimeInformation;

	public GameObject DrillHeadInformation;

	public StationpediaRocketPanel rocketInternalsInformation;

	[FormerlySerializedAs("stationpediaSuitThermalsPanel")]
	public StationpediaSuitInfoPanel stationpediaSuitInfoPanel;

	public Image PageHeaderImage;

	[HideInInspector]
	public List<StationpediaCategory> CreatedCategories = new List<StationpediaCategory>();

	private const string LINK_COLOR_FORMAT = "<link={0}><color=#008AE6>{1}</color></link>";

	public void CheckAndSetTextElement(TextMeshProUGUI textMesh, string text, string linkType = "", bool trim = false)
	{
		string text2 = (trim ? Stationpedia.Trim(text) : text);
		if (!string.IsNullOrEmpty(linkType) && !string.IsNullOrEmpty(text2) && !text2.Equals("0"))
		{
			text2 = $"<link={linkType}><color=#008AE6>{text2}</color></link>";
		}
		if (!string.IsNullOrEmpty(text2) && !text2.Equals("0"))
		{
			textMesh.text = text2;
			textMesh.transform.parent.gameObject.SetActive(value: true);
		}
		else
		{
			textMesh.text = string.Empty;
			textMesh.transform.parent.gameObject.SetActive(value: false);
		}
	}

	public void ResetUniversalPageInserts()
	{
		LifeRequirements.ClearChildInserts();
		SlotContents.ClearChildInserts();
		CostToPrintContents.ClearChildInserts();
		BuildStateContents.ClearChildInserts();
		StructureVersionContents.ClearChildInserts();
		LogicContents.ClearChildInserts();
		LogicInstructions.ClearChildInserts();
		LogicBindings.ClearChildInserts();
		LogicSlotContents.ClearChildInserts();
		ModeContents.ClearChildInserts();
		ConnectionContents.ClearChildInserts();
		FoundInOreContents.ClearChildInserts();
		FoundInGasContents.ClearChildInserts();
		FoundInFermentationContents.ClearChildInserts();
		ConstructedThingsContents.ClearChildInserts();
		ProducedThingsContents.ClearChildInserts();
		ConstructedByKitsContents.ClearChildInserts();
		ResourcesUsed.ClearChildInserts();
		UsedIn.ClearChildInserts();
		CombustionInfo.ClearChildInserts();
	}

	public void PopulateCustomCategories(StationpediaPage page)
	{
		foreach (string pageCustomCategory in page.PageCustomCategories)
		{
			foreach (KeyValuePair<string, List<StationCategoryInsert>> item in Stationpedia.DataHandler.GetList(pageCustomCategory))
			{
				StationpediaCategory stationpediaCategory = Object.Instantiate(Stationpedia.Instance.CategoryPrefab, Content);
				stationpediaCategory.Title.text = Stationpedia.Trim(item.Key);
				foreach (StationCategoryInsert newInsertDat in item.Value)
				{
					Sprite sprite = Prefab.Find(newInsertDat.PrefabHash)?.Thumbnail;
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, stationpediaCategory.Contents);
					newInsertDat.ApplyTo(sPDAListItem);
					if ((bool)sprite)
					{
						sPDAListItem.InsertImage.sprite = sprite;
					}
					else if (newInsertDat.InsertImage != null)
					{
						sPDAListItem.InsertImage.sprite = newInsertDat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(newInsertDat.PageLink);
					});
				}
				CreatedCategories.Add(stationpediaCategory);
			}
		}
	}

	public void PopulateSlotInserts(StationpediaPage page)
	{
		if (page.SlotInserts.Count <= 0)
		{
			SlotContents.SetVisible(isVisble: false);
			return;
		}
		SlotContents.SetVisible(isVisble: true);
		foreach (StationSlotsInsert slotInsert in page.SlotInserts)
		{
			SPDASlot sPDASlot = Object.Instantiate(Stationpedia.Instance.SlotInsertPrefab, SlotContents.Contents);
			if ((bool)slotInsert.SlotIcon)
			{
				sPDASlot.SlotImage.sprite = slotInsert.SlotIcon;
			}
			else
			{
				sPDASlot.SlotImage.gameObject.SetActive(value: false);
			}
			sPDASlot.SlotTitle.text = Stationpedia.Trim(string.IsNullOrEmpty(slotInsert.SlotName) ? (slotInsert.SlotType + Localization.GetInterface("SlotKey")) : slotInsert.SlotName);
			sPDASlot.PopulateSlotType(slotInsert.SlotType);
			sPDASlot.PopulateSlotIndex(slotInsert.SlotIndex);
		}
	}

	public void PopulateHowToBuildInserts(StationpediaPage page)
	{
		if (page.HowToBuild.Count > 0)
		{
			CostToPrintContents.SetVisible(isVisble: true);
			{
				foreach (StationBuildCostInsert thingRecipeInfo in page.HowToBuild)
				{
					SPDAManufacturer sPDAManufacturer = Object.Instantiate(Stationpedia.Instance.ManufactureInsertPrefab, CostToPrintContents.Contents);
					string text = Localization.ParseHelpText(thingRecipeInfo.PrinterName);
					if (!string.IsNullOrEmpty(thingRecipeInfo.TierName))
					{
						text = text + " (" + thingRecipeInfo.TierName + ")";
					}
					sPDAManufacturer.PrinterNameTitle.text = Stationpedia.Trim(text);
					sPDAManufacturer.ImageButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(thingRecipeInfo.PageLink);
					});
					sPDAManufacturer.SetText(thingRecipeInfo);
				}
				return;
			}
		}
		CostToPrintContents.SetVisible(isVisble: false);
	}

	public void PopulateBuildStatesInserts(StationpediaPage page)
	{
		if (page.BuildStates.Count > 0)
		{
			BuildStateContents.SetVisible(isVisble: true);
			{
				foreach (StationBuildCostInsert thingRecipeInfo in page.BuildStates)
				{
					SPDAManufacturer sPDAManufacturer = Object.Instantiate(Stationpedia.Instance.ManufactureInsertPrefab, BuildStateContents.Contents);
					sPDAManufacturer.PrinterNameTitle.gameObject.SetActive(value: false);
					sPDAManufacturer.ImageButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(thingRecipeInfo.PageLink);
					});
					sPDAManufacturer.SetText(thingRecipeInfo);
				}
				return;
			}
		}
		BuildStateContents.SetVisible(isVisble: false);
	}

	public void PopulatePhaseDiagram(StationpediaPage page)
	{
		Stationpedia.Instance.PhaseChangeDiagram.Initialize(page.GasType);
	}

	public void PopulateStructureVersion(StationpediaPage page)
	{
		if (page.StructVersionInsert.Count > 0)
		{
			StructureVersionContents.SetVisible(isVisble: true);
			{
				foreach (StationStructureVersionInsert item in page.StructVersionInsert)
				{
					SPDAVersion sPDAVersion = Object.Instantiate(Stationpedia.Instance.MachineTierInsertPrefab, StructureVersionContents.Contents);
					sPDAVersion.BuildTimeMultiplier.text = item.BuildTimeMultiplier;
					sPDAVersion.CreationMultiplier.text = item.CreationMultiplier;
					sPDAVersion.EnergyMultiplier.text = item.EnergyCostMultiplier;
					sPDAVersion.MaterialMultiplier.text = item.MaterialCostMultiplier;
					sPDAVersion.BuildTitle.text = item.StructureVersion;
					sPDAVersion.StructureImage.gameObject.SetActive(value: false);
				}
				return;
			}
		}
		StructureVersionContents.SetVisible(isVisble: false);
	}

	public void PopulateLogicInserts(StationpediaPage page)
	{
		if (page.LogicInsert.Count > 0)
		{
			LogicContents.SetVisible(isVisble: true);
			{
				foreach (StationLogicInsert item in page.LogicInsert)
				{
					SPDALogic sPDALogic = Object.Instantiate(Stationpedia.Instance.LogicInsertPrefab, LogicContents.Contents);
					sPDALogic.InfoReadWrite.text = item.LogicAccessTypes;
					sPDALogic.InfoValue.text = item.LogicName;
				}
				return;
			}
		}
		LogicContents.SetVisible(isVisble: false);
	}

	public void PopulateLogicSlotInserts(StationpediaPage page)
	{
		if (page.LogicSlotInsert.Count > 0)
		{
			LogicSlotContents.SetVisible(isVisble: true);
			{
				foreach (StationLogicInsert item in page.LogicSlotInsert)
				{
					SPDALogic sPDALogic = Object.Instantiate(Stationpedia.Instance.LogicInsertPrefab, LogicSlotContents.Contents);
					sPDALogic.InfoReadWrite.text = item.LogicAccessTypes;
					sPDALogic.InfoValue.text = item.LogicName;
				}
				return;
			}
		}
		LogicSlotContents.SetVisible(isVisble: false);
	}

	public void PopulateLogicBindings(StationpediaPage page)
	{
		if (page.HasBindings)
		{
			LogicBindings.SetVisible(isVisble: true);
			foreach (StationBinding logicBinding in page.LogicBindings)
			{
				SPDAGeneric sPDAGeneric = Object.Instantiate(Stationpedia.Instance.LogicBindingPrefab, LogicBindings.Contents);
				sPDAGeneric.Header.text = logicBinding.Header;
				sPDAGeneric.Info.text = logicBinding.Label;
			}
			LogicBindings.Contents.gameObject.SetActive(page.LogicBindings.Count != 0);
		}
		else
		{
			LogicBindings.SetVisible(isVisble: false);
		}
	}

	public void PopulateLogicInstructions(StationpediaPage page)
	{
		if (page.HasMemory)
		{
			LogicInstructions.SetVisible(isVisble: true);
			foreach (StationInstruction logicInstruction in page.LogicInstructions)
			{
				SPDAGeneric sPDAGeneric = Object.Instantiate(Stationpedia.Instance.GenericPrefab, LogicInstructions.Contents);
				sPDAGeneric.Header.text = logicInstruction.Text;
				sPDAGeneric.Info.text = logicInstruction.Info;
			}
			LogicInstructions.Contents.gameObject.SetActive(page.LogicInstructions.Count != 0);
		}
		else
		{
			LogicInstructions.SetVisible(isVisble: false);
		}
	}

	public void PopulateModeInserts(StationpediaPage page)
	{
		if (page.ModeInsert.Count > 0)
		{
			ModeContents.SetVisible(isVisble: true);
			{
				foreach (StationLogicInsert item in page.ModeInsert)
				{
					SPDALogic sPDALogic = Object.Instantiate(Stationpedia.Instance.LogicInsertPrefab, ModeContents.Contents);
					sPDALogic.InfoReadWrite.text = item.LogicAccessTypes;
					sPDALogic.InfoValue.text = item.LogicName;
				}
				return;
			}
		}
		ModeContents.SetVisible(isVisble: false);
	}

	public void PopulateConnectionInserts(StationpediaPage page)
	{
		if (page.ConnectionInsert.Count > 0)
		{
			ConnectionContents.SetVisible(isVisble: true);
			{
				foreach (StationLogicInsert item in page.ConnectionInsert)
				{
					SPDALogic sPDALogic = Object.Instantiate(Stationpedia.Instance.LogicInsertPrefab, ConnectionContents.Contents);
					sPDALogic.InfoReadWrite.text = item.LogicAccessTypes;
					sPDALogic.InfoValue.text = item.LogicName;
				}
				return;
			}
		}
		ConnectionContents.SetVisible(isVisble: false);
	}

	public void PopulateLifeRequirements(StationpediaPage page)
	{
		if (page.LifeRequirements.Count > 0)
		{
			foreach (StationLifeRequirement lifeRequirement in page.LifeRequirements)
			{
				LifeRequirements.SetVisible(isVisble: true);
				SPDALifeRequirement sPDALifeRequirement = Object.Instantiate(Stationpedia.Instance.LifeRequirementPrefab, LifeRequirements.Contents);
				sPDALifeRequirement.Name.text = lifeRequirement.Name;
				sPDALifeRequirement.Value.text = lifeRequirement.Value;
				sPDALifeRequirement.Value.fontSize = lifeRequirement.ValueSize;
				sPDALifeRequirement.Gene.text = lifeRequirement.Gene;
			}
			return;
		}
		LifeRequirements.SetVisible(isVisble: false);
	}

	public void PopulateOreInserts(StationpediaPage page)
	{
		if (page.FoundInOre.Count > 0)
		{
			foreach (StationFoundInInsert item in page.FoundInOre)
			{
				FoundInOreContents.SetVisible(isVisble: true);
				SPDAFoundIn sPDAFoundIn = Object.Instantiate(Stationpedia.Instance.FoundInInsertPrefab, FoundInOreContents.Contents);
				sPDAFoundIn.ItemFound.text = Stationpedia.Trim(item.NameOfThing);
				sPDAFoundIn.QuantityofItem.text = item.QuantityOfThing;
			}
			return;
		}
		FoundInOreContents.SetVisible(isVisble: false);
	}

	public void PopulateGasInserts(StationpediaPage page)
	{
		if (page.FoundInGas.Count > 0)
		{
			FoundInGasContents.SetVisible(isVisble: true);
			{
				foreach (StationFoundInInsert foundInGa in page.FoundInGas)
				{
					SPDAFoundIn sPDAFoundIn = Object.Instantiate(Stationpedia.Instance.FoundInInsertPrefab, FoundInGasContents.Contents);
					sPDAFoundIn.ItemFound.text = Stationpedia.Trim(foundInGa.NameOfThing);
					sPDAFoundIn.QuantityofItem.text = foundInGa.QuantityOfThing;
				}
				return;
			}
		}
		FoundInGasContents.SetVisible(isVisble: false);
	}

	public void PopulateFermentationInserts(StationpediaPage page)
	{
		if (page.FoundInFermentation.Count > 0)
		{
			FoundInFermentationContents.SetVisible(isVisble: true);
			{
				foreach (StationFoundInInsert item in page.FoundInFermentation)
				{
					SPDAFoundIn sPDAFoundIn = Object.Instantiate(Stationpedia.Instance.FermentationInsertPrefab, FoundInFermentationContents.Contents);
					sPDAFoundIn.ItemFound.text = Stationpedia.Trim(item.NameOfThing);
					sPDAFoundIn.QuantityofItem.text = item.QuantityOfThing;
				}
				return;
			}
		}
		FoundInFermentationContents.SetVisible(isVisble: false);
	}

	public void PopulateCombustionInfo(StationpediaPage page)
	{
		if (page.CombustionInserts.Count > 0)
		{
			CombustionInfo.SetVisible(isVisble: true);
			{
				foreach (StationCombustionInsert combustionInsert in page.CombustionInserts)
				{
					SPDACombustionItem insert = Object.Instantiate(Stationpedia.Instance.CombustionItemPrefab, CombustionInfo.Contents);
					combustionInsert.ApplyTo(insert);
				}
				return;
			}
		}
		CombustionInfo.SetVisible(isVisble: false);
	}

	public void PopulateUsedIn(StationpediaPage page)
	{
		if (page.UsedIn.Count > 0)
		{
			UsedIn.SetVisible(isVisble: true);
			{
				foreach (StationCategoryInsert dat in page.UsedIn)
				{
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, UsedIn.Contents);
					dat.ApplyTo(sPDAListItem);
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(dat.PageLink);
					});
					if ((bool)dat.InsertImage)
					{
						sPDAListItem.InsertImage.sprite = dat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
				}
				return;
			}
		}
		UsedIn.SetVisible(isVisble: false);
	}

	public void PopulateUsedResources(StationpediaPage page)
	{
		if (page.ResourcesUsed.Count > 0)
		{
			ResourcesUsed.SetVisible(isVisble: true);
			{
				foreach (StationCategoryInsert dat in page.ResourcesUsed)
				{
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, ResourcesUsed.Contents);
					dat.ApplyTo(sPDAListItem);
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(dat.PageLink);
					});
					if (dat is StationCategoryInsertSpecial)
					{
						sPDAListItem.InsertImage.sprite = Stationpedia.Instance.ImportantSearchImage;
						sPDAListItem.SetSpecial();
					}
					else if ((bool)dat.InsertImage)
					{
						sPDAListItem.InsertImage.sprite = dat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
				}
				return;
			}
		}
		ResourcesUsed.SetVisible(isVisble: false);
	}

	public void PopulateConstructedThings(StationpediaPage page)
	{
		if (page.ConstructedThings.Count > 0)
		{
			ConstructedByKitsContents.SetVisible(isVisble: true);
			{
				foreach (StationCategoryInsert dat in page.ConstructedThings)
				{
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, ConstructedByKitsContents.Contents);
					dat.ApplyTo(sPDAListItem);
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(dat.PageLink);
					});
					if ((bool)dat.InsertImage)
					{
						sPDAListItem.InsertImage.sprite = dat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
				}
				return;
			}
		}
		ConstructedByKitsContents.SetVisible(isVisble: false);
	}

	public void PopulateProducedThings(StationpediaPage page)
	{
		if (page.ProducedThingsInserts.Count > 0)
		{
			ProducedThingsContents.SetVisible(isVisble: true);
			{
				foreach (StationCategoryInsert dat in page.ProducedThingsInserts)
				{
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, ProducedThingsContents.Contents);
					dat.ApplyTo(sPDAListItem);
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(dat.PageLink);
					});
					if (dat is StationCategoryInsertSpecial)
					{
						sPDAListItem.InsertImage.sprite = Stationpedia.Instance.ImportantSearchImage;
						sPDAListItem.SetSpecial();
					}
					else if ((bool)dat.InsertImage)
					{
						sPDAListItem.InsertImage.sprite = dat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
				}
				return;
			}
		}
		ProducedThingsContents.SetVisible(isVisble: false);
	}

	public void PopulateKitInserts(StationpediaPage page)
	{
		if (page.ConstructedByKits.Count > 0)
		{
			ConstructedThingsContents.SetVisible(isVisble: true);
			{
				foreach (StationCategoryInsert dat in page.ConstructedByKits)
				{
					SPDAListItem sPDAListItem = Object.Instantiate(Stationpedia.Instance.ListInsertPrefab, ConstructedThingsContents.Contents);
					dat.ApplyTo(sPDAListItem);
					sPDAListItem.InsertsButton.onClick.AddListener(delegate
					{
						Stationpedia.Instance.OpenPageByKey(dat.PageLink);
					});
					if ((bool)dat.InsertImage)
					{
						sPDAListItem.InsertImage.sprite = dat.InsertImage;
					}
					else
					{
						sPDAListItem.InsertImage.gameObject.SetActive(value: false);
					}
				}
				return;
			}
		}
		ConstructedThingsContents.SetVisible(isVisble: false);
	}

	public void ChangeDisplay(StationpediaPage page)
	{
		foreach (StationpediaCategory createdCategory in CreatedCategories)
		{
			Object.Destroy(createdCategory.gameObject);
		}
		Thing thing = Prefab.Find(page.PrefabHash);
		if ((bool)thing && (bool)thing.GetThumbnail())
		{
			PageHeaderImage.gameObject.SetActive(value: true);
			PageHeaderImage.sprite = thing.GetThumbnail();
		}
		else if (page.CustomSpriteToUse != null)
		{
			PageHeaderImage.gameObject.SetActive(value: true);
			PageHeaderImage.sprite = page.CustomSpriteToUse;
		}
		else
		{
			PageHeaderImage.gameObject.SetActive(value: false);
		}
		CreatedCategories.Clear();
		ResetUniversalPageInserts();
		CheckAndSetTextElement(PageDescription, page.Description);
		CheckAndSetTextElement(PaintableText, page.PaintableText);
		CheckAndSetTextElement(PageTitle, page.Title, "", trim: true);
		CheckAndSetTextElement(ConstructWithText, page.ConstructWithText);
		CheckAndSetTextElement(PrefabNameText, page.PrefabName, "Clipboard");
		CheckAndSetTextElement(PrefabHashText, page.PrefabHash.ToString(), "Clipboard");
		CheckAndSetTextElement(StackSizeInfo, page.StackSizeText);
		CheckAndSetTextElement(UnitText, page.UnitText);
		CheckAndSetTextElement(ReagentHashText, page.ReagentsHash.ToString(), "Clipboard");
		CheckAndSetTextElement(ReagentTypeText, page.ReagentsType, "Clipboard");
		CheckAndSetTextElement(ReagentsText, page.ReagentsText);
		CheckAndSetTextElement(CableBreakPointText, page.CableBreakText);
		CheckAndSetTextElement(InternalAtmosphereText, page.InternalAtmosInfoText);
		CheckAndSetTextElement(PressureBreakPipeText, page.PressureBreakText);
		CheckAndSetTextElement(SpecificHeat, page.SpecificHeatText);
		CheckAndSetTextElement(FreezeTemperature, page.FreezeTemperatureText);
		CheckAndSetTextElement(BoilingTemperature, page.BoilingTemperatureText);
		CheckAndSetTextElement(LatentHeat, page.LatentHeatText);
		CheckAndSetTextElement(MolesPerLitre, page.MolesPerLitreText);
		CheckAndSetTextElement(MolesPerLitreInWorld, page.MolesPerLitreInWorldText);
		CheckAndSetTextElement(MaxLiquidTemperature, page.MaxLiquidTemperatureText);
		CheckAndSetTextElement(MinLiquidPressure, page.MinLiquidPressure);
		CheckAndSetTextElement(FlashPointText, page.FlashpointText);
		CheckAndSetTextElement(AutoIgniteText, page.AutoIgnitionText);
		CheckAndSetTextElement(HeatTransferConvectionText, page.ConvectionFactorText);
		CheckAndSetTextElement(HeatTransferRadiationText, page.RadiationFactorText);
		CheckAndSetTextElement(SolarHeatingFactorText, page.SolarHeatingFactorText);
		CheckAndSetTextElement(DeviceBasePower, page.BasePowerDraw);
		CheckAndSetTextElement(DevicePowerStorage, page.PowerStorage);
		CheckAndSetTextElement(DevicePowerGeneration, page.PowerGeneration);
		CheckAndSetTextElement(MaxPressure, page.MaxPressure);
		CheckAndSetTextElement(Volume, page.Volume);
		CheckAndSetTextElement(Nutrition, page.Nutrition);
		CheckAndSetTextElement(FermentationGases, page.FermentedGases);
		CheckAndSetTextElement(NutritionQuality, page.NutritionQuality);
		CheckAndSetTextElement(MoodBonus, page.MoodBonus);
		CheckAndSetTextElement(GrowthTime, page.GrowthTime);
		rocketInternalsInformation.SetRocketPanelValues(page);
		stationpediaSuitInfoPanel.SetValues(page);
		CheckAndSetTextElement(SpeedMultiplier, page.DrillHeadProperties?.SpeedMultiplier ?? string.Empty);
		CheckAndSetTextElement(ReagentYieldMultiplier, page.DrillHeadProperties?.ReagentYieldMultiplier ?? string.Empty);
		CheckAndSetTextElement(IceYieldMultiplier, page.DrillHeadProperties?.IceYieldMultiplier ?? string.Empty);
		CheckAndSetTextElement(HealthMultiplier, page.DrillHeadProperties?.HealthMultiplier ?? string.Empty);
		CheckAndSetTextElement(PowerConsumptionMultiplier, page.DrillHeadProperties?.PowerConsumptionMultiplier ?? string.Empty);
		CheckAndSetTextElement(MemorySize, page.MemorySize);
		CheckAndSetTextElement(MemoryAccess, page.MemoryAccess);
		PopulateLifeRequirements(page);
		PopulateCustomCategories(page);
		PopulateSlotInserts(page);
		PopulateStructureVersion(page);
		PopulateLogicInserts(page);
		PopulateLogicBindings(page);
		PopulateLogicInstructions(page);
		PopulateLogicSlotInserts(page);
		PopulateModeInserts(page);
		PopulateConnectionInserts(page);
		PopulateOreInserts(page);
		PopulateGasInserts(page);
		PopulateFermentationInserts(page);
		PopulateConstructedThings(page);
		PopulateUsedResources(page);
		PopulateUsedIn(page);
		PopulateCombustionInfo(page);
		PopulateProducedThings(page);
		PopulateKitInserts(page);
		PopulateHowToBuildInserts(page);
		PopulateBuildStatesInserts(page);
		PopulatePhaseDiagram(page);
		bool active = !string.IsNullOrEmpty(FlashPointText.text) || !string.IsNullOrEmpty(AutoIgniteText.text) || !string.IsNullOrEmpty(HeatTransferConvectionText.text) || !string.IsNullOrEmpty(PressureBreakPipeText.text) || !string.IsNullOrEmpty(HeatTransferRadiationText.text);
		ThermalLimitsPanel.SetActive(active);
		bool active2 = page.StationSuitInfo != null;
		stationpediaSuitInfoPanel.SetActive(active2);
		bool flag = !string.IsNullOrEmpty(ReagentHashText.text) || !string.IsNullOrEmpty(UnitText.text) || !string.IsNullOrEmpty(ReagentsText.text);
		ReagentPanel.SetActive(flag);
		if (flag)
		{
			UnitText.gameObject.SetActive(!string.IsNullOrEmpty(UnitText.text));
			ReagentsText.gameObject.SetActive(!string.IsNullOrEmpty(ReagentsText.text));
		}
		bool flag2 = !string.IsNullOrEmpty(SpecificHeat.text) || !string.IsNullOrEmpty(FreezeTemperature.text) || !string.IsNullOrEmpty(MaxLiquidTemperature.text) || !string.IsNullOrEmpty(MinLiquidPressure.text) || !string.IsNullOrEmpty(BoilingTemperature.text) || !string.IsNullOrEmpty(MolesPerLitre.text) || !string.IsNullOrEmpty(MolesPerLitreInWorld.text);
		GasPropertiesPanel.SetActive(flag2);
		bool active3 = MoleHelper.CanEvaporate(page.GasType) || MoleHelper.CanCondense(page.GasType);
		PhaseDiagramPanel.SetActive(active3);
		if (flag2)
		{
			SpecificHeat.gameObject.SetActive(!string.IsNullOrEmpty(SpecificHeat.text));
			FreezeTemperature.gameObject.SetActive(!string.IsNullOrEmpty(FreezeTemperature.text));
			BoilingTemperature.gameObject.SetActive(!string.IsNullOrEmpty(BoilingTemperature.text));
			MaxLiquidTemperature.gameObject.SetActive(!string.IsNullOrEmpty(MaxLiquidTemperature.text));
			MinLiquidPressure.gameObject.SetActive(!string.IsNullOrEmpty(MinLiquidPressure.text));
			MolesPerLitre.gameObject.SetActive(!string.IsNullOrEmpty(MolesPerLitre.text));
			MolesPerLitreInWorld.gameObject.SetActive(!string.IsNullOrEmpty(MolesPerLitreInWorld.text));
		}
		bool active4 = !string.IsNullOrEmpty(PrefabHashText.text) || !string.IsNullOrEmpty(StackSizeInfo.text) || !string.IsNullOrEmpty(PaintableText.text);
		GeneralInfoPanel.SetActive(active4);
		MetabolicSuspension.SetActive(thing is ILifeSuspender);
		CryogenicRegeneration.SetActive(thing is ICryogenicRegenerator);
		ConstructWithPanel.SetActive(!string.IsNullOrEmpty(ConstructWithText.text));
		DeviceInformationPanel.SetActive(!string.IsNullOrEmpty(DeviceBasePower.text));
		GasCanisterInformation.SetActive(!string.IsNullOrEmpty(MaxPressure.text));
		AtmopshericsInformation.SetActive(!string.IsNullOrEmpty(Volume.text));
		NutritionInformation.SetActive(!string.IsNullOrEmpty(Nutrition.text));
		GrowthTimeInformation.SetActive(!string.IsNullOrEmpty(GrowthTime.text));
		rocketInternalsInformation.gameObject.SetActive(rocketInternalsInformation.Show(page));
		bool active5 = !string.IsNullOrEmpty(SpeedMultiplier.text);
		DrillHeadInformation.SetActive(active5);
	}
}
