using System;

namespace Reagents;

[Serializable]
public class Hastelloy : OrganicReagent
{
	public Hastelloy()
	{
		ReagentId = 35;
		Unit = "g";
	}

	public Hastelloy(double quantity)
		: base(quantity)
	{
		ReagentId = 35;
		Unit = "g";
		base.Quantity = quantity;
	}
}
