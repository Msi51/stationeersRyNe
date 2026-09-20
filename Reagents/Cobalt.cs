using System;

namespace Reagents;

[Serializable]
public class Cobalt : OrganicReagent
{
	public Cobalt()
	{
		ReagentId = 37;
		Unit = "g";
	}

	public Cobalt(double quantity)
		: base(quantity)
	{
		ReagentId = 37;
		Unit = "g";
		base.Quantity = quantity;
	}
}
