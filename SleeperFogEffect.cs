using System.Collections.Generic;
using UnityEngine;

public class SleeperFogEffect : MonoBehaviour
{
	public List<Light> FogLights;

	public ParticleSystem FogParticleSystem;

	private ParticleSystem.ColorOverLifetimeModule _fogColorLifetime;

	public void Awake()
	{
		_fogColorLifetime = FogParticleSystem.colorOverLifetime;
		FogParticleSystem.Stop();
	}

	public void StopEffects()
	{
		foreach (Light fogLight in FogLights)
		{
			fogLight.enabled = false;
		}
		FogParticleSystem.Stop();
		FogParticleSystem.Clear();
	}

	public void StartEffects()
	{
		foreach (Light fogLight in FogLights)
		{
			fogLight.enabled = true;
		}
		FogParticleSystem.Play();
	}

	public void SetEffectsColor(Color color)
	{
		ParticleSystem.ColorOverLifetimeModule fogColorLifetime = _fogColorLifetime;
		ParticleSystem.MinMaxGradient color2 = fogColorLifetime.color;
		Gradient gradient = new Gradient();
		gradient.alphaKeys = color2.gradient.alphaKeys;
		gradient.colorKeys = new GradientColorKey[1]
		{
			new GradientColorKey(color, 0f)
		};
		Gradient gradient2 = gradient;
		color2.gradient = gradient2;
		fogColorLifetime.color = color2;
		foreach (Light fogLight in FogLights)
		{
			fogLight.color = color;
		}
	}
}
