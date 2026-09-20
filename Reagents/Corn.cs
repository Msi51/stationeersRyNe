using System;

namespace Reagents;

[Serializable]
public class Corn : OrganicReagent
{
	public Corn()
	{
		ReagentId = 38;
		Unit = string.Empty;
	}

	public Corn(double quantity)
		: base(quantity)
	{
		ReagentId = 38;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
