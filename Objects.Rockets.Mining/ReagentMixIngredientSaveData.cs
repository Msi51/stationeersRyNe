using System.Xml.Serialization;
using Reagents;

namespace Objects.Rockets.Mining;

public class ReagentMixIngredientSaveData
{
	[XmlAttribute("Id")]
	public string ReagentName;

	[XmlAttribute("Quantity")]
	public double Quantity;

	public ReagentMixIngredientSaveData()
	{
	}

	public ReagentMixIngredientSaveData(Reagent reagent)
	{
		ReagentName = reagent.TypeName;
		Quantity = reagent.Quantity;
	}
}
