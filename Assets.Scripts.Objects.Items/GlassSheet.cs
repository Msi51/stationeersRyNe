using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class GlassSheet : Stackable, IResource, IShowBuildStateTooltip, ISolarRepairer, IRepairer
{
	[Header("ISolarRepairer")]
	[Tooltip("Time taken to repair one full unit of leaking")]
	public float RepairSpeed = 1f;

	public override int ConstructingSoundHash => Animator.StringToHash("GlassSheetsUsing");

	public override int FinishedConstructingSoundHash => Animator.StringToHash("GlassSheetsDone");

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public float RepairQuantity(IRepairable item, float actionCompletionRatio = 1f)
	{
		if (item == null)
		{
			return 0f;
		}
		return Mathf.Clamp(item.RepairRatio * actionCompletionRatio * 2f, 1f, base.Quantity);
	}

	public float GetRepairSpeed()
	{
		return RepairSpeed;
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
