using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using CharacterCustomisation;

namespace Assets.Scripts.Objects.Items;

public class HemDroidRepairKit : Stackable
{
	public override int ConstructingSoundHash => Defines.Sounds.HemDroidRepairKitHash;

	public override int FinishedConstructingSoundHash => Defines.Sounds.HemDroidRepairKitFinishedHash;

	public override string GetQuantityText()
	{
		return "x" + base.Quantity;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this)
		{
			return null;
		}
		if (!RootParentHuman.IsDamaged())
		{
			return null;
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.Repair.DisplayString,
			ActionSoundHash = Defines.Sounds.HemDroidRepairKitHash,
			ActionCompleteSoundHash = Defines.Sounds.HemDroidRepairKitFinishedHash
		};
		if (!doAction)
		{
			return result;
		}
		if (actionCompletedRatio >= 1f)
		{
			OnUseItem(actionCompletedRatio, RootParent);
		}
		return result;
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (onUseThing == null)
		{
			return true;
		}
		Human human = onUseThing as Human;
		if (!human || human.SpeciesClass != SpeciesClass.Robot)
		{
			return true;
		}
		human.DamageState.HealAll();
		if (human.OrganBrain != null)
		{
			human.OrganBrain.DamageState.HealAll();
		}
		return base.OnUseItem(quantity, onUseThing);
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}
}
