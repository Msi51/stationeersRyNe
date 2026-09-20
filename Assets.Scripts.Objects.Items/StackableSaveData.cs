using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class StackableSaveData : DynamicThingSaveData
{
	[XmlElement]
	public int Quantity;
}
