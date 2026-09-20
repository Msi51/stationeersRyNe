using System.Xml.Serialization;

[XmlRoot]
public class RotatingCelestialBodySaveData : CelestialBodySaveData
{
	[XmlElement("Axis")]
	public DoubleReference AxisAngle;
}
