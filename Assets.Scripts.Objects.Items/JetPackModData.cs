using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class JetPackModData : ThingModData
{
	public float MaxSpeed = float.NaN;
}
