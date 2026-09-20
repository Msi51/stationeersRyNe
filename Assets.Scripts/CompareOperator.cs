using System.Xml.Serialization;

namespace Assets.Scripts;

public enum CompareOperator
{
	Unassigned,
	[XmlEnum("Less")]
	Less,
	[XmlEnum("EqualOrLess")]
	EqualOrLess,
	[XmlEnum("Equal")]
	Equal,
	[XmlEnum("EqualOrGreater")]
	EqualOrGreater,
	[XmlEnum("Greater")]
	Greater
}
