using System;

namespace Reagents;

[Serializable]
public class Steel : OrganicReagent
{
	public Steel()
	{
		ReagentId = 8;
		Unit = "g";
	}

	public Steel(double quantity)
		: base(quantity)
	{
		ReagentId = 8;
		Unit = "g";
		base.Quantity = quantity;
	}
}
