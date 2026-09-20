using System.Xml.Serialization;

[XmlRoot]
public class SPDAThingOverideData
{
	[XmlElement]
	public string ParentListKey;

	[XmlElement]
	public string ThingName;

	[XmlElement]
	public bool HideInSPDA;

	[XmlElement]
	public string CustomCategory;
}
