using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using Objects.Electrical;

namespace Assets.Scripts.Objects;

public class Organ : Item, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable
{
	public virtual Entity ParentEntity
	{
		get
		{
			Slot.Class? obj = base.ParentSlot?.Type;
			if (!obj.HasValue || obj != Slot.Class.Organ)
			{
				return null;
			}
			return base.ParentSlot?.Parent as Entity;
		}
	}

	public virtual DynamicBodyBag ParentBodyBag => base.ParentSlot?.Parent as DynamicBodyBag;

	public override bool IsBurnable => false;

	public override void ApplyLavaDamage()
	{
	}

	public override void OnDamageDestroyed()
	{
		if (GameManager.RunSimulation && (bool)ParentEntity)
		{
			ParentEntity.DamageState.Damage(ChangeDamageType.Increment, 100f, DamageUpdateType.Brute);
			ParentEntity.State = EntityState.Dead;
		}
	}

	public override void InitializeDamageState()
	{
		DamageState = new OrganicDamageState(this);
	}

	public virtual string ToConsoleString()
	{
		return string.Empty;
	}

	public virtual void OnLifeTick()
	{
	}
}
