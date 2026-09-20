using System;

namespace Reagents;

[Serializable]
public class ColorGreen : OrganicReagent
{
	public ColorGreen()
	{
		ReagentId = 26;
		Unit = "g";
	}

	public ColorGreen(double quantity)
		: base(quantity)
	{
		ReagentId = 26;
		Unit = "g";
		base.Quantity = quantity;
	}
}
