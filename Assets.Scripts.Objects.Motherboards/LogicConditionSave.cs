using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Motherboards;

[XmlRoot]
public class LogicConditionSave
{
	[XmlElement]
	public long DeviceReferenceId;

	[XmlElement]
	public LogicType Type;

	[XmlElement]
	public ConditionOperation Operation;

	[XmlElement]
	public double Value;

	[XmlElement]
	public bool IsDisconnected;

	[XmlElement]
	public bool IsTrue;

	public LogicConditionSave()
	{
	}

	public LogicConditionSave(LogicCondition condition)
	{
		DeviceReferenceId = ((condition.Device != null) ? condition.Device.ReferenceId : 0);
		Type = condition.Type;
		Operation = condition.Operation;
		Value = condition.Value;
		IsDisconnected = condition.IsDisconnected;
		IsTrue = condition.IsTrue;
	}
}
