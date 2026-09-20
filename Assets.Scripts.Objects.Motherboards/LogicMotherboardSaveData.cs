using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(MotherboardSaveData))]
public class LogicMotherboardSaveData : MotherboardSaveData
{
	[XmlArray("LogicStates")]
	[XmlArrayItem("LogicState")]
	public List<LogicStateSave> LogicStates = new List<LogicStateSave>();

	[XmlElement]
	public int CurrentLogicIndex;
}
