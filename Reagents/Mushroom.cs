using System;

namespace Reagents;

[Serializable]
public class Mushroom : OrganicReagent
{
	public Mushroom()
	{
		ReagentId = 42;
		Unit = "g";
	}

	public Mushroom(double quantity)
		: base(quantity)
	{
		ReagentId = 42;
		Unit = "g";
		base.Quantity = quantity;
	}
}
