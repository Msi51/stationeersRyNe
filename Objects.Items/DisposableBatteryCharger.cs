using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using CharacterCustomisation;
using UnityEngine;

namespace Objects.Items;

public class DisposableBatteryCharger : Consumable
{
	[SerializeField]
	private float powerBase;

	public const float TIME_TO_USE = 1f;

	public float PowerBase => powerBase;

	public float PowerStored => base.Quantity * powerBase;

	public override int ConstructingSoundHash => Defines.Sounds.DisposableBatteryChargerHash;

	public override int FinishedConstructingSoundHash => Defines.Sounds.DisposableBatteryChargerFinishedHash;

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
		if (human == null || !OnOff)
		{
			return base.OnUseSecondary(doAction);
		}
		BatteryCell targetBattery = GetTargetBattery(human);
		if (!targetBattery)
		{
			return new DelayedActionInstance
			{
				Duration = float.MaxValue,
				ActionMessage = GameStrings.FullyCharged.DisplayString
			};
		}
		float num = Mathf.Min(PowerStored, targetBattery.PowerMaximum - targetBattery.PowerStored) / PowerBase;
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.Recharge.DisplayString,
			ActionSoundHash = Defines.Sounds.DisposableBatteryChargerHash,
			ActionCompleteSoundHash = Defines.Sounds.DisposableBatteryChargerFinishedHash
		};
		if (!doAction)
		{
			return delayedActionInstance;
		}
		if (GameManager.RunSimulation && actionCompletionRatio >= 1f)
		{
			OnUseItem(num * actionCompletionRatio, targetBattery);
		}
		return delayedActionInstance.Succeed();
	}

	public BatteryCell GetTargetBattery(Human targetHuman)
	{
		switch (targetHuman.SpeciesClass)
		{
		case SpeciesClass.None:
			return null;
		case SpeciesClass.Human:
		case SpeciesClass.Zrilian:
			if (targetHuman.Suit == null)
			{
				return null;
			}
			return targetHuman.Suit.Battery;
		case SpeciesClass.Robot:
		{
			BatteryCell batteryCell = targetHuman.RobotBattery;
			if (batteryCell == null || batteryCell.PowerRatio > 0.99f)
			{
				batteryCell = ((targetHuman.Suit != null) ? targetHuman.Suit.Battery : null);
			}
			return batteryCell;
		}
		default:
			return null;
		}
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (onUseThing is BatteryCell batteryCell)
		{
			batteryCell.AddPowerSafe(PowerStored);
			OnServer.Interact(base.InteractOnOff, 0);
			return base.OnUseItem(base.Quantity, onUseThing);
		}
		return false;
	}
}
