using System.Xml.Serialization;

namespace ThingImport;

public abstract class CustomThingData
{
	[XmlAttribute("Name")]
	public string Name;

	[XmlElement("Blueprint")]
	public BlueprintData BlueprintData;

	public abstract void RegisterPrefab();

	public virtual void Initialize()
	{
	}
}
