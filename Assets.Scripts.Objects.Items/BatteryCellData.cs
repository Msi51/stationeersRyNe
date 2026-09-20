using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class BatteryCellData : ItemModData
{
	public float PowerMaximum = float.NaN;
}
