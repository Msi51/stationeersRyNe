using System.Xml.Serialization;

[XmlRoot("TidalLocking")]
public class TidalLocking : CelestialRotation
{
	public override double GetSpeed(CelestialBodyTemplate reference)
	{
		return 360.0 / reference.OrbitData.Period;
	}
}
