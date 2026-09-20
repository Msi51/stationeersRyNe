using System.Xml.Serialization;

namespace Assets.Scripts;

public class LavaLakeProp
{
	[XmlElement("Position")]
	public Vector3Reference Position = new Vector3Reference();

	[XmlElement("Rotation")]
	public Vector3Reference Rotation = new Vector3Reference();

	[XmlElement("Scale")]
	public Vector3Reference Scale = new Vector3Reference();
}
