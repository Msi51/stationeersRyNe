using System;

namespace Reagents;

[Serializable]
public class Cocoa : OrganicReagent
{
	public Cocoa()
	{
		ReagentId = 44;
		Unit = "g";
	}

	public Cocoa(double quantity)
		: base(quantity)
	{
		ReagentId = 44;
		Unit = "g";
		base.Quantity = quantity;
	}
}
