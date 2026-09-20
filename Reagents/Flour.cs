using System;

namespace Reagents;

[Serializable]
public class Flour : OrganicReagent
{
	public Flour()
	{
		ReagentId = 0;
		Unit = "g";
	}

	public Flour(double quantity)
		: base(quantity)
	{
		ReagentId = 0;
		Unit = "g";
		base.Quantity = quantity;
	}
}
