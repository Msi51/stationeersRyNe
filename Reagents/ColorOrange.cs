using System;

namespace Reagents;

[Serializable]
public class ColorOrange : OrganicReagent
{
	public ColorOrange()
	{
		ReagentId = 29;
		Unit = "g";
	}

	public ColorOrange(double quantity)
		: base(quantity)
	{
		ReagentId = 29;
		Unit = "g";
		base.Quantity = quantity;
	}
}
