using System.Xml.Serialization;
using Assets.Scripts;

namespace Objects.Rockets;

public class NodeConnectionData
{
	[XmlAttribute("Id")]
	public string ConnectedNodeId;

	[XmlAttribute("DistanceMultiplier")]
	public float DistanceMultiplier = 1f;

	[XmlAttribute("Difficulty")]
	public int ChartDifficulty = 100;

	public SpaceMapNodeData ConnectedNodeData => DataCollection.Get<SpaceMapNodeData>(ConnectedNodeId);
}
