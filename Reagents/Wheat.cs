using System;

namespace Reagents;

[Serializable]
public class Wheat : OrganicReagent
{
	public Wheat()
	{
		ReagentId = 39;
		Unit = string.Empty;
	}

	public Wheat(double quantity)
		: base(quantity)
	{
		ReagentId = 39;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
