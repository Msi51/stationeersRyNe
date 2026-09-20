using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;

namespace Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicStopWatchSaveData : LogicBaseSaveData
{
	[XmlElement]
	public double Offset;
}
