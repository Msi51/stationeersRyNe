using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Vehicles;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DuctTape : Consumable, ISuitReparier, IRepairer, ISolarRepairer, IRobotRepairer, IRoverRepairer
{
	[Header("Duct Tape")]
	[Tooltip("Time taken to repair one full unit of leaking")]
	public float RepairSpeed = 1f;

	public static readonly int DuctTapeUsingHash = Animator.StringToHash("DuctTapeUsing");

	public static readonly int DuctTapeFinishedHash = Animator.StringToHash("DuctTapeFinished");

	public override int ConstructingSoundHash => DuctTapeUsingHash;

	public override int FinishedConstructingSoundHash => DuctTapeFinishedHash;

	public event Event OnDuctTapeUsedEvent;

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletionRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction);
		}
		Human human = RootParent as Human;
		if (human != null)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				ActionSoundHash = DuctTapeUsingHash,
				ActionCompleteSoundHash = DuctTapeFinishedHash
			};
			if (human.Suit != null && human.Suit.LeakRatio > 0f)
			{
				delayedActionInstance.Duration = RepairQuantity(human.Suit.AsRepairable) * RepairSpeed;
				delayedActionInstance.ActionMessage = $"Patch {human.Suit.AsThing.ToTooltip()}";
				if (!doAction)
				{
					return delayedActionInstance;
				}
				RepairLeak(human.Suit.AsThing.netId, RepairQuantity(human.Suit.AsRepairable, actionCompletionRatio));
				this.OnDuctTapeUsedEvent?.Invoke();
				return delayedActionInstance;
			}
			if (human.HeadAsSpaceHelmet != null && human.HeadAsSpaceHelmet.LeakRatio > 0f)
			{
				delayedActionInstance.Duration = RepairQuantity(human.HeadAsSpaceHelmet) * RepairSpeed;
				delayedActionInstance.ActionMessage = $"Patch {human.HeadAsSpaceHelmet.ToTooltip()}";
				if (!doAction)
				{
					return delayedActionInstance;
				}
				RepairLeak(human.HeadAsSpaceHelmet.netId, RepairQuantity(human.HeadAsSpaceHelmet, actionCompletionRatio));
				this.OnDuctTapeUsedEvent?.Invoke();
				return delayedActionInstance;
			}
		}
		return base.OnUseSecondary(doAction);
	}

	public float RepairQuantity(IRepairable item, float actionCompletionRatio = 1f)
	{
		if (item == null)
		{
			return 0f;
		}
		return Mathf.Min(item.RepairRatio * actionCompletionRatio, base.Quantity);
	}

	public float GetRepairSpeed()
	{
		return RepairSpeed;
	}

	public void RepairLeak(long suitNetId, float quantityToRepair)
	{
		AtmosphericItem atmosphericItem = Thing.Find<AtmosphericItem>(suitNetId);
		if (atmosphericItem != null && OnUseItem(quantityToRepair, atmosphericItem))
		{
			atmosphericItem.LeakRatio -= quantityToRepair;
		}
	}

	public void Repair(long repairedThingId, float ratioToRepair)
	{
		Thing thing = Thing.Find<Thing>(repairedThingId);
		if ((object)thing != null && OnUseItem(ratioToRepair, thing))
		{
			if (thing.DamageState.TotalRatio <= ratioToRepair)
			{
				thing.DamageState.HealAll();
			}
			else
			{
				thing.DamageState.Heal(ratioToRepair * thing.DamageState.MaxDamage);
			}
		}
	}
}
