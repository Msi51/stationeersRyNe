using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(CircuitHousingSaveData))]
public class CircuitHousingSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long[] DeviceIDs;

	[XmlElement]
	public string[] DeviceLabels;
}
