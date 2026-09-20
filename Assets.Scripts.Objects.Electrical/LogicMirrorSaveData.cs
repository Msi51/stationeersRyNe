using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LogicMirrorSaveData : StructureSaveData
{
	[XmlElement]
	public long CurrentDeviceId;
}
