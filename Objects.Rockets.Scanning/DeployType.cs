using System.Xml.Serialization;

namespace Objects.Rockets.Scanning;

public enum DeployType
{
	[XmlEnum("None")]
	None,
	[XmlEnum("NuclearBomb")]
	NuclearBomb,
	[XmlEnum("DeliveryPayload")]
	DeliveryPayload
}
