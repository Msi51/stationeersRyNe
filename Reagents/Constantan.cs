using System;

namespace Reagents;

[Serializable]
public class Constantan : OrganicReagent
{
	public Constantan()
	{
		ReagentId = 15;
		Unit = "g";
	}

	public Constantan(double quantity)
		: base(quantity)
	{
		ReagentId = 15;
		Unit = "g";
		base.Quantity = quantity;
	}
}
