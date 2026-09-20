using System;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class IndestructableDamageState
{
	[NonSerialized]
	public Thing Parent;

	private readonly ThingDamageValue _burnDamage;

	private readonly ThingDamageValue _bruteDamage;

	private readonly ThingDamageValue _oxygenDamage;

	private readonly ThingDamageValue _hydrationDamage;

	private readonly ThingDamageValue _starvationDamage;

	private readonly ThingDamageValue _toxicDamage;

	private readonly ThingDamageValue _radiationDamage;

	private readonly ThingDamageValue _stunDamage;

	private readonly ThingDamageValue _decayDamage;

	public float MaxDamage { get; }

	public virtual bool Indestructable => true;

	public bool Defend { get; set; }

	public virtual float TotalRatio => Total / MaxDamage;

	public virtual float TotalRatioClamped => 0f;

	public virtual float TotalRatioClampedUndamaged => 1f;

	public virtual float Total => 0f;

	public virtual int TotalRounded => Mathf.RoundToInt(Total);

	public virtual float Stun => _stunDamage.Value;

	public virtual float Oxygen => _oxygenDamage.Value;

	public virtual float Burn => _burnDamage.Value;

	public virtual float Brute => _bruteDamage.Value;

	public virtual float Hydration => _hydrationDamage.Value;

	public virtual float Starvation => _starvationDamage.Value;

	public virtual float Toxic => _toxicDamage.Value;

	public virtual float Radiation => _radiationDamage.Value;

	public virtual float Decay => _decayDamage.Value;

	public virtual int DecayRounded => Mathf.RoundToInt(Decay);

	public DamageUpdateType UpdateType { get; set; }

	public IndestructableDamageState(Thing parent, float maxDamage = 200f)
	{
		Parent = parent;
		MaxDamage = maxDamage;
		_burnDamage = new ThingDamageValue(maxDamage);
		_bruteDamage = new ThingDamageValue(maxDamage);
		_oxygenDamage = new ThingDamageValue(maxDamage);
		_hydrationDamage = new ThingDamageValue(maxDamage);
		_starvationDamage = new ThingDamageValue(maxDamage);
		_toxicDamage = new ThingDamageValue(maxDamage);
		_radiationDamage = new ThingDamageValue(maxDamage);
		_stunDamage = new ThingDamageValue(maxDamage);
		_decayDamage = new ThingDamageValue(maxDamage);
	}

	public virtual void Damage(ChangeDamageType change, float value, DamageUpdateType updateType)
	{
		if (GameManager.RunSimulation && DamageAllowed(updateType) && !Defend && GetDamageValue(updateType).Damage(change, value))
		{
			if (NetworkManager.IsServer)
			{
				UpdateType |= updateType;
			}
			OnDamageUpdated();
		}
	}

	protected virtual bool DamageAllowed(DamageUpdateType updateType)
	{
		return false;
	}

	public ThingDamageValue GetDamageValue(DamageUpdateType damageUpdateType)
	{
		switch (damageUpdateType)
		{
		case DamageUpdateType.All:
			return null;
		case DamageUpdateType.None:
			return null;
		case DamageUpdateType.Burn:
			return _burnDamage;
		case DamageUpdateType.Brute:
			return _bruteDamage;
		case DamageUpdateType.Decay:
			return _decayDamage;
		case DamageUpdateType.Hydration:
			return _hydrationDamage;
		case DamageUpdateType.Oxygen:
			return _oxygenDamage;
		case DamageUpdateType.Radiation:
			return _radiationDamage;
		case DamageUpdateType.Starvation:
			return _starvationDamage;
		case DamageUpdateType.Stun:
			return _stunDamage;
		case DamageUpdateType.Toxic:
			return _toxicDamage;
		default:
		{
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(damageUpdateType);
			ThingDamageValue result = default(ThingDamageValue);
			return result;
		}
		}
	}

	public virtual void Heal(float quantity)
	{
	}

	public virtual void HealAll(float minDamageRemaining = 0f)
	{
	}

	public virtual void OnDamageUpdated()
	{
		if (NetworkManager.IsServer && (bool)Parent)
		{
			Parent.NetworkUpdateFlags |= 4;
		}
	}

	public bool UpdateRequired(DamageUpdateType toCheck)
	{
		return UpdateRequired(UpdateType, toCheck);
	}

	public static bool UpdateRequired(DamageUpdateType updateType, DamageUpdateType toCheck)
	{
		return (updateType & toCheck) != 0;
	}

	public static void Read(RocketBinaryReader reader, IndestructableDamageState damageState)
	{
		DamageUpdateType updateType = (DamageUpdateType)reader.ReadInt16();
		if (damageState == null)
		{
			damageState = new IndestructableDamageState(null);
		}
		damageState.UpdateType = updateType;
		if (UpdateRequired(updateType, DamageUpdateType.Burn))
		{
			float value = reader.ReadSingle();
			damageState._burnDamage.Damage(ChangeDamageType.Set, value);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Brute))
		{
			float value2 = reader.ReadSingle();
			damageState._bruteDamage.Damage(ChangeDamageType.Set, value2);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Oxygen))
		{
			float value3 = reader.ReadSingle();
			damageState._oxygenDamage.Damage(ChangeDamageType.Set, value3);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Hydration))
		{
			float value4 = reader.ReadSingle();
			damageState._hydrationDamage.Damage(ChangeDamageType.Set, value4);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Radiation))
		{
			float value5 = reader.ReadSingle();
			damageState._radiationDamage.Damage(ChangeDamageType.Set, value5);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Starvation))
		{
			float value6 = reader.ReadSingle();
			damageState._starvationDamage.Damage(ChangeDamageType.Set, value6);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Toxic))
		{
			float value7 = reader.ReadSingle();
			damageState._toxicDamage.Damage(ChangeDamageType.Set, value7);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Stun))
		{
			float value8 = reader.ReadSingle();
			damageState._stunDamage.Damage(ChangeDamageType.Set, value8);
		}
		if (UpdateRequired(updateType, DamageUpdateType.Decay))
		{
			float value9 = reader.ReadSingle();
			damageState._decayDamage.Damage(ChangeDamageType.Set, value9);
		}
	}

	public static void Write(RocketBinaryWriter writer, IndestructableDamageState damageState)
	{
		writer.WriteInt16((short)damageState.UpdateType);
		if (damageState.UpdateRequired(DamageUpdateType.Burn))
		{
			writer.WriteSingle(damageState.Burn);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Brute))
		{
			writer.WriteSingle(damageState.Brute);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Oxygen))
		{
			writer.WriteSingle(damageState.Oxygen);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Hydration))
		{
			writer.WriteSingle(damageState.Hydration);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Radiation))
		{
			writer.WriteSingle(damageState.Radiation);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Starvation))
		{
			writer.WriteSingle(damageState.Starvation);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Toxic))
		{
			writer.WriteSingle(damageState.Toxic);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Stun))
		{
			writer.WriteSingle(damageState.Stun);
		}
		if (damageState.UpdateRequired(DamageUpdateType.Decay))
		{
			writer.WriteSingle(damageState.Decay);
		}
		damageState.UpdateType = DamageUpdateType.None;
	}

	public void Copy(IndestructableDamageState damageState)
	{
		_burnDamage.Damage(ChangeDamageType.Set, damageState.Burn);
		_bruteDamage.Damage(ChangeDamageType.Set, damageState.Brute);
		_oxygenDamage.Damage(ChangeDamageType.Set, damageState.Oxygen);
		_hydrationDamage.Damage(ChangeDamageType.Set, damageState.Hydration);
		_radiationDamage.Damage(ChangeDamageType.Set, damageState.Radiation);
		_starvationDamage.Damage(ChangeDamageType.Set, damageState.Starvation);
		_toxicDamage.Damage(ChangeDamageType.Set, damageState.Toxic);
		_stunDamage.Damage(ChangeDamageType.Set, damageState.Stun);
		_decayDamage.Damage(ChangeDamageType.Set, damageState.Decay);
	}

	public void Copy(DamageUpdate damageState)
	{
		_burnDamage.Damage(ChangeDamageType.Set, damageState.Burn);
		_bruteDamage.Damage(ChangeDamageType.Set, damageState.Brute);
		_oxygenDamage.Damage(ChangeDamageType.Set, damageState.Oxygen);
		_hydrationDamage.Damage(ChangeDamageType.Set, damageState.Hydration);
		_radiationDamage.Damage(ChangeDamageType.Set, damageState.Radiation);
		_starvationDamage.Damage(ChangeDamageType.Set, damageState.Starvation);
		_toxicDamage.Damage(ChangeDamageType.Set, damageState.Toxic);
		_stunDamage.Damage(ChangeDamageType.Set, damageState.Stun);
		_decayDamage.Damage(ChangeDamageType.Set, damageState.Decay);
	}
}
