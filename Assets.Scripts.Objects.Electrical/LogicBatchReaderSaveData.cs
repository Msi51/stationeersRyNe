using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicBatchReaderSaveData : LogicBaseSaveData
{
	[XmlElement]
	public int CurrentOutputHash;

	[XmlElement]
	public int InputIndex;

	[XmlElement]
	public LogicBatchMethod BatchMethod;
}
