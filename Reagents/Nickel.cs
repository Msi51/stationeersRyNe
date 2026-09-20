using System;

namespace Reagents;

[Serializable]
public class Nickel : OrganicReagent
{
	public Nickel()
	{
		ReagentId = 11;
		Unit = "g";
	}

	public Nickel(double quantity)
		: base(quantity)
	{
		ReagentId = 11;
		Unit = "g";
		base.Quantity = quantity;
	}
}
