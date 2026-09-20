using System;
using UnityEngine;

namespace Assets.Scripts.Weather;

[Serializable]
public class WeatherEventPresetSettings
{
	[Tooltip("Particle system used to display nearby cells. This should be high detail.\nIf this is null, far effect will be used instead.")]
	public ParticleSystem NearDetailEffectPrefab;

	[Tooltip("Particle system used to display farther away cells. This should obscure vision.\nMust not be null!")]
	public ParticleSystem FarDetailEffectPrefab;

	[Tooltip("The number of layers/shells to emit particles at starting from the centre outwards")]
	public int NumParticleEmissionLayers;
}
