using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class EntityDamageState : OrganicDamageState
{
	private float _starvation;

	private Entity ParentEntity => Parent as Entity;

	public override bool Indestructable => false;

	public override float Oxygen
	{
		get
		{
			if (!ParentEntity || !(ParentEntity.OrganBrain != null))
			{
				return base.MaxDamage;
			}
			return ParentEntity.OrganBrain.DamageState.Oxygen;
		}
	}

	public override float Stun
	{
		get
		{
			if (!ParentEntity || !(ParentEntity.OrganBrain != null))
			{
				return base.MaxDamage;
			}
			return ParentEntity.OrganBrain.DamageState.Stun;
		}
	}

	public EntityDamageState(Thing parent)
		: base(parent)
	{
	}

	public override void OnDamageUpdated()
	{
		if (GameManager.GameState == GameState.Running)
		{
			base.OnDamageUpdated();
		}
	}

	public void CryoHeal(float quantity)
	{
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Starvation);
		base.Heal(quantity);
	}

	public override void Heal(float quantity)
	{
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Starvation);
		base.Heal(quantity);
	}

	public override void HealAll(float minDamageRemaining)
	{
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Starvation).Value, minDamageRemaining), DamageUpdateType.Starvation);
		base.HealAll(minDamageRemaining);
	}

	protected override bool DamageAllowed(DamageUpdateType updateType)
	{
		if (updateType == DamageUpdateType.Starvation)
		{
			return true;
		}
		return base.DamageAllowed(updateType);
	}

	public override void Damage(ChangeDamageType change, float value, DamageUpdateType updateType)
	{
		if (base.Defend)
		{
			return;
		}
		if (change == ChangeDamageType.Increment)
		{
			ParentEntity?.OnDamaged(updateType, value);
		}
		if (updateType == DamageUpdateType.Stun || updateType == DamageUpdateType.Oxygen)
		{
			if (!(ParentEntity?.OrganBrain == null))
			{
				ParentEntity.OrganBrain.DamageState.Damage(change, value, updateType);
			}
		}
		else
		{
			base.Damage(change, value, updateType);
		}
	}
}
