using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class LfoVolumeSaveData : LogicBaseSaveData
{
	[XmlElement]
	public int Intensity;

	[XmlElement]
	public long OutputId;

	[XmlElement]
	public int Bpm;

	[XmlElement]
	public int Waveform;
}
