using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class OreSaveData : StackableSaveData
{
	[XmlElement]
	public int QuantitySmelted;
}
