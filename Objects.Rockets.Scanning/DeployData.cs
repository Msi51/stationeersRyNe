using System.Xml.Serialization;

namespace Objects.Rockets.Scanning;

public class DeployData : SpaceMapNodeActionData
{
	[XmlAttribute("Type")]
	public DeployType DeployType;

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new RocketDeploy(this, node);
	}
}
