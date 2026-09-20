using System;

namespace Reagents;

[Serializable]
public class Hydrocarbon : OrganicReagent
{
	public Hydrocarbon()
	{
		ReagentId = 9;
		Unit = "g";
	}

	public Hydrocarbon(double quantity)
		: base(quantity)
	{
		ReagentId = 9;
		Unit = "g";
		base.Quantity = quantity;
	}
}
