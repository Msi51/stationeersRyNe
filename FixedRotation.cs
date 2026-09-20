using System.Xml.Serialization;

[XmlRoot("FixedRotation")]
public class FixedRotation : CelestialRotation
{
	[XmlAttribute]
	public double Speed = double.NaN;

	public override double GetSpeed(CelestialBodyTemplate reference)
	{
		if (!double.IsNaN(Speed))
		{
			return Speed;
		}
		return 0.0;
	}
}
