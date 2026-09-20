using System;

namespace Reagents;

[Serializable]
public class Alcohol : OrganicReagent
{
	public Alcohol()
	{
		ReagentId = 20;
		Unit = "ml";
	}

	public Alcohol(double quantity)
		: base(quantity)
	{
		ReagentId = 20;
		Unit = "ml";
		base.Quantity = quantity;
	}
}
