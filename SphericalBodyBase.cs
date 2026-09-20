using System.Xml.Serialization;
using ThingImport;

public abstract class SphericalBodyBase : CelestialBodyReference
{
	[XmlElement]
	public ColorRGB Color;

	[XmlElement("Texture")]
	public TextureReference TextureRef;

	[XmlAttribute]
	public float RadiusKm = float.NaN;

	[XmlAttribute]
	public double RadiusAu = double.NaN;

	[XmlAttribute]
	public double Scale = 1.0;

	[XmlAttribute]
	public bool CanOccult = true;

	[XmlElement("Rotation")]
	public Vector3Reference Rotation;

	public float GetRadius()
	{
		if (!float.IsNaN(RadiusKm))
		{
			return (float)((double)RadiusKm * 6.6845871222684464E-09 * (double)OrbitalSimulation.SkyboxScale * 10000.0 * Scale);
		}
		if (!double.IsNaN(RadiusAu))
		{
			return (float)(RadiusAu * (double)OrbitalSimulation.SkyboxScale * 10000.0 * Scale);
		}
		return 1f;
	}

	public override void LoadData()
	{
		base.LoadData();
		TextureRef?.Load();
	}
}
