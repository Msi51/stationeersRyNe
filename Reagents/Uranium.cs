using System;

namespace Reagents;

[Serializable]
public class Uranium : OrganicReagent
{
	public Uranium()
	{
		ReagentId = 6;
		Unit = "g";
	}

	public Uranium(double quantity)
		: base(quantity)
	{
		ReagentId = 6;
		Unit = "g";
		base.Quantity = quantity;
	}
}
