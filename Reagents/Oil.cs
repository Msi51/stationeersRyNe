using System;

namespace Reagents;

[Serializable]
public class Oil : OrganicReagent
{
	public Oil()
	{
		ReagentId = 21;
		Unit = "ml";
	}

	public Oil(double quantity)
		: base(quantity)
	{
		ReagentId = 21;
		Unit = "ml";
		base.Quantity = quantity;
	}
}
