using System.Xml.Serialization;
using Reagents;

namespace Objects.Rockets.Mining;

public class DepositMaterialReagentMixData : ReagentAction
{
	[XmlAttribute("Weight")]
	public float Weight = 1f;
}
