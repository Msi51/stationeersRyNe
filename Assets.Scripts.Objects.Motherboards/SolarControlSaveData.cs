using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(MotherboardSaveData))]
public class SolarControlSaveData : MotherboardSaveData
{
	[XmlElement]
	public float TargetHorizontal;

	[XmlElement]
	public float TargetVertical;
}
