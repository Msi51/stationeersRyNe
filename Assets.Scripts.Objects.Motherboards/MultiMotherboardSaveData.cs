using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(MotherboardSaveData))]
public class MultiMotherboardSaveData : MotherboardSaveData
{
	[XmlElement]
	public int CurrentTab;
}
