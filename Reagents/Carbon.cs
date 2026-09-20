using System;

namespace Reagents;

[Serializable]
public class Carbon : OrganicReagent
{
	public Carbon()
	{
		ReagentId = 5;
		Unit = "g";
	}

	public Carbon(double quantity)
		: base(quantity)
	{
		ReagentId = 5;
		Unit = "g";
		base.Quantity = quantity;
	}
}
