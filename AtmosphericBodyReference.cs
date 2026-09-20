using System.Xml.Serialization;
using ThingImport;

[XmlRoot("AtmosphericBody")]
public class AtmosphericBodyReference : SphericalBodyBase
{
	[XmlElement("Fresnel")]
	public FresnelReference Fresnel;

	[XmlElement("Normal")]
	public TextureReference NormalRef;

	[XmlElement("Specular")]
	public TextureReference SpecularRef;

	[XmlElement("Cloud")]
	public CloudTextureReference CloudRef;

	[XmlElement("Ring")]
	public RingReference PlanetaryRing;

	[XmlElement("Material")]
	public MaterialReference MaterialRef;

	public override void LoadData()
	{
		base.LoadData();
		CloudRef?.Load();
		NormalRef?.Load();
		SpecularRef?.Load();
	}

	public override void Create(CelestialBody body, CelestialBodyTemplate template)
	{
		AtmosphericBody.AssignFromPool(body, template, this);
	}

	public float GetCloudSpeed()
	{
		return CloudRef?.Speed ?? 0f;
	}
}
