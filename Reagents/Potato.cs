using System;

namespace Reagents;

[Serializable]
public class Potato : OrganicReagent
{
	public Potato()
	{
		ReagentId = 22;
		Unit = string.Empty;
	}

	public Potato(double quantity)
		: base(quantity)
	{
		ReagentId = 22;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
