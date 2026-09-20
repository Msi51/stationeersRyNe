using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Motherboards;

[XmlRoot]
public class LogicActionSave
{
	[XmlElement]
	public long DeviceReferenceId;

	[XmlElement]
	public LogicType Type;

	[XmlElement]
	public double Value;

	public LogicActionSave()
	{
	}

	public LogicActionSave(LogicAction action)
	{
		DeviceReferenceId = ((action.Device != null) ? action.Device.ReferenceId : 0);
		Type = action.Type;
		Value = action.Value;
	}
}
