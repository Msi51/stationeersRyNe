using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects.Items;

public class ItemRenamable : Item, IRepairable
{
	public static float RepairSpeedScale = 0.2f;

	public virtual float RepairRatio => DamageState.TotalRatio;

	public override bool AttackWithAllowIncomplete => true;

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (attack.SourceItem is ISuitReparier suitReparier)
		{
			float num = suitReparier.RepairQuantity(this);
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = num * suitReparier.GetRepairSpeed() * RepairSpeedScale,
				ActionMessage = "Repair"
			};
			if (DamageState.TotalRatio <= float.Epsilon)
			{
				return delayedActionInstance.Fail(GameStrings.StructureIsNotDamaged, ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			suitReparier.Repair(base.netId, num * attack.CompletedRatio);
			return delayedActionInstance;
		}
		Labeller labeller = sourceItem as Labeller;
		if (labeller != null)
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance2.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance2.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance2;
			}
			labeller.Rename(this);
			return delayedActionInstance2;
		}
		return base.AttackWith(attack, doAction);
	}
}
