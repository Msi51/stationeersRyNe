using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicBatchSlotReaderSaveData : LogicBaseSaveData
{
	[XmlElement]
	public int CurrentInputHash;

	[XmlElement]
	public int InputIndex;

	[XmlElement]
	public int SlotIndex;

	[XmlElement]
	public LogicBatchMethod BatchMethod;
}
