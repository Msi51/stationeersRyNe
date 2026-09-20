namespace Reagents;

public class OrganicReagent : Reagent
{
	public OrganicReagent()
	{
	}

	public OrganicReagent(double quantity)
		: base(quantity)
	{
	}

	public override void Burn()
	{
		base.Burn();
		ParentMixture.Carbon.Quantity += base.Quantity;
		base.Quantity = 0.0;
	}
}
