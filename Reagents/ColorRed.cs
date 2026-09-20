using System;

namespace Reagents;

[Serializable]
public class ColorRed : OrganicReagent
{
	public ColorRed()
	{
		ReagentId = 25;
		Unit = "g";
	}

	public ColorRed(double quantity)
		: base(quantity)
	{
		ReagentId = 25;
		Unit = "g";
		base.Quantity = quantity;
	}
}
