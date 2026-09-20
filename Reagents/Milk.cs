using System;

namespace Reagents;

[Serializable]
public class Milk : OrganicReagent
{
	public Milk()
	{
		ReagentId = 1;
		Unit = "ml";
	}

	public Milk(double quantity)
		: base(quantity)
	{
		ReagentId = 1;
		Unit = "ml";
		base.Quantity = quantity;
	}
}
