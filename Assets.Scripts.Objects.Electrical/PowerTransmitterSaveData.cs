using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(WirelessPowerSaveData))]
public class PowerTransmitterSaveData : WirelessPowerSaveData
{
	[XmlElement]
	public long OutputNetworkReferenceId;
}
