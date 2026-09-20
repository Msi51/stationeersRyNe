using System;

namespace Reagents;

[Serializable]
public class Waspaloy : OrganicReagent
{
	public Waspaloy()
	{
		ReagentId = 32;
		Unit = "g";
	}

	public Waspaloy(double quantity)
		: base(quantity)
	{
		ReagentId = 32;
		Unit = "g";
		base.Quantity = quantity;
	}
}
