using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;

namespace Objects.Electrical;

[XmlInclude(typeof(LogicBaseSaveData))]
public class StepSequencerSaveData : LogicBaseSaveData
{
	[XmlElement]
	public long[] InputIds;

	[XmlElement]
	public int Attack;

	[XmlElement]
	public int Release;

	[XmlElement]
	public long OutputId;

	[XmlElement]
	public int Bpm;
}
