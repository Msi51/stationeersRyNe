using System.Xml.Serialization;

namespace WorldLogSystem;

public abstract class WorldEventData
{
	[XmlAttribute("Text")]
	public string Text;

	[XmlAttribute("DateTime")]
	public string DateTime;

	public abstract WorldEvent ToEvent();
}
