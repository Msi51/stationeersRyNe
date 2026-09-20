using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;
using Trading;

public class StartConditionData : DataCollection
{
	[XmlAttribute("IsDefault")]
	public bool IsDefault;

	[XmlElement("Spawn")]
	public List<SpawnData> Spawns = new List<SpawnData>();

	[XmlIgnore]
	public ModAbout Mod;

	[XmlElement("Description")]
	public LocalizedStringReference Description;

	[XmlElement("PreviewButton")]
	public TextureReference PreviewButton;

	[XmlAttribute("IsBrutal")]
	public bool IsBrutal;

	[XmlAttribute("IsTerrainEdit")]
	public bool IsTerrainEdit;

	[XmlElement("WorldInject")]
	public WorldCollection WorldInjection;

	public static List<StartConditionData> AllStartConditions = new List<StartConditionData>();

	public override bool IsValid()
	{
		return Spawns.Count > 0;
	}

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		Mod = mod;
		PreviewButton?.Load();
		DataCollection.Register(this, mod);
		AllStartConditions.Add(this);
		foreach (SpawnData spawn in Spawns)
		{
			spawn.Initialize(mod);
		}
	}

	public static StartConditionData GetFallBack(string worldId)
	{
		return worldId switch
		{
			"Europa" => DataCollection.Get<StartConditionData>("EuropaDefault"), 
			"Vulcan" => DataCollection.Get<StartConditionData>("VulcanDefault"), 
			"Mimas" => DataCollection.Get<StartConditionData>("MimasDefault"), 
			"Venus" => DataCollection.Get<StartConditionData>("VenusDefault"), 
			_ => DataCollection.Get<StartConditionData>("Default"), 
		};
	}

	public static StartConditionData GetDefaultStartCondition(WorldSetting worldSetting)
	{
		foreach (StartConditionData startConditionData in worldSetting.Data.StartConditionDatas)
		{
			if (startConditionData.IsDefault)
			{
				return startConditionData;
			}
		}
		return null;
	}
}
