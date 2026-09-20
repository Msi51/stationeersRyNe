using Reagents;

namespace Assets.Scripts.Objects.Items;

public class Slag : Ore
{
	public override float ProcessTime => 6f;

	protected override void OnSplitStack(Stackable newStack)
	{
		base.OnSplitStack(newStack);
		newStack.CreatedReagentMixture = CreatedReagentMixture;
	}

	protected override void OnMergeStack(Stackable oldStack, float delta)
	{
		base.OnMergeStack(oldStack, delta);
		if (oldStack.CreatedReagentMixture.TotalReagents > 0.0)
		{
			CreatedReagentMixture *= (double)base.Quantity;
			ReagentMixture reagentMixture = new ReagentMixture(oldStack.CreatedReagentMixture);
			CreatedReagentMixture.Add(reagentMixture * oldStack.Quantity);
			CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SlagSaveData slagSaveData)
		{
			slagSaveData.CreatedReagents = CreatedReagentMixture.Serialize();
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SlagSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (!(saveData is SlagSaveData slagSaveData))
		{
			return;
		}
		foreach (ReagentSaveData createdReagent in slagSaveData.CreatedReagents)
		{
			Reagent reagent = Reagent.Generate(createdReagent.TypeName);
			if (reagent != null)
			{
				reagent.Quantity = createdReagent.Quantity;
				if (CreatedReagentMixture == null)
				{
					CreatedReagentMixture = new ReagentMixture(reagent);
				}
				else
				{
					CreatedReagentMixture.Add(reagent);
				}
				if (CreatedReagentMixture.TotalReagents > 1.0)
				{
					CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
				}
			}
		}
	}
}
