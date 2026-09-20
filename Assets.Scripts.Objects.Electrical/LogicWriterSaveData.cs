using System.Xml.Serialization;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicWriterSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long CurrentOutputId;

	[XmlElement]
	public long CurrentInputId;

	[XmlElement]
	public LogicType LogicType;

	[XmlElement]
	public long LastInputId;

	[XmlElement]
	public long LastOutputId;

	[XmlElement]
	public double LastSetting;
}
