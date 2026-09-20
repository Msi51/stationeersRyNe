using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicReagentReaderSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long CurrentDeviceId;

	[XmlElement]
	public int ReagentHash;

	[XmlElement]
	public int ModeIndex;
}
