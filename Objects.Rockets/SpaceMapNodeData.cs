using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Objects.Rockets.Mining;
using Objects.Rockets.Scanning;

namespace Objects.Rockets;

public class SpaceMapNodeData : DataCollection
{
	[XmlAttribute("Code")]
	public ulong Code;

	[XmlAttribute("IsCharted")]
	public bool IsCharted;

	[XmlElement("Connection")]
	public List<NodeConnectionData> Children = new List<NodeConnectionData>();

	[XmlElement("MapDisplay")]
	public MapDisplayData MapDisplay;

	[XmlElement("Deposit")]
	public MineableDepositData DepositData;

	[XmlElement("Mine", Type = typeof(SpaceMapNodeMineData))]
	[XmlElement("Survey", Type = typeof(SurveyData))]
	[XmlElement("Discover", Type = typeof(DiscoverSiteData))]
	[XmlElement("Chart", Type = typeof(ChartData))]
	[XmlElement("Deploy", Type = typeof(DeployData))]
	[XmlElement("SurfaceScan", Type = typeof(SurfaceScanData))]
	public List<SpaceMapNodeActionData> AvailableActions = new List<SpaceMapNodeActionData>();

	[XmlElement("AchievementReached")]
	public AchievementData AchievementReached;

	public override bool IsValid()
	{
		if (AvailableActions.Count <= 0 && Children.Count <= 0)
		{
			return DepositData != null;
		}
		return true;
	}

	public override void Initialize(ModAbout mod)
	{
		if (MapDisplay == null)
		{
			ConsoleWindow.PrintError("Initialise SpaceMap Error! Node " + Id + " has no MapDisplayData");
		}
		MapDisplay?.Initialise();
		SpaceMapNodeActionData.ValidateActionDatas(this);
		foreach (SpaceMapNodeActionData availableAction in AvailableActions)
		{
			availableAction.Initialize(mod);
		}
		DepositData?.Validate();
		DataCollection.Register(this, mod);
	}
}
