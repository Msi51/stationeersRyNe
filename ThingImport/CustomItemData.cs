using System.Xml.Serialization;

namespace ThingImport;

public abstract class CustomItemData : CustomThingData
{
	[XmlElement("Reagents")]
	public ReagentData ReagentData;
}
