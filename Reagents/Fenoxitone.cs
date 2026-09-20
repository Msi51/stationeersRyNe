using System;

namespace Reagents;

[Serializable]
public class Fenoxitone : OrganicReagent
{
	public Fenoxitone()
	{
		ReagentId = 24;
		Unit = "g";
	}

	public Fenoxitone(double quantity)
		: base(quantity)
	{
		ReagentId = 24;
		Unit = "g";
		base.Quantity = quantity;
	}
}
