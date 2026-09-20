using System.Xml.Serialization;

namespace ThingImport;

public class PerennialData
{
	[XmlAttribute("RevertToStage")]
	public int RevertToStage;
}
