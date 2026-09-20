using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class HydrationBase : Consumable, IHydration
{
	public const float CONSUME_SPEED = 1f;

	public static float HydrationPerLitreWater => 5f;

	public static MoleQuantity WaterMolesPerUnitHydration => new MoleQuantity(55.55555555555556 / (double)HydrationPerLitreWater);

	public static float HydrationPerMole => 1f / WaterMolesPerUnitHydration.ToFloat();

	public float Hydration(float useAmount)
	{
		return 5f * useAmount;
	}

	public MoleQuantity GetCurrentMoleCount()
	{
		return new MoleQuantity(55.55555555555556 * (double)base.Quantity);
	}

	public float GetCurrentQuanitityFromMole(MoleQuantity moles)
	{
		return (moles / 55.55555555555556).ToFloat();
	}

	public MoleQuantity GetMissingMoleCount()
	{
		return new MoleQuantity(55.55555555555556 * (double)(MaxQuantity - base.Quantity));
	}

	public float HydrateAmount(Entity consumer)
	{
		return Mathf.Min((consumer.GetHydrationStorage() - consumer.Hydration) / 5f, base.Quantity);
	}

	public float HydrateTime(float quantityToDrink)
	{
		float num = quantityToDrink * 5f;
		float num2 = 1f / (float)DifficultySetting.Current.RespawnStressConsumptionSpeed;
		return 1f * num * num2;
	}

	public virtual void AddLiquidToThing(float quantity)
	{
		base.Quantity += Mathf.Min(quantity, MaxQuantity);
	}

	public override bool OnUseItem(float quantityToEat, Thing useOnThing)
	{
		if (!(useOnThing is Human human))
		{
			return false;
		}
		quantityToEat = Mathf.Min(quantityToEat, base.Quantity);
		MoleQuantity moleQuantity = new MoleQuantity((double)quantityToEat / 0.018);
		MoleEnergy energy = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, Mole.SpecificHeat(Chemistry.GasType.Water), moleQuantity);
		human.Hydrate(new Mole(Chemistry.GasType.Water, moleQuantity, energy));
		base.Quantity -= quantityToEat;
		OnStateChanged();
		return true;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction, actionCompletedRatio);
		}
		if (!(RootParent is Human human) || Math.Abs(human.GetHydrationStorage() - human.Hydration) < 0.005f || base.Quantity <= 0f)
		{
			return base.OnUseSecondary(doAction, actionCompletedRatio);
		}
		DelayedActionInstance result;
		if (!human.CanDrink())
		{
			result = new DelayedActionInstance
			{
				Duration = float.MaxValue,
				ActionMessage = ActionStrings.ConsumeFail
			};
		}
		else
		{
			float num = HydrateAmount(human);
			result = new DelayedActionInstance
			{
				Duration = HydrateTime(num),
				ActionMessage = ActionStrings.Consume,
				ActionSoundHash = Item.DrinkingHash,
				ActionCompleteSoundHash = Item.DrinkingFinishedHash
			};
			if (!doAction)
			{
				return result;
			}
			OnUseItem(num * actionCompletedRatio, RootParent);
		}
		return result;
	}

	public virtual void OnStateChanged()
	{
	}
}
