using System;

namespace Reagents;

[Serializable]
public class Iron : OrganicReagent
{
	public Iron()
	{
		ReagentId = 3;
		Unit = "g";
	}

	public Iron(double quantity)
		: base(quantity)
	{
		ReagentId = 3;
		Unit = "g";
		base.Quantity = quantity;
	}
}
