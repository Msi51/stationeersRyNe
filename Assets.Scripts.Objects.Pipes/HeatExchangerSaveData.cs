using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(HeatExchangerSaveData))]
public class HeatExchangerSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public AtmosphereSaveData atmos2;

	[XmlElement]
	public AtmosphereSaveData atmos3;
}
