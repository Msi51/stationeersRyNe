using System.Xml.Serialization;

[XmlRoot("SynodicRotation")]
public class SynodicRotation : CelestialRotation
{
	[XmlAttribute]
	public double Days = double.NaN;

	public override double GetSpeed(CelestialBodyTemplate reference)
	{
		if (double.IsNaN(Days))
		{
			return 0.0;
		}
		return 360.0 / Days;
	}
}
