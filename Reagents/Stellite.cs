using System;

namespace Reagents;

[Serializable]
public class Stellite : OrganicReagent
{
	public Stellite()
	{
		ReagentId = 33;
		Unit = "g";
	}

	public Stellite(double quantity)
		: base(quantity)
	{
		ReagentId = 33;
		Unit = "g";
		base.Quantity = quantity;
	}
}
