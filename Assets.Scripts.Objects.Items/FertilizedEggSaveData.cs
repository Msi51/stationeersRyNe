using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class FertilizedEggSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float EggHatchTime;

	[XmlElement]
	public bool Viable;
}
