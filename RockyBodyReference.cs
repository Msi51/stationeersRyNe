using System.Xml.Serialization;
using ThingImport;

[XmlRoot("RockyBody")]
public class RockyBodyReference : SphericalBodyBase
{
	[XmlElement("Normal")]
	public TextureReference NormalRef;

	[XmlElement("Mesh")]
	public MeshReference MeshRef;

	public FloatReference Emissive;

	public override void LoadData()
	{
		base.LoadData();
		NormalRef?.Load();
	}

	public override void Create(CelestialBody body, CelestialBodyTemplate template)
	{
		RockyBody.AssignFromPool(body, template, this);
	}
}
