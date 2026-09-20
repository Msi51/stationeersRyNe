using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(StackableSaveData))]
public class RoadflareSaveData : StackableSaveData
{
	[XmlElement]
	public float Lifetime;
}
