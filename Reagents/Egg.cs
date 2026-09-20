using System;

namespace Reagents;

[Serializable]
public class Egg : OrganicReagent
{
	public Egg()
	{
		ReagentId = 2;
		Unit = string.Empty;
	}

	public Egg(double quantity)
		: base(quantity)
	{
		ReagentId = 2;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
