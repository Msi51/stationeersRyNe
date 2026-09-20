using System;

namespace Reagents;

[Serializable]
public class Sugar : OrganicReagent
{
	public Sugar()
	{
		ReagentId = 43;
		Unit = "g";
	}

	public Sugar(double quantity)
		: base(quantity)
	{
		ReagentId = 43;
		Unit = "g";
		base.Quantity = quantity;
	}
}
