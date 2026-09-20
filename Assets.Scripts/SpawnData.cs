using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class SpawnData : DataCollection, ICreativeSpawnable
{
	private const string EVENT_ATTRIBUTE = "Event";

	[XmlAttribute("Event")]
	public SpawnEvent EventType;

	private const string HIDE_IN_START_SCREEN_ATTRIBUTE = "HideInStartScreen";

	[XmlAttribute("HideInStartScreen")]
	public bool HideInStartScreen;

	private const string SHOW_IN_SPAWN_MENU = "ShowInSpawnMenu";

	[XmlAttribute("ShowInSpawnMenu")]
	public bool ShowInSpawnMenu;

	private const string START_SCREEN_HEADER_ATTRIBUTE = "StartScreenHeader";

	[XmlAttribute("StartScreenHeader")]
	public string StartScreenHeader;

	[XmlElement("Species", typeof(SpeciesCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	[XmlElement("SurvivalProperty", typeof(SurvivalPropertyAction))]
	public List<ActionData> Actions = new List<ActionData>();

	private const string ITEM_ELEMENT = "Item";

	[XmlElement("Item")]
	public List<DynamicSpawnData> Items = new List<DynamicSpawnData>();

	private const string DYNAMIC_THING_ELEMENT = "DynamicThing";

	[XmlElement("DynamicThing")]
	public List<DynamicSpawnData> DynamicThings = new List<DynamicSpawnData>();

	private const string STRUCTURE_ELEMENT = "Structure";

	[XmlElement("Structure")]
	public List<StructureSpawnData> Structures = new List<StructureSpawnData>();

	private const string WORLD_ATMOSPHERE_ELEMENT = "WorldAtmosphere";

	[XmlElement("WorldAtmosphere")]
	public List<WorldAtmosphereSpawnData> WorldAtmospheres = new List<WorldAtmosphereSpawnData>();

	private const string SPAWN_ELEMENT = "Spawn";

	[XmlElement("Spawn")]
	public List<SpawnData> Spawns = new List<SpawnData>();

	[XmlElement("PositionList")]
	public List<SpawnPositionListData> SpawnPositionListData = new List<SpawnPositionListData>();

	private const int SHOW_ALL_CHILDREN = 999;

	public int SpawnId => base.IdHash;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(Name))
			{
				return Name;
			}
			if (DynamicThings.Count > 0 && !string.IsNullOrEmpty(DynamicThings[0].Name))
			{
				return DynamicThings[0].Name;
			}
			if (Items.Count > 0 && !string.IsNullOrEmpty(Items[0].Name))
			{
				return Items[0].Name;
			}
			return Id;
		}
	}

	public string SpawnableName => Id;

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ Items.Count) * 41;
		foreach (DynamicSpawnData item in Items)
		{
			checksum = (checksum ^ item.GetChecksum()) * 41;
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			checksum = (checksum ^ dynamicThing.GetChecksum()) * 41;
		}
		foreach (SpawnData spawn in Spawns)
		{
			checksum = (checksum ^ spawn.GetChecksum()) * 41;
		}
		foreach (StructureSpawnData structure in Structures)
		{
			checksum = (checksum ^ structure.GetChecksum()) * 41;
		}
		foreach (WorldAtmosphereSpawnData worldAtmosphere in WorldAtmospheres)
		{
			checksum = (checksum ^ worldAtmosphere.GetChecksum()) * 41;
		}
		return checksum;
	}

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
		foreach (ActionData action in Actions)
		{
			action.Initialize();
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
		foreach (StructureSpawnData structure in Structures)
		{
			structure.Initialize(mod);
		}
		foreach (WorldAtmosphereSpawnData worldAtmosphere in WorldAtmospheres)
		{
			worldAtmosphere.Initialize();
		}
		foreach (SpawnPositionListData spawnPositionListDatum in SpawnPositionListData)
		{
			spawnPositionListDatum.Initialise();
		}
		DataCollection.Register(this, mod);
		if (!GameManager.IsBatchMode && IsValid() && ShowInSpawnMenu)
		{
			ImguiCreativeSpawnMenu.AddDynamicItem(this);
		}
	}

	public override bool IsValid()
	{
		if (Items.Count <= 0 && Spawns.Count <= 0 && DynamicThings.Count <= 0 && Structures.Count <= 0 && WorldAtmospheres.Count <= 0)
		{
			return SpawnPositionListData.Count > 0;
		}
		return true;
	}

	public void Execute()
	{
		Execute(null, null);
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

	public void Execute(Thing parent, Vector3 position, Quaternion rotation)
	{
		if (!IsValid())
		{
			DataCollection.Get<SpawnData>(base.IdHash)?.Execute(parent, position, rotation);
			return;
		}
		foreach (ActionData action in Actions)
		{
			action.Execute(parent, null);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Execute(parent, position, rotation);
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Execute(parent, position, rotation);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Execute(parent, position, rotation);
		}
	}

	public void Execute(Thing parent, Human player)
	{
		if (!Evaluate(parent))
		{
			return;
		}
		if (!IsValid())
		{
			DataCollection.Get<SpawnData>(base.IdHash)?.Execute(parent, player);
			return;
		}
		foreach (ActionData action in Actions)
		{
			action.Execute(parent, player);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Execute(parent, player);
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Execute(parent, player);
		}
		foreach (StructureSpawnData structure in Structures)
		{
			structure.Execute(parent, player);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Execute(parent, player);
		}
		foreach (WorldAtmosphereSpawnData worldAtmosphere in WorldAtmospheres)
		{
			worldAtmosphere.Execute();
		}
		foreach (SpawnPositionListData spawnPositionListDatum in SpawnPositionListData)
		{
			spawnPositionListDatum.Execute();
		}
	}

	public void ToDescription(StringBuilder sb)
	{
		if (!IsValid())
		{
			DataCollection.Get<SpawnData>(base.IdHash).ToDescription(sb);
			return;
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			if (dynamicThing.Prefab is Lander)
			{
				dynamicThing.ToDescription(sb);
			}
			else
			{
				sb.AppendLine("<color=green>" + dynamicThing.DisplayName + "</color>");
			}
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.ToDescription(sb);
		}
	}

	public void ToTooltip(StringBuilder sb)
	{
		Tooltip(sb, 0);
	}

	public void Tooltip(StringBuilder stringBuilder, int generations, int maxDepth = 999)
	{
		maxDepth--;
		if (maxDepth < 0)
		{
			return;
		}
		if (!IsValid())
		{
			DataCollection.Get<SpawnData>(base.IdHash)?.Tooltip(stringBuilder, generations, maxDepth);
			return;
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.ToolTip(stringBuilder, generations, maxDepth);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.ToolTip(stringBuilder, generations, maxDepth);
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Tooltip(stringBuilder, generations, maxDepth);
		}
	}

	public int GetCount()
	{
		if (!IsValid())
		{
			return DataCollection.Get<SpawnData>(base.IdHash)?.GetCount() ?? 0;
		}
		int num = Items.Count;
		foreach (DynamicSpawnData item in Items)
		{
			num += item.GetCount();
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			num += dynamicThing.GetCount();
		}
		foreach (SpawnData spawn in Spawns)
		{
			num += spawn.GetCount();
		}
		return num;
	}

	public void Add(ref XElement parentElement, string elementName)
	{
		XElement parentElement2 = XDocumentHelper.MakeElement(elementName, ref parentElement);
		XDocumentHelper.SetAttribute(parentElement2, "Id", Id);
		Name?.Add(ref parentElement2, "Name");
		if (EventType != SpawnEvent.None)
		{
			XDocumentHelper.SetAttribute(parentElement2, "Event", EventType.GetXmlEnumAttributeValueFromEnum());
		}
		if (HideInStartScreen)
		{
			XDocumentHelper.SetAttribute(parentElement2, "HideInStartScreen", HideInStartScreen.ToString().ToLower());
		}
		if (!string.IsNullOrEmpty(StartScreenHeader))
		{
			XDocumentHelper.SetAttribute(parentElement2, "StartScreenHeader", StartScreenHeader);
		}
		foreach (DynamicSpawnData item in Items)
		{
			item.Add(ref parentElement2, "Item");
		}
		foreach (DynamicSpawnData dynamicThing in DynamicThings)
		{
			dynamicThing.Add(ref parentElement2, "DynamicThing");
		}
		foreach (StructureSpawnData structure in Structures)
		{
			structure.Add(ref parentElement2, "Structure");
		}
		foreach (WorldAtmosphereSpawnData worldAtmosphere in WorldAtmospheres)
		{
			worldAtmosphere.Add(ref parentElement2, "WorldAtmosphere");
		}
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Add(ref parentElement2, "Spawn");
		}
	}

	public Sprite GetThumbnail()
	{
		if (DynamicThings.Count > 0)
		{
			if (DynamicThings[0].colorSwatch == null)
			{
				return DynamicThings[0].Prefab.Thumbnail;
			}
			return DynamicThings[0].Prefab.Thumbnails[DynamicThings[0].colorSwatch.GetIndex()];
		}
		if (Items.Count > 0)
		{
			if (Items[0].colorSwatch == null)
			{
				return Items[0].Prefab.Thumbnail;
			}
			return Items[0].Prefab.Thumbnails[Items[0].colorSwatch.GetIndex()];
		}
		foreach (SpawnData spawn in Spawns)
		{
			Sprite thumbnail = spawn.GetThumbnail();
			if ((bool)thumbnail)
			{
				return thumbnail;
			}
		}
		return null;
	}
}
