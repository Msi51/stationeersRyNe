using System;

namespace Reagents;

[Serializable]
public class Silver : OrganicReagent
{
	public Silver()
	{
		ReagentId = 10;
		Unit = "g";
	}

	public Silver(double quantity)
		: base(quantity)
	{
		ReagentId = 10;
		Unit = "g";
		base.Quantity = quantity;
	}
}
