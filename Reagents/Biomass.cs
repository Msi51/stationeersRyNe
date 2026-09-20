using System;

namespace Reagents;

[Serializable]
public class Biomass : OrganicReagent
{
	public Biomass()
	{
		ReagentId = 40;
		Unit = string.Empty;
	}

	public Biomass(double quantity)
		: base(quantity)
	{
		ReagentId = 40;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
