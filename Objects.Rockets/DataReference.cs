using System.Xml.Serialization;

namespace Objects.Rockets;

public class DataReference
{
	[XmlAttribute("Id")]
	public string Id;
}
