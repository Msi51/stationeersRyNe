using System.Xml.Serialization;

namespace Trading;

[XmlType("DelayedAction")]
public abstract class DelayedAction : ActionData
{
	[XmlElement("Delay")]
	public TimeSpanReference Delay;
}
