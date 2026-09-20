using System;

namespace Reagents;

[Serializable]
public class ColorYellow : OrganicReagent
{
	public ColorYellow()
	{
		ReagentId = 28;
		Unit = "g";
	}

	public ColorYellow(double quantity)
		: base(quantity)
	{
		ReagentId = 28;
		Unit = "g";
		base.Quantity = quantity;
	}
}
