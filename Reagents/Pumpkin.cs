using System;

namespace Reagents;

[Serializable]
public class Pumpkin : OrganicReagent
{
	public Pumpkin()
	{
		ReagentId = 30;
		Unit = string.Empty;
	}

	public Pumpkin(double quantity)
		: base(quantity)
	{
		ReagentId = 30;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
