using System;

namespace Reagents;

[Serializable]
public class Silicon : OrganicReagent
{
	public Silicon()
	{
		ReagentId = 18;
		Unit = "g";
	}

	public Silicon(double quantity)
		: base(quantity)
	{
		ReagentId = 18;
		Unit = "g";
		base.Quantity = quantity;
	}
}
