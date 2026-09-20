using System.Xml.Serialization;
using Assets.Scripts;
using Objects.Rockets.Mining;

namespace Objects.Rockets;

public class SiteNodeReference : TemplateNodeReference
{
	public IntReference SurveyPoints;

	[XmlElement("MineableDeposit")]
	public MineableDepositSaveData DepositSaveData;

	public SiteNodeReference()
	{
	}

	public SiteNodeReference(SpaceMapNode spaceMapNode)
		: base(spaceMapNode)
	{
		if (spaceMapNode.Deposit != null)
		{
			DepositSaveData = new MineableDepositSaveData(spaceMapNode.Deposit);
		}
		SurveyPoints = new IntReference(spaceMapNode.SurveyPoints);
	}

	public override void Deserialize()
	{
		base.Deserialize();
		SpaceMapNode spaceMapNode = SpaceMapNode.Create(this);
		if (spaceMapNode == null)
		{
			ConsoleWindow.PrintError("Failed To load SpaceMapNode: " + TemplateId);
			return;
		}
		spaceMapNode.Deposit.Apply(DepositSaveData);
		spaceMapNode.Deposit?.SetParent(spaceMapNode);
		IntReference surveyPoints = SurveyPoints;
		spaceMapNode.SurveyPoints = ((surveyPoints != null) ? ((int)surveyPoints) : 0);
	}
}
