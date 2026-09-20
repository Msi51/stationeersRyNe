using System;
using UnityEngine;

namespace Assets.Scripts.Weather;

public class WeatherEventParticlePreset : GameBase
{
	public string Id;

	public WeatherEventPresetSettings LowSetting;

	public WeatherEventPresetSettings MediumSetting;

	public WeatherEventPresetSettings HighSetting;

	private int _hash;

	public int Hash => _hash;

	public void Initialize()
	{
		_hash = Animator.StringToHash(Id);
	}

	public WeatherEventPresetSettings GetSetting(string setting)
	{
		if (!Enum.TryParse<WeatherQuality>(setting, out var result))
		{
			return MediumSetting;
		}
		return result switch
		{
			WeatherQuality.Low => LowSetting, 
			WeatherQuality.Medium => MediumSetting, 
			WeatherQuality.High => HighSetting, 
			_ => MediumSetting, 
		};
	}
}
