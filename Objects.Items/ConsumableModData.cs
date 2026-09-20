using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class ConsumableModData : ItemModData
{
	public float MaxQuantity = float.NaN;

	public float UseAmount = float.NaN;

	public string AllowSplitting = string.Empty;
}
