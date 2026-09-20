using System.Xml.Serialization;
using Reagents;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(ThingModData))]
[XmlInclude(typeof(DynamicThingModData))]
public class ItemModData : DynamicThingModData
{
	public float InventoryScale = float.NaN;

	public ReagentMixture CreatedReagentMixture;
}
