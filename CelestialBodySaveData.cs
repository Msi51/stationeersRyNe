using System.Xml.Serialization;

[XmlRoot]
public class CelestialBodySaveData : CelestialSaveData
{
	[XmlElement("Orbit")]
	public DoubleReference OrbitAngle;
}
