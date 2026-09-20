using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class DynamiteSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float Lifetime;
}
