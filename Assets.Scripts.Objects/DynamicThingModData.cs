using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(ThingModData))]
public class DynamicThingModData : ThingModData
{
	public float AtmosphereDampeningScale = float.NaN;

	public int TradeValue = -1;
}
