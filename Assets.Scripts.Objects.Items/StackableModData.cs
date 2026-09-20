using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class StackableModData : ItemModData
{
	public float MaxQuantity = float.NaN;
}
