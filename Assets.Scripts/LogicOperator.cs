using System.Xml.Serialization;

namespace Assets.Scripts;

public enum LogicOperator : byte
{
	[XmlEnum("All")]
	All,
	[XmlEnum("Any")]
	Any,
	[XmlEnum("None")]
	None
}
