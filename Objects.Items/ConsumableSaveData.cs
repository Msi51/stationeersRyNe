using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class ConsumableSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float Quantity;
}
