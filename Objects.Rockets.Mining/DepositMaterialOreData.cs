using System.Xml.Serialization;

namespace Objects.Rockets.Mining;

public class DepositMaterialOreData
{
	[XmlAttribute("Weight")]
	public float Weight = 1f;

	[XmlAttribute("Id")]
	public string PrefabName;
}
