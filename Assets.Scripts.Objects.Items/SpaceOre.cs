using System.Text;
using Assets.Scripts.Atmospherics;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SpaceOre : Slag
{
	public static readonly int SpaceOreStackSize = 100;

	public override bool IsCentrifugeSmelt => false;

	public override ReagentMixture CentrifugeProcessUnit()
	{
		ReagentMixture reagentMixture = new ReagentMixture();
		if (CreatedReagentMixture.TotalReagents > 0.0)
		{
			if (CreatedReagentMixture.TotalReagents > 1.0)
			{
				CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
			}
			reagentMixture.Add(CreatedReagentMixture);
		}
		base.Quantity--;
		return reagentMixture;
	}

	protected override void OnMergeStack(Stackable oldStack, float delta)
	{
	}

	public override StringBuilder GetExtendedText()
	{
		return base.GetExtendedText();
	}

	public override void Merge(IMergeable mergeable)
	{
		if (mergeable is Stackable stackable)
		{
			int num = Mathf.Min(base.Quantity + stackable.Quantity, MaxQuantity) - base.Quantity;
			int num2 = base.Quantity + num;
			ReagentMixture reagentMixture = new ReagentMixture(CreatedReagentMixture) * base.Quantity;
			ReagentMixture reagentMixture2 = new ReagentMixture(stackable.CreatedReagentMixture) * num;
			reagentMixture.Add(reagentMixture2);
			CreatedReagentMixture.Set(reagentMixture / num2);
			base.Merge(mergeable);
		}
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		base.Quantity--;
	}
}
