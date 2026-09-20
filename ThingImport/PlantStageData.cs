using System.Xml.Serialization;

namespace ThingImport;

public class PlantStageData
{
	[XmlElement("Mesh")]
	public MeshReference MeshRef;

	[XmlAttribute("Length")]
	public float Length;

	[XmlAttribute("Mature")]
	public bool Mature;

	[XmlAttribute("Seeding")]
	public bool Seeding;

	[XmlAttribute("Dead")]
	public bool Dead;

	public void Initialize()
	{
		MeshRef?.Load();
	}
}
