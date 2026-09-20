using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Motherboards;

[XmlRoot]
public class AirControlVent
{
	[XmlElement]
	public long ReferenceId;

	[XmlIgnore]
	public IPoweredVent PoweredVent;

	[XmlElement]
	public VentDirection Direction;
}
