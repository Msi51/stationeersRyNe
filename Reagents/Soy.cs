using System;

namespace Reagents;

[Serializable]
public class Soy : OrganicReagent
{
	public Soy()
	{
		ReagentId = 41;
		Unit = string.Empty;
	}

	public Soy(double quantity)
		: base(quantity)
	{
		ReagentId = 41;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
