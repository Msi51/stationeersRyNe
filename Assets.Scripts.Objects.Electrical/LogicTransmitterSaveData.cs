using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicTransmitterSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long CurrentConnectedId;
}
