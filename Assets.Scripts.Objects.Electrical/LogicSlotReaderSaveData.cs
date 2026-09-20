using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicSlotReaderSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long CurrentDeviceId;

	[XmlElement]
	public int InputIndex;

	[XmlElement]
	public int SlotIndex;
}
