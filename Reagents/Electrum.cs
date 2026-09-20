using System;

namespace Reagents;

[Serializable]
public class Electrum : OrganicReagent
{
	public Electrum()
	{
		ReagentId = 13;
		Unit = "g";
	}

	public Electrum(double quantity)
		: base(quantity)
	{
		ReagentId = 13;
		Unit = "g";
		base.Quantity = quantity;
	}
}
