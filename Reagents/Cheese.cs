using System;

namespace Reagents;

[Serializable]
public class Cheese : OrganicReagent
{
	public Cheese()
	{
		ReagentId = 45;
		Unit = "g";
	}

	public Cheese(double quantity)
		: base(quantity)
	{
		ReagentId = 45;
		Unit = "g";
		base.Quantity = quantity;
	}
}
