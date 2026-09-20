using System;

namespace Reagents;

[Serializable]
public class Solder : OrganicReagent
{
	public Solder()
	{
		ReagentId = 16;
		Unit = "g";
	}

	public Solder(double quantity)
		: base(quantity)
	{
		ReagentId = 16;
		Unit = "g";
		base.Quantity = quantity;
	}
}
