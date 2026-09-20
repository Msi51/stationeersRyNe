using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects;

public class DirtCanisterSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float CurrentCollectedDirt;
}
