using System.Xml.Serialization;

[XmlRoot("CelestialBody")]
public class CelestialBodyReference : CelestialReference
{
	[XmlAttribute]
	public string Parent;

	[XmlElement("FixedRotation", typeof(FixedRotation))]
	[XmlElement("SynodicRotation", typeof(SynodicRotation))]
	[XmlElement("TidalLocking", typeof(TidalLocking))]
	public CelestialRotation Axis;

	public virtual void Create(CelestialBody body, CelestialBodyTemplate template)
	{
	}
}
