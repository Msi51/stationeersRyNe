using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects;

public interface IRepairer
{
	float RepairQuantity(IRepairable item, float actionCompletionRatio = 1f);

	float GetRepairSpeed();

	void Repair(long repairedThingId, float ratioToRepair);
}
