using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Tool : ItemRenamable, IShowBuildStateTooltip
{
	[Header("Tool")]
	[SerializeField]
	private float manuallyAuthoredToolSpeedOffset;

	private readonly float _suitlessAddedSpeed = 0.2f;

	public virtual bool IsOperable => true;

	public virtual float getToolSpeed()
	{
		float num = 1f;
		num += manuallyAuthoredToolSpeedOffset;
		Human human = RootParent as Human;
		if ((bool)human)
		{
			if (human.Suit == null)
			{
				num += _suitlessAddedSpeed;
			}
			else if (human.Suit is SuitBase suitBase)
			{
				num *= suitBase.ToolSpeedMultiplier;
			}
			float num2 = (human.ExperiencingRespawnStress ? ((float)DifficultySetting.Current.RespawnStressToolUseSpeed) : 1f);
			float num3 = ((human.Mood > 0f) ? 1f : ((float)DifficultySetting.Current.MoodToolSpeedMultiplier));
			float num4 = ((human.Hygiene > 1f) ? ((float)DifficultySetting.Current.GoodHygieneToolSpeedMultiplier) : 1f);
			num *= num4;
			num *= num3;
			num *= num2;
		}
		return num;
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override bool CheckTogglePower()
	{
		return base.CanTogglePower;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ManualTools);
	}

	public bool CanUseTool(ref DelayedActionInstance actionInstance)
	{
		if (IsOperable)
		{
			return true;
		}
		actionInstance.IsDisabled = true;
		actionInstance.AppendStateMessage(GameStrings.ToolNotCurrentlyOperableForTask, DisplayName);
		return false;
	}

	public virtual void OnMinedOre(Ore oreMined)
	{
		if ((object)oreMined != null && oreMined.PrefabHash == Ore.COBALT_ORE_HASH)
		{
			Achievements.Increment(Achievements.Stat.TotalCobaltMined, oreMined.Quantity);
		}
		if (base.ParentSlot == null)
		{
			return;
		}
		foreach (Slot slot in RootParent.Slots)
		{
			if (slot.Type == Slot.Class.Suit && slot.Contains<SuitBase>(out var occupant) && occupant.BackSlot.Contains<MiningBelt>(out var occupant2))
			{
				occupant2.TryAddOre(oreMined);
				continue;
			}
			MiningBelt miningBelt = slot.Occupant as MiningBelt;
			if (miningBelt?.SlotType == slot.Type && miningBelt.TryAddOre(oreMined))
			{
				break;
			}
		}
	}

	public void OnMinedVoxel(float density)
	{
		if (base.ParentSlot != null)
		{
			FindAvailableDirtCanister()?.AddDirt(density);
		}
	}
}
