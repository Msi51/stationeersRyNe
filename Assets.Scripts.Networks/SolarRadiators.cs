using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Weather;

namespace Assets.Scripts.Networks;

public static class SolarRadiators
{
	private const int MAX_SOLAR_RADIATORS = 1024;

	public static readonly DensePool<ISolarRadiator> AllSolarRadiators = new DensePool<ISolarRadiator>("AllSolarRadiators", 1024);

	public const float SOLAR_PANEL_HEALTH_DAMAGE = 0.005f;

	private static readonly Action<ISolarRadiator> CheckSolarRadiatorWeatherDamageAction = delegate(ISolarRadiator radiator)
	{
		if (radiator != null && !radiator.IsBeingDestroyed)
		{
			if (radiator is SolarPanel solarPanel)
			{
				if (!(solarPanel.WeatherDamageScale <= 0f) && WeatherManager.CurrentEventAffects(solarPanel.Position.y) && !solarPanel.IsBroken && solarPanel.IsExposedToGlobal() && WeatherManager.DamagingRandom.GetChance(0, 10))
				{
					solarPanel.DamageState.Damage(ChangeDamageType.Increment, solarPanel.ThingHealth * (solarPanel.WeatherDamageScale * 0.005f * (float)WeatherManager.CurrentWeatherEvent.WeatherDamageMultiplier), DamageUpdateType.Brute);
				}
			}
			else if (radiator is RadiatorRotatable { WeatherDamageScale: 0 } radiatorRotatable && WeatherManager.CurrentEventAffects(radiatorRotatable.Position.y) && (bool)radiatorRotatable && !radiatorRotatable.IsBroken && radiatorRotatable.IsExposedToGlobal() && WeatherManager.DamagingRandom.GetChance(0, 10) && radiatorRotatable.IsOpen)
			{
				radiatorRotatable.DamageState.Damage(ChangeDamageType.Increment, radiatorRotatable.ThingHealth * (radiatorRotatable.WeatherDamageScale * 0.005f * (float)WeatherManager.CurrentWeatherEvent.WeatherDamageMultiplier), DamageUpdateType.Brute);
			}
		}
	};

	public static void DamageSolarRadiators()
	{
		if (WeatherManager.IsWeatherEventRunning && WeatherManager.CurrentWeatherEvent?.WeatherDamageMultiplier != null && !((float)WeatherManager.CurrentWeatherEvent.WeatherDamageMultiplier <= 0f))
		{
			AllSolarRadiators.ForEach(CheckSolarRadiatorWeatherDamageAction);
		}
	}

	public static void Register(ISolarRadiator radiator)
	{
		if (radiator != null)
		{
			AllSolarRadiators.Add(radiator);
		}
	}

	public static void Deregister(ISolarRadiator radiator)
	{
		if (radiator != null)
		{
			AllSolarRadiators.Remove(radiator);
		}
	}
}
