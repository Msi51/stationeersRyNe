using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class LogicStackData
{
	[XmlElement("Stack")]
	public List<LogicStackItemData> Registers = new List<LogicStackItemData>();
}
