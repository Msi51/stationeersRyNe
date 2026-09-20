using System;

namespace Reagents;

[Serializable]
public class Copper : OrganicReagent
{
	public Copper()
	{
		ReagentId = 7;
		Unit = "g";
	}

	public Copper(double quantity)
		: base(quantity)
	{
		ReagentId = 7;
		Unit = "g";
		base.Quantity = quantity;
	}
}
