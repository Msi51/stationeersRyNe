using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Items;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public abstract class ThingSpawnData : IChecksum
{
	private const string PREFAB_ID_ATTRIBUTE = "Id";

	[XmlAttribute("Id")]
	public string PrefabId;

	private const string COLOR_ELEMENT = "Color";

	[XmlElement("Color")]
	public ColorSwatchReference colorSwatch;

	private const string NAME_ELEMENT = "Name";

	[XmlElement("Name")]
	public LocalizedStringReference Name;

	private const string SPAWN_POSITION_ELEMENT = "SpawnPosition";

	[XmlElement("SpawnPosition")]
	public SpawnPositionData SpawnPositionData;

	private const string HIDE_IN_START_SCREEN_ATTRIBUTE = "HideInStartScreen";

	[XmlAttribute("HideInStartScreen")]
	public bool HideInStartScreen;

	private const string EXPAND_IN_START_SCREEN_ATTRIBUTE = "ExpandInStartScreen";

	[XmlAttribute("ExpandInStartScreen")]
	public bool ExpandInStartScreen;

	private const string SHOW_DEEP_START_SCREEN_TOOLTIP_ATTRIBUTE = "ShowDeepStartScreenTooltip";

	[XmlAttribute("ShowDeepStartScreenTooltip")]
	public bool ShowDeepStartScreenTooltip;

	private const string START_SCREEN_HEADER_ATTRIBUTE = "StartScreenHeader";

	[XmlAttribute("StartScreenHeader")]
	public string StartScreenHeader;

	[XmlElement("Logic", typeof(LogicValueAction))]
	[XmlElement("BuildState", typeof(BuildStateAction))]
	[XmlElement("Interaction", typeof(InteractionAction))]
	[XmlElement("MovePlayer", typeof(MovePlayerAction))]
	[XmlElement("Gene", typeof(GeneAction))]
	[XmlElement("Quantity", typeof(QuantityAction))]
	[XmlElement("Percent", typeof(PercentAction))]
	[XmlElement("Reagents", typeof(ReagentAction))]
	[XmlElement("Charge", typeof(ChargeAction))]
	[XmlElement("Gas", typeof(GasAction))]
	[XmlElement("SourceCode", typeof(SourceCodeAction))]
	public List<ActionData> Actions = new List<ActionData>();

	[XmlElement("Species", typeof(SpeciesCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	private const string ITEM_ELEMENT = "Item";

	[XmlElement("Item")]
	public List<DynamicSpawnData> Items = new List<DynamicSpawnData>();

	private const string DYNAMIC_THING_ELEMENT = "DynamicThing";

	[XmlElement("DynamicThing")]
	public List<DynamicSpawnData> DynamicThings = new List<DynamicSpawnData>();

	private const string SPAWN_ELEMENT = "Spawn";

	[XmlElement("Spawn")]
	public List<SpawnData> Spawns = new List<SpawnData>();

	public static readonly HashSet<long> PreSpawnedThingIds = new HashSet<long>();

	private const int SHOW_ALL_CHILDREN = 999;

	[XmlIgnore]
	public Thing Prefab { get; set; }

	public string DisplayName
	{
		get
		{
			object obj;
			if (Name == null)
			{
				obj = Prefab?.ToTooltip();
				if (obj == null)
				{
					return string.Empty;
				}
			}
			else
			{
				obj = $"<color=green>{Name}</color>";
			}
			return (string)obj;
		}
	}

	public ThingSpawnData()
	{
	}

	public ThingSpawnData(Thing thing)
	{
		HideInStartScreen = true;
		PrefabId = thing.PrefabName;
		if (thing.CustomColor != null && !string.IsNullOrEmpty(thing.CustomColor.Name))
		{
			colorSwatch = new ColorSwatchReference(thing.CustomColor);
		}
		if (!string.IsNullOrEmpty(thing.CustomName))
		{
			Name = new LocalizedStringReference(thing.CustomName);
		}
		foreach (Interactable interactable in thing.Interactables)
		{
			if (interactable.JoinInProgressSync)
			{
				Actions.Add(new InteractionAction(interactable));
			}
		}
		if (thing is IQuantity quantity)
		{
			Actions.Add(new QuantityAction
			{
				Value = quantity.GetQuantity
			});
		}
		ReagentMixture reagentMixture = thing.ReagentMixture;
		if (reagentMixture != null && reagentMixture.TotalReagents > 0.0)
		{
			Actions.Add(new ReagentAction(thing.ReagentMixture));
		}
		if (thing is IChargable chargable)
		{
			Actions.Add(new ChargeAction
			{
				State = IChargable.GetState(chargable)
			});
		}
		if (thing is ILogicable logicable)
		{
			if (logicable.CanLogicRead(LogicType.Setting) && logicable.CanLogicWrite(LogicType.Setting))
			{
				Actions.Add(new LogicValueAction(LogicType.Setting, logicable.GetLogicValue(LogicType.Setting)));
			}
			if (logicable.CanLogicRead(LogicType.SettingInput) && logicable.CanLogicWrite(LogicType.SettingInput))
			{
				Actions.Add(new LogicValueAction(LogicType.SettingInput, logicable.GetLogicValue(LogicType.SettingInput)));
			}
			if (logicable.CanLogicRead(LogicType.SettingOutput) && logicable.CanLogicWrite(LogicType.SettingOutput))
			{
				Actions.Add(new LogicValueAction(LogicType.SettingOutput, logicable.GetLogicValue(LogicType.SettingOutput)));
			}
		}
		if (thing.InternalAtmosphere != null && thing.InternalAtmosphere.TotalMoles > MoleQuantity.Zero)
		{
			GasMixture.CreateGasActions(ref Actions, thing.InternalAtmosphere.GasMixture);
		}
		foreach (Slot slot in thing.Slots)
		{
			if (!slot.IsEmpty() && !slot.Contains<Human>())
			{
				if (slot.Contains<Item>())
				{
					Item dynamicThing = slot.Get<Item>();
					Items.Add(new DynamicSpawnData(dynamicThing));
				}
				else if (slot.Contains<DynamicThing>())
				{
					DynamicThing dynamicThing2 = slot.Get<DynamicThing>();
					DynamicThings.Add(new DynamicSpawnData(dynamicThing2));
				}
			}
		}
	}

	public virtual XElement Add(ref XElement parent, string elementName)
	{
		if (string.IsNullOrEmpty(PrefabId))
		{
			return null;
		}
		XElement parent2 = XDocumentHelper.MakeElement(elementName, ref parent);
		XDocumentHelper.SetAttribute(parent2, "Id", PrefabId);
		colorSwatch?.Add(ref parent2, "Color");
		Name?.Add(ref parent2, "Name");
		SpawnPositionData?.Add(ref parent2, "SpawnPosition");
		if (HideInStartScreen)
		{
			XDocumentHelper.SetAttribute(parent2, "HideInStartScreen", HideInStartScreen.ToString().ToLower());
		}
		if (ExpandInStartScreen)
		{
			XDocumentHelper.SetAttribute(parent2, "ExpandInStartScreen", ExpandInStartScreen.ToString().ToLower());
		}
		if (ShowDeepStartScreenTooltip)
		{
			XDocumentHelper.SetAttribute(parent2, "ShowDeepStartScreenTooltip", ShowDeepStartScreenTooltip.ToString().ToLower());
		}
		if (!string.IsNullOrEmpty(StartScreenHeader))
		{
			XDocumentHelper.SetAttribute(parent2, "StartScreenHeader", StartScreenHeader);
		}
		foreach (ActionData action in Actions)
		{
			action?.Add(ref parent2, action.XElementName);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Add(ref parent2, "Item");
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Add(ref parent2, "DynamicThing");
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Add(ref parent2, "Spawn");
		}
		return parent2;
	}

	public virtual int GetChecksum()
	{
		int num = (SpawnPositionData?.GetChecksum() ?? 0) * 41;
		num = (num ^ Actions.Count) * 41;
		foreach (ActionData action in Actions)
		{
			num = (num ^ action.GetChecksum()) * 41;
		}
		foreach (ConditionData condition in Conditions)
		{
			num = (num ^ condition.GetChecksum()) * 41;
		}
		foreach (DynamicSpawnData item in Items)
		{
			num = (num ^ item.GetChecksum()) * 41;
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			num = (num ^ dynamicThing.GetChecksum()) * 41;
		}
		foreach (SpawnData spawn in Spawns)
		{
			num = (num ^ spawn.GetChecksum()) * 41;
		}
		return num;
	}

	public bool Evaluate(Thing parent = null)
	{
		if (Conditions.Count == 0)
		{
			return true;
		}
		foreach (ConditionData condition in Conditions)
		{
			if (!condition.Evaluate(parent))
			{
				return false;
			}
		}
		return true;
	}

	public virtual void Initialize(ModAbout mod)
	{
		Prefab = Assets.Scripts.Objects.Prefab.Find<Thing>(PrefabId);
		if ((object)Prefab == null && WorldManager.Instance != null)
		{
			ConsoleWindow.PrintError("Spawn Prefab " + PrefabId + " is not a valid prefab.");
		}
		colorSwatch?.Initialize();
		foreach (ActionData action in Actions)
		{
			action.Initialize();
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Initialize(mod);
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Initialize(mod);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Initialize(mod);
		}
	}

	public void ToDescription(StringBuilder sb)
	{
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			sb.AppendLine("<color=green>" + dynamicThing.DisplayName + "</color>");
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.ToDescription(sb);
		}
	}

	public int GetCount()
	{
		int num = 1;
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			num += dynamicThing.GetCount();
		}
		foreach (DynamicSpawnData item in Items)
		{
			num += item.GetCount();
		}
		foreach (SpawnData spawn in Spawns)
		{
			num += spawn.GetCount();
		}
		return num;
	}

	public static void ClearAll()
	{
		PreSpawnedThingIds.Clear();
	}

	public virtual Slot GetSlotIn(Thing parent)
	{
		return null;
	}

	public virtual bool Execute(Thing parent, Human player)
	{
		if (!Evaluate())
		{
			return false;
		}
		Thing thing = null;
		if ((object)Prefab == null)
		{
			ConsoleWindow.PrintError("error executing item data as prefab '" + PrefabId + "' was not found");
			return false;
		}
		if ((object)parent != null)
		{
			Slot slotIn = GetSlotIn(parent);
			if (slotIn == null)
			{
				ConsoleWindow.PrintError("error slot for spawning '" + PrefabId + "' in '" + parent.DisplayName + "' was not found");
				return false;
			}
			thing = ((slotIn.Location == null) ? Thing.Create<DynamicThing>(Prefab, parent.Transform.position, Quaternion.identity, 0L) : Thing.Create<DynamicThing>(Prefab, slotIn.Location.position, slotIn.Location.rotation, 0L));
			OnServer.MoveToSlot((DynamicThing)thing, slotIn);
		}
		else
		{
			StartLocationData startLocationData = player?.StartLocation ?? WorldSetting.Current.StartLocationData;
			SpawnPositionRule spawnPositionRule = SpawnPositionData?.SpawnPositionRule ?? SpawnPositionRule.Random;
			Vector3 vector = Vector3.zero;
			if (SpawnPositionData?.Offset != null)
			{
				vector = SpawnPositionData.Offset.ToVector3();
			}
			Quaternion worldRotation = Quaternion.identity;
			if (SpawnPositionData?.Rotation != null)
			{
				worldRotation = SpawnPositionData.Rotation.ToQuaternion();
			}
			float spawnRadius = startLocationData.GetSpawnRadius();
			Vector3 position;
			switch (spawnPositionRule)
			{
			case SpawnPositionRule.Radial:
			{
				float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
				position = new Vector3(spawnRadius * Mathf.Cos(f), 0f, spawnRadius * Mathf.Sin(f));
				position += UnityEngine.Random.insideUnitSphere;
				position += startLocationData.WorldPosition();
				worldRotation = Quaternion.LookRotation(-position, Vector3.up);
				position = SpawnPoint.GetSafePoint(position, vector, avoidStructure: true);
				break;
			}
			case SpawnPositionRule.Random:
				position = UnityEngine.Random.insideUnitSphere * spawnRadius;
				position += startLocationData.WorldPosition();
				worldRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				position = SpawnPoint.GetSafePoint(position, vector, avoidStructure: true);
				break;
			default:
				position = vector;
				break;
			case SpawnPositionRule.None:
				position = startLocationData.WorldPosition();
				position = SpawnPoint.GetSafePoint(position, vector, avoidStructure: true);
				break;
			}
			thing = Thing.Create<Thing>(Prefab, position, worldRotation, 0L);
			thing.CachePositionOnSpawn();
		}
		if ((object)thing == null)
		{
			ConsoleWindow.PrintError("error executing item data as prefab '" + PrefabId + "' was not initialized correctly");
			return false;
		}
		PreSpawnedThingIds.Add(thing.ReferenceId);
		if (colorSwatch != null)
		{
			thing.SetCustomColor(colorSwatch);
		}
		if (Name != null)
		{
			thing.RenameThing(Name);
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Execute(thing, player);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Execute(thing, player);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Execute(thing, player);
		}
		foreach (ActionData action in Actions)
		{
			if (action is DelayedAction { Delay: not null } delayedAction)
			{
				SpawnDataHelper.AddPendingAction(delayedAction, thing, player);
			}
			else
			{
				action.Execute(thing, player);
			}
		}
		return true;
	}

	public virtual bool Execute(Thing parent, Vector3 position, Quaternion rotation)
	{
		if (!Evaluate())
		{
			return false;
		}
		Thing thing = null;
		if ((object)Prefab == null)
		{
			ConsoleWindow.PrintError("error executing item data as prefab '" + PrefabId + "' was not found");
			return false;
		}
		if ((object)parent != null)
		{
			Slot slotIn = GetSlotIn(parent);
			if (slotIn == null)
			{
				ConsoleWindow.PrintError("error slot for spawning '" + PrefabId + "' in '" + parent.DisplayName + "' was not found");
				return false;
			}
			thing = ((slotIn.Location == null) ? Thing.Create<DynamicThing>(Prefab, parent.Transform.position, Quaternion.identity, 0L) : Thing.Create<DynamicThing>(Prefab, slotIn.Location.position, slotIn.Location.rotation, 0L));
			OnServer.MoveToSlot((DynamicThing)thing, slotIn);
		}
		else
		{
			thing = Thing.Create<Thing>(Prefab, position, rotation, 0L);
		}
		if ((object)thing == null)
		{
			ConsoleWindow.PrintError("error executing item data as prefab '" + PrefabId + "' was not initialized correctly");
			return false;
		}
		if (colorSwatch != null)
		{
			thing.SetCustomColor(colorSwatch);
		}
		if (Name != null)
		{
			thing.RenameThing(Name);
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Execute(thing, null);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Execute(thing, null);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Execute(thing, null);
		}
		foreach (ActionData action in Actions)
		{
			if (action is DelayedAction { Delay: not null } delayedAction)
			{
				SpawnDataHelper.AddPendingAction(delayedAction, thing, null);
			}
			else
			{
				action.Execute(thing, null);
			}
		}
		return true;
	}

	public void TooltipName()
	{
	}

	public void ToolTip(StringBuilder stringBuilder, int generations, int maxDepth = 999, bool includeName = true)
	{
		maxDepth--;
		if (maxDepth < 0)
		{
			return;
		}
		float num = float.NaN;
		foreach (ActionData action in Actions)
		{
			if (action is QuantityAction quantityAction)
			{
				num = quantityAction.Value;
				break;
			}
		}
		if (float.IsNaN(num) && Items.Count == 0 && Actions.Count == 0 && DynamicThings.Count == 0)
		{
			if ((generations != 0 || Name != null) && (object)Prefab != null)
			{
				for (int i = 0; i < generations; i++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(Prefab.ToTooltip());
			}
			return;
		}
		bool flag;
		if (Prefab is Item item && this is DynamicSpawnData dynamicSpawnData)
		{
			for (int j = 0; j < generations; j++)
			{
				stringBuilder.Append("    ");
			}
			if (!string.IsNullOrEmpty(dynamicSpawnData.SlotId))
			{
				stringBuilder.AppendLine(GameStrings.ItemInSlot.AsString(item.ToTooltip(), dynamicSpawnData.SlotId.AsColor("yellow")));
			}
			else if (dynamicSpawnData.SlotIndex >= 0)
			{
				stringBuilder.AppendLine(GameStrings.ItemInSlot.AsString(item.ToTooltip(), dynamicSpawnData.SlotIndex.ToString().AsColor("yellow")));
			}
			if (!float.IsNaN(num))
			{
				if (item is Stackable stackable)
				{
					stringBuilder.AppendLine(GameStrings.ItemInSlotStack.AsString(stackable.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
				else if (item is Consumable consumable)
				{
					stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(consumable.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
				else
				{
					stringBuilder.AppendLine(GameStrings.TradeItemStackChild.AsString(item.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
			}
			else
			{
				string customName = item.CustomName;
				if (!string.IsNullOrWhiteSpace(Name))
				{
					item.CustomName = Name;
				}
				if (includeName)
				{
					stringBuilder.AppendLine((maxDepth > 0) ? GameStrings.TradeItemParent.AsString(item.ToTooltip()) : item.ToTooltip());
				}
				item.CustomName = customName;
			}
			generations++;
			if (item is Ore)
			{
				if (!(item is Slag))
				{
					goto IL_028b;
				}
			}
			else if (item is Ingot)
			{
				goto IL_028b;
			}
			flag = false;
			goto IL_0293;
		}
		goto IL_048e;
		IL_048e:
		foreach (ActionData action2 in Actions)
		{
			if (!(action2 is QuantityAction))
			{
				action2.ToolTip(stringBuilder, generations, Prefab);
			}
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.ToolTip(stringBuilder, generations, maxDepth);
		}
		foreach (DynamicSpawnData item2 in Items)
		{
			item2.ToolTip(stringBuilder, generations, maxDepth);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Tooltip(stringBuilder, generations, maxDepth);
		}
		return;
		IL_028b:
		flag = true;
		goto IL_0293;
		IL_0293:
		if (flag)
		{
			string value = (float.IsNaN(num) ? item.CreatedReagentMixture.ToString() : item.CreatedReagentMixture.ToString(num));
			Ice ice = item as Ice;
			if (((object)ice != null && ice.SpawnContents.Count > 0) || !string.IsNullOrEmpty(value))
			{
				for (int k = 0; k < generations; k++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemContains.AsString(item.ToTooltip()).AsColor("white"));
			}
			if (!string.IsNullOrEmpty(value))
			{
				stringBuilder.Append(value);
			}
			if ((object)ice != null && ice.SpawnContents.Count > 0)
			{
				foreach (SpawnGas spawnContent in ice.SpawnContents)
				{
					for (int l = 0; l < generations; l++)
					{
						stringBuilder.Append("    ");
					}
					AtmosphericsManager.DisplayGas(stringBuilder, spawnContent, float.IsNaN(num) ? 1f : num);
				}
			}
		}
		else if (item is IMicrowaveIngredient microwaveIngredient)
		{
			string value2 = item.CreatedReagentMixture.ToString(item.GetQuantity / microwaveIngredient.QuantityPerUse);
			if (!string.IsNullOrEmpty(value2))
			{
				stringBuilder.Append(value2);
			}
			else if (!string.IsNullOrEmpty(item.GetQuantityText()))
			{
				for (int m = 0; m < generations; m++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemQuantity.AsString(item.GetQuantityText().AsColor("yellow")));
			}
		}
		if (item is INutrition nutrition)
		{
			float nutritionalValue = nutrition.GetNutritionalValue();
			if (nutritionalValue > 0f)
			{
				for (int n = 0; n < generations; n++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemNutrition.AsString(nutritionalValue.ToStringRounded("yellow")));
			}
		}
		goto IL_048e;
	}

	public string GetTooltipName()
	{
		return DisplayName;
	}
}
