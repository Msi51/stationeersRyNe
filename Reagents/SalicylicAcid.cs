using System;

namespace Reagents;

[Serializable]
public class SalicylicAcid : OrganicReagent
{
	public SalicylicAcid()
	{
		ReagentId = 19;
		Unit = "g";
	}

	public SalicylicAcid(double quantity)
		: base(quantity)
	{
		ReagentId = 19;
		Unit = "g";
		base.Quantity = quantity;
	}
}
