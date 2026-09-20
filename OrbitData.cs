using System.Xml.Serialization;

[XmlRoot]
public class OrbitData
{
	[XmlAttribute]
	public double Period = double.MaxValue;

	[XmlAttribute]
	public float Inclination;

	[XmlAttribute]
	public float Eccentricity = float.NaN;

	[XmlAttribute]
	public double SemiMajorAxisKm = double.NaN;

	[XmlAttribute]
	public double SemiMajorAxisAu = double.NaN;

	[XmlAttribute]
	public double ArgumentOfPeriapsis = double.NaN;

	[XmlAttribute]
	public double LongitudeOfAscendingNode = double.NaN;

	public double GetArgumentOfPeriapsis()
	{
		if (double.IsNaN(ArgumentOfPeriapsis))
		{
			return 0.0;
		}
		return ArgumentOfPeriapsis;
	}

	public double GetLongitudeOfAscendingNode()
	{
		if (double.IsNaN(LongitudeOfAscendingNode))
		{
			return 0.0;
		}
		return LongitudeOfAscendingNode;
	}

	public float GetEccentricity()
	{
		if (!float.IsNaN(Eccentricity))
		{
			return Eccentricity;
		}
		return 0f;
	}

	public double GetSemiMajorAxis()
	{
		if (!double.IsNaN(SemiMajorAxisAu))
		{
			return SemiMajorAxisAu;
		}
		if (!double.IsNaN(SemiMajorAxisKm))
		{
			return SemiMajorAxisKm * 6.6845871222684464E-09;
		}
		return 1.0;
	}
}
