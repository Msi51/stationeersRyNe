using System.Xml.Serialization;
using Assets.Scripts;

namespace Objects.Rockets;

public class MapConnectionData
{
	[XmlAttribute("MapId")]
	public string ConnectedMapId;

	[XmlAttribute("Distance")]
	public float Distance = 100f;

	[XmlAttribute("Difficulty")]
	public int Difficulty = 100;

	public SpaceMapData ConnectedMapData => DataCollection.Get<SpaceMapData>(ConnectedMapId);
}
