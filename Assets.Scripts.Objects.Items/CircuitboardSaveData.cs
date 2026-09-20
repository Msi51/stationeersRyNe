using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(MotherboardSaveData))]
public class CircuitboardSaveData : MotherboardSaveData
{
	[XmlElement]
	public int LastIndex;

	[XmlElement]
	public string FilterString;
}
