using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Lander : DraggableThing, ITrackable
{
	public override float LavaDamage => 0f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			ITrackable.Trackables.Add(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ITrackable.Trackables.Remove(this);
	}

	public override bool CanBeWeathered()
	{
		if ((float)DifficultySetting.Current.WeatherLanderDamageRate <= float.Epsilon)
		{
			return false;
		}
		return base.CanBeWeathered();
	}

	public override Vector3 GetStormWindVector()
	{
		return base.GetStormWindVector() * DifficultySetting.Current.WeatherLanderDamageRate;
	}

	public override void DoWeatherDamage(float damageMultiplier)
	{
		DamageState.Damage(ChangeDamageType.Increment, ThingHealth * (WeatherDamageScale * (float)DifficultySetting.Current.WeatherLanderDamageRate * 0.03f * damageMultiplier), DamageUpdateType.Brute);
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		saveData.WorldPosition.y += 1f;
		saveData.WorldRotation.y = 180f;
		base.DeserializeSave(saveData);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		foreach (WeatherEvent weatherEvent in WorldSetting.Current.WeatherEvents)
		{
			if ((float)weatherEvent.WeatherDamageMultiplier > 0f && (float)DifficultySetting.Current.WeatherLanderDamageRate > 0f)
			{
				extendedText.AppendLine(GameStrings.ThingCanBeDestroyedByWeather.AsString(ToTooltip(), weatherEvent.ToTooltip()));
			}
		}
		return extendedText;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		foreach (Slot slot in newChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: false, hideOnPlayer: true, isRecursive: true);
			}
		}
		DynamicGasCanister dynamicGasCanister = newChild as DynamicGasCanister;
		if ((bool)dynamicGasCanister)
		{
			dynamicGasCanister.MainCollider.enabled = false;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		foreach (Slot slot in previousChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: true, hideOnPlayer: false, isRecursive: true);
			}
		}
		DynamicGasCanister dynamicGasCanister = previousChild as DynamicGasCanister;
		if ((bool)dynamicGasCanister)
		{
			dynamicGasCanister.MainCollider.enabled = true;
		}
	}
}
