using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Weapons;

[XmlInclude(typeof(DynamicThingSaveData))]
public class GrenadeSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float Lifetime;
}
