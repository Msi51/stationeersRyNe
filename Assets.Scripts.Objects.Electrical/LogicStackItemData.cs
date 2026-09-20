using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class LogicStackItemData : DoubleReference
{
	[XmlAttribute("Index")]
	public int Index = -1;
}
