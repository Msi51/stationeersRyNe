using System;

namespace Reagents;

[Serializable]
public class Lead : OrganicReagent
{
	public Lead()
	{
		ReagentId = 12;
		Unit = "g";
	}

	public Lead(double quantity)
		: base(quantity)
	{
		ReagentId = 12;
		Unit = "g";
		base.Quantity = quantity;
	}
}
