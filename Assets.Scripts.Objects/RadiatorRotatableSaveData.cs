using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(RadiatorRotatableSaveData))]
public class RadiatorRotatableSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public double Horizontal;

	[XmlElement]
	public double Vertical;

	[XmlElement]
	public double TargetHorizontal;

	[XmlElement]
	public double TargetVertical;
}
