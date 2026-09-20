using Assets.Scripts;
using Assets.Scripts.GridSystem;
using UnityEngine;
using Weather;

public class RainMaterialController : GameBase
{
	[SerializeField]
	private ParticleSystem _rainParticles;

	private ParticleSystemRenderer _renderer;

	private void Awake()
	{
		_renderer = _rainParticles.gameObject.GetComponent<ParticleSystemRenderer>();
	}

	private void Update()
	{
		if (GameManager.GameState == GameState.Running && WeatherManager.CurrentWeatherEvent != null)
		{
			Color.RGBToHSV(WeatherManager.CurrentWeatherEvent.Fog.FogColor, out var H, out var S, out var V);
			V = Mathf.Lerp(0f, V, OrbitalSimulation.SolarIntensity);
			Color value = Color.HSVToRGB(H, S, V);
			_renderer.material.SetColor("_EmissionColor", value);
		}
	}
}
