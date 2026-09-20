using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class OrganicDamageState : ThingDamageState
{
	public override bool Indestructable => false;

	public OrganicDamageState(Thing parent)
		: base(parent)
	{
	}

	protected override bool DamageAllowed(DamageUpdateType updateType)
	{
		switch (updateType)
		{
		case DamageUpdateType.Oxygen:
		case DamageUpdateType.Hydration:
		case DamageUpdateType.Radiation:
		case DamageUpdateType.Toxic:
		case DamageUpdateType.Stun:
		case DamageUpdateType.Decay:
			return true;
		default:
			return base.DamageAllowed(updateType);
		}
	}

	public override void Heal(float quantity)
	{
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Oxygen);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Toxic);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Radiation);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Hydration);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Stun);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Decay);
		base.Heal(quantity);
	}

	public override void HealAll(float minDamageRemaining)
	{
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Oxygen).Value, minDamageRemaining), DamageUpdateType.Oxygen);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Toxic).Value, minDamageRemaining), DamageUpdateType.Toxic);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Radiation).Value, minDamageRemaining), DamageUpdateType.Radiation);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Hydration).Value, minDamageRemaining), DamageUpdateType.Hydration);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Stun).Value, minDamageRemaining), DamageUpdateType.Stun);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Decay).Value, minDamageRemaining), DamageUpdateType.Decay);
		base.HealAll(minDamageRemaining);
	}
}
