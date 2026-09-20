using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

public class Fertiliser : Stackable
{
	public float Cycles = 2f;

	public float HarvestBoost = 1f;

	public float GrowthSpeed = 1f;

	protected override void OnSplitStack(Stackable newStack)
	{
		base.OnSplitStack(newStack);
		if (newStack is Fertiliser fertiliser)
		{
			fertiliser.Cycles = Cycles;
			fertiliser.HarvestBoost = HarvestBoost;
			fertiliser.GrowthSpeed = GrowthSpeed;
		}
	}

	protected override void OnMergeStack(Stackable oldStack, float delta)
	{
		base.OnMergeStack(oldStack, delta);
		if (oldStack is Fertiliser fertiliser)
		{
			Cycles = (fertiliser.Cycles + Cycles) / 2f;
			HarvestBoost = (fertiliser.HarvestBoost + HarvestBoost) / 2f;
			GrowthSpeed = (fertiliser.GrowthSpeed + GrowthSpeed) / 2f;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FertiliserSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is FertiliserSaveData fertiliserSaveData)
		{
			Cycles = fertiliserSaveData.Cycles;
			HarvestBoost = fertiliserSaveData.HarvestBoost;
			GrowthSpeed = fertiliserSaveData.GrowthSpeed;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is FertiliserSaveData fertiliserSaveData)
		{
			fertiliserSaveData.Cycles = Cycles;
			fertiliserSaveData.HarvestBoost = HarvestBoost;
			fertiliserSaveData.GrowthSpeed = GrowthSpeed;
		}
	}

	public void CopyStats(Fertiliser newFertiliser)
	{
		newFertiliser.Cycles = Cycles;
		newFertiliser.HarvestBoost = HarvestBoost;
		newFertiliser.GrowthSpeed = GrowthSpeed;
	}

	public void UseOneCycle()
	{
		Cycles -= 1f;
		if (Cycles < 1f)
		{
			OnServer.Destroy(this);
		}
	}
}
