using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class SpeakerSaveData : StructureSaveData
{
	[XmlElement]
	public float Volume;

	[XmlElement]
	public float ClipTime;
}
