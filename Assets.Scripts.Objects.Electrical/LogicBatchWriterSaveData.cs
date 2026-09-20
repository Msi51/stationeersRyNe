using System.Xml.Serialization;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicBatchWriterSaveData : LogicBaseSaveData
{
	[XmlElement]
	public int CurrentOutputHash;

	[XmlElement]
	public long CurrentInputId;

	[XmlElement]
	public LogicType LogicType;

	[XmlElement]
	public long LastInputId;

	[XmlElement]
	public double LastSetting;
}
