using System;

namespace Reagents;

[Serializable]
public class Rice : OrganicReagent
{
	public Rice()
	{
		ReagentId = 31;
		Unit = "g";
	}

	public Rice(double quantity)
		: base(quantity)
	{
		ReagentId = 31;
		Unit = "g";
		base.Quantity = quantity;
	}
}
