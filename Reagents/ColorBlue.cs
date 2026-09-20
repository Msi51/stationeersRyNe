using System;

namespace Reagents;

[Serializable]
public class ColorBlue : OrganicReagent
{
	public ColorBlue()
	{
		ReagentId = 27;
		Unit = "g";
	}

	public ColorBlue(double quantity)
		: base(quantity)
	{
		ReagentId = 27;
		Unit = "g";
		base.Quantity = quantity;
	}
}
