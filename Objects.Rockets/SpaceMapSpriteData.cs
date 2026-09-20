using System.Xml.Serialization;
using Assets.Scripts;

namespace Objects.Rockets;

public class SpaceMapSpriteData : DataCollection
{
	[XmlElement("MapDisplay")]
	public MapDisplayData MapDisplay;

	public override void Initialize(ModAbout mod)
	{
		MapDisplay?.Initialise();
	}
}
