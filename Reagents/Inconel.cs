using System;

namespace Reagents;

[Serializable]
public class Inconel : OrganicReagent
{
	public Inconel()
	{
		ReagentId = 34;
		Unit = "g";
	}

	public Inconel(double quantity)
		: base(quantity)
	{
		ReagentId = 34;
		Unit = "g";
		base.Quantity = quantity;
	}
}
