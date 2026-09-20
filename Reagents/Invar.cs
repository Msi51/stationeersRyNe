using System;

namespace Reagents;

[Serializable]
public class Invar : OrganicReagent
{
	public Invar()
	{
		ReagentId = 14;
		Unit = "g";
	}

	public Invar(double quantity)
		: base(quantity)
	{
		ReagentId = 14;
		Unit = "g";
		base.Quantity = quantity;
	}
}
