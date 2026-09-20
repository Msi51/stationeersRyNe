using System;

namespace Reagents;

[Serializable]
public class Gold : OrganicReagent
{
	public Gold()
	{
		ReagentId = 4;
		Unit = "g";
	}

	public Gold(double quantity)
		: base(quantity)
	{
		ReagentId = 4;
		Unit = "g";
		base.Quantity = quantity;
	}
}
