using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class ThingDamageState : IndestructableDamageState
{
	protected bool _isDestroyed;

	private static readonly int _particlesOnDestroy = 5;

	private UniTask _destroyTask;

	private UniTask _decayTask;

	public override bool Indestructable => false;

	public override float TotalRatio => Total / base.MaxDamage;

	public override float TotalRatioClamped => Mathf.Clamp(Total / base.MaxDamage, 0f, 1f);

	public override float TotalRatioClampedUndamaged => 1f - Mathf.Clamp(Total / base.MaxDamage, 0f, 1f);

	public override int TotalRounded => Math.Clamp(Mathf.RoundToInt(Total / base.MaxDamage * 100f), 0, 100);

	public override int DecayRounded => Math.Clamp(Mathf.RoundToInt(Decay / base.MaxDamage * 100f), 0, 100);

	public override float Total => Mathf.Clamp(Brute + Burn + Oxygen + Toxic + Radiation + Hydration + Starvation + Decay, 0f, base.MaxDamage);

	public ThingDamageState(Thing parent)
		: base(parent)
	{
	}

	public ThingDamageState(Thing parent, float maxDamage)
		: base(parent, maxDamage)
	{
	}

	public override void OnDamageUpdated()
	{
		if (!Parent)
		{
			return;
		}
		base.OnDamageUpdated();
		if (!GameManager.IsBatchMode && (bool)Parent.AsDynamicThing && Parent.AsDynamicThing.IsChild && Parent.AsDynamicThing.ParentSlot.Display != null)
		{
			Parent.AsDynamicThing.ParentSlot.RefreshDamage();
		}
		if (Total >= base.MaxDamage)
		{
			if (GameManager.RunSimulation && Decay >= base.MaxDamage && Parent is Item { CanDecay: not false, IsDecayed: false } item && _decayTask.Status != UniTaskStatus.Pending)
			{
				_decayTask = item.SetDecayedAsync();
			}
			if (_destroyTask.Status != UniTaskStatus.Pending)
			{
				_destroyTask = Destroy();
			}
		}
	}

	protected override bool DamageAllowed(DamageUpdateType updateType)
	{
		if (updateType == DamageUpdateType.Burn || updateType == DamageUpdateType.Brute)
		{
			return true;
		}
		return base.DamageAllowed(updateType);
	}

	private async UniTask Destroy()
	{
		await UniTask.SwitchToMainThread();
		await UniTask.DelayFrame(1);
		Structure asStructure = Parent.AsStructure;
		if ((bool)asStructure)
		{
			if (!_isDestroyed && asStructure.IsBroken && asStructure.HasBrokenMesh)
			{
				asStructure.UpdateBuildStateAndVisualizer(asStructure.GetBrokenState(), _particlesOnDestroy);
				asStructure.OnStructureBroken();
				HealAll();
				_isDestroyed = true;
				return;
			}
			EffectManager.CreateDeconstructionEffect(asStructure, _particlesOnDestroy);
			if ((bool)Parent)
			{
				Parent.OnDamageDestroyed();
			}
		}
		if ((bool)Parent)
		{
			Parent.OnDamageDestroyed();
		}
	}

	public override void Heal(float quantity)
	{
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Burn);
		Damage(ChangeDamageType.Decrement, quantity, DamageUpdateType.Brute);
	}

	public override void HealAll(float minDamageRemaining = 0f)
	{
		_isDestroyed = false;
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Burn).Value, minDamageRemaining), DamageUpdateType.Burn);
		Damage(ChangeDamageType.Set, Mathf.Min(GetDamageValue(DamageUpdateType.Brute).Value, minDamageRemaining), DamageUpdateType.Brute);
	}
}
