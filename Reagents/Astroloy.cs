using System;

namespace Reagents;

[Serializable]
public class Astroloy : OrganicReagent
{
	public Astroloy()
	{
		ReagentId = 36;
		Unit = "g";
	}

	public Astroloy(double quantity)
		: base(quantity)
	{
		ReagentId = 36;
		Unit = "g";
		base.Quantity = quantity;
	}
}
