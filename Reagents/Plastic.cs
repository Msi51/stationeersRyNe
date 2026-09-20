using System;

namespace Reagents;

[Serializable]
public class Plastic : OrganicReagent
{
	public Plastic()
	{
		ReagentId = 17;
		Unit = "g";
	}

	public Plastic(double quantity)
		: base(quantity)
	{
		ReagentId = 17;
		Unit = "g";
		base.Quantity = quantity;
	}
}
