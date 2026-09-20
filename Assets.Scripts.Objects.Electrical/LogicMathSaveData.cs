using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicMathSaveData))]
public class LogicMathSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long Input1;

	[XmlElement]
	public long Input2;

	[XmlElement]
	public long Input3;
}
