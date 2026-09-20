using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlRoot]
public enum VentDirection
{
	Inward = 1,
	Outward = 0
}
