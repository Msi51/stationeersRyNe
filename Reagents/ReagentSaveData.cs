using System.Xml.Serialization;

namespace Reagents;

[XmlRoot]
public class ReagentSaveData
{
	[XmlElement]
	public string TypeName;

	[XmlElement]
	public double Quantity;

	public ReagentSaveData()
	{
	}

	public ReagentSaveData(Reagent reagent)
	{
		TypeName = reagent.TypeName;
		Quantity = reagent.Quantity;
	}
}
