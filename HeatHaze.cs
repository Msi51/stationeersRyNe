using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

public class HeatHaze : GameBase
{
	public static HeatHaze Instance;

	public ParticleSystem ParticleSystem;

	private ParticleSystem.EmissionModule _emissionModule;

	private ParticleSystem.MainModule _mainModule;

	private static readonly TemperatureKelvin StartTemperature = new TemperatureKelvin(354.15);

	private static readonly TemperatureKelvin FullTemperature = new TemperatureKelvin(774.15);

	public void Awake()
	{
		Instance = this;
		_emissionModule = ParticleSystem.emission;
		_mainModule = ParticleSystem.main;
	}

	public static void Clear()
	{
		if (Instance != null)
		{
			Instance._emissionModule.enabled = false;
		}
	}

	public static void Apply(Atmosphere parentWorldAtmosphere)
	{
		float num = RocketMath.MapToScale(StartTemperature.ToFloat(), FullTemperature.ToFloat(), 0f, 1f, parentWorldAtmosphere.Temperature.ToFloat());
		float num2 = RocketMath.MapToScale(0f, 20f, 0f, 1f, parentWorldAtmosphere.PressureGasses.ToFloat());
		float t = num * num2;
		Instance._mainModule.simulationSpeed = Mathf.Lerp(0.01f, 1.5f, t);
		Instance._emissionModule.rateOverTime = Mathf.Lerp(0.01f, 50f, t);
		if (!Instance._emissionModule.enabled)
		{
			Instance._emissionModule.enabled = true;
		}
	}
}
