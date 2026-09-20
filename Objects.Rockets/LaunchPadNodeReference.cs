using System.Xml.Serialization;

namespace Objects.Rockets;

public class LaunchPadNodeReference : MapNodeReference
{
	[XmlAttribute("IsOrbital")]
	public bool IsOrbital;

	public LaunchPadNodeReference()
	{
	}

	public LaunchPadNodeReference(SpaceMapNode spaceMapNode)
		: base(spaceMapNode)
	{
		IsOrbital = spaceMapNode.NodeType == NodeType.LowOrbitLaunchPad;
	}

	public override void Deserialize()
	{
		base.Deserialize();
		SpaceMapNode.Create(this);
	}
}
