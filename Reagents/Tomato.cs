using System;

namespace Reagents;

[Serializable]
public class Tomato : OrganicReagent
{
	public Tomato()
	{
		ReagentId = 23;
		Unit = string.Empty;
	}

	public Tomato(double quantity)
		: base(quantity)
	{
		ReagentId = 23;
		Unit = string.Empty;
		base.Quantity = quantity;
	}
}
