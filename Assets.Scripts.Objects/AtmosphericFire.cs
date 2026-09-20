using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class AtmosphericFire : Fire
{
	public static List<AtmosphericFire> AllAtmosphericFires = new List<AtmosphericFire>();

	public static Dictionary<Atmosphere, AtmosphericFire> AtmosphericFireLookup = new Dictionary<Atmosphere, AtmosphericFire>();

	public static ParticleSystem AtmosphericFireParticleSystem;

	public static ParticleSystem.Particle[] AtmosphereFireParticles;

	public static Transform AtmosphereFireVisualizerTransform;

	public static ParticleSystem.MainModule AtmosphericFireMainModule;

	public static Vector3[] FireParticlePositions;

	public static Vector3[] FireAtmosphereVelocities;

	private static readonly int MaxActiveFireSounds = 24;

	public float LastAudioTime;

	public static readonly int MAXFireParticles = 5000;

	private const float AUDIBLE_SQUARE_DISTANCE = 300f;

	private static float _lastPlayedTime;

	public static int ParticleCount = 0;

	private static readonly float MinCombustionForFlameFX = 5E-05f;

	public Atmosphere Atmosphere { get; }

	public bool IsAudioCoolDown => LastAudioTime + 3f > Time.time;

	public override Vector3 Position => Atmosphere.WorldPosition;

	public override bool IsValid
	{
		get
		{
			if (Atmosphere != null)
			{
				return Atmosphere.Inflamed;
			}
			return false;
		}
	}

	private bool IsAudible
	{
		get
		{
			if (InventoryManager.ParentHuman != null)
			{
				return Vector3.SqrMagnitude(Position - InventoryManager.ParentHuman.Position) < 300f;
			}
			return false;
		}
	}

	public AtmosphericFire(Atmosphere atmosphere)
	{
		Atmosphere = atmosphere;
		_particleData = GetFlameParticleData(atmosphere);
	}

	public static void Initialise(ParticleSystem atmosphereFireParticleSystem)
	{
		AtmosphericFireParticleSystem = atmosphereFireParticleSystem;
		AtmosphereFireVisualizerTransform = atmosphereFireParticleSystem.transform;
		AtmosphericFireMainModule = atmosphereFireParticleSystem.main;
		AtmosphericFireMainModule.maxParticles = MAXFireParticles;
		AtmosphereFireParticles = new ParticleSystem.Particle[MAXFireParticles];
		FireParticlePositions = new Vector3[MAXFireParticles];
		FireAtmosphereVelocities = new Vector3[MAXFireParticles];
	}

	public static void Register(AtmosphericFire atmosphericFire)
	{
		if (!atmosphericFire.IsValid)
		{
			return;
		}
		lock (AtmosphericFireLookup)
		{
			if (AtmosphericFireLookup.TryAdd(atmosphericFire.Atmosphere, atmosphericFire))
			{
				AllAtmosphericFires.Add(atmosphericFire);
			}
			if (atmosphericFire.IsEmitting())
			{
				atmosphericFire.PlayCatchFireSound();
			}
		}
	}

	private void PlayCatchFireSound()
	{
		if (IsAudible)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.AtmosphereFireStart, Position, _particleData.Size * 1.5f);
		}
	}

	private static bool ShouldPlayFireSound(float currentTime)
	{
		return currentTime > _lastPlayedTime + 0.1f;
	}

	private static void PlayFireSound(AtmosphericFire fire)
	{
		Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.AtmosphereFire, fire.Position);
		_lastPlayedTime = (fire.LastAudioTime = Time.time);
	}

	public static void DeRegister(Atmosphere atmosphere)
	{
		lock (AtmosphericFireLookup)
		{
			if (AtmosphericFireLookup.TryGetValue(atmosphere, out var value))
			{
				AllAtmosphericFires.Remove(value);
				AtmosphericFireLookup.Remove(atmosphere);
			}
		}
	}

	public static void SetFlameParticleValues(Atmosphere atmosphere)
	{
		if (AtmosphericFireLookup.TryGetValue(atmosphere, out var value))
		{
			value._particleData = GetFlameParticleData(atmosphere);
		}
	}

	public static void EmitAtmosphericFireParticles()
	{
		if (AllAtmosphericFires.Count <= 0)
		{
			ParticleCount = 0;
			return;
		}
		float time = Time.time;
		for (int num = AllAtmosphericFires.Count - 1; num >= 0; num--)
		{
			try
			{
				AtmosphericFire atmosphericFire = AllAtmosphericFires[num];
				if (atmosphericFire != null && atmosphericFire.IsEmitting())
				{
					AtmosphereFireVisualizerTransform.position = atmosphericFire.EmissionPosition();
					atmosphericFire.PrepareEmitterState();
					AtmosphericFireParticleSystem.Emit(atmosphericFire._particleData.NumberOfEmitters);
					if (ShouldPlayFireSound(time) && !atmosphericFire.IsAudioCoolDown && atmosphericFire.IsAudible)
					{
						PlayFireSound(atmosphericFire);
					}
				}
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
		ParticleCount = AtmosphericFireParticleSystem.particleCount;
	}

	public static FlameParticleData GetFlameParticleData(Atmosphere atmosphere)
	{
		float num = 0f;
		if (atmosphere.Temperature > Chemistry.Temperature.TwentyDegrees)
		{
			num = Mathf.Clamp((atmosphere.Temperature - Chemistry.Temperature.TwentyDegrees).ToFloat() / (new TemperatureKelvin(5000.0) - Chemistry.Temperature.TwentyDegrees).ToFloat(), 0f, 0.5f);
		}
		float t = (float)ParticleCount / (float)MAXFireParticles;
		float num2 = Mathf.Clamp(atmosphere.FuelBurnedRatio * 100f + num, 0.01f, 1f);
		Vector3 one = Vector3.one;
		int a = Mathf.Clamp(Mathf.FloorToInt(num2 * 10f), 1, 5);
		int b = Mathf.RoundToInt(Mathf.Lerp(5f, 1f, t));
		a = Mathf.Min(a, b);
		float num3 = num;
		float alpha = Mathf.Lerp(0.25f, 0.75f, num3);
		return new FlameParticleData(AtmosphericsManager.Instance.TemperatureGradient.Evaluate(num3).SetAlpha(alpha), num2, one, a);
	}

	public Vector3 EmissionPosition()
	{
		return Position + UnityEngine.Random.insideUnitSphere * 0.75f;
	}

	public bool IsEmitting()
	{
		if (IsValid && Atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && Atmosphere.SquareDistanceToPlayer <= 2500f)
		{
			return Atmosphere.FuelBurnedRatio > MinCombustionForFlameFX;
		}
		return false;
	}

	public override void PrepareEmitterState()
	{
		if (!GameManager.IsBatchMode)
		{
			AtmosphericFireMainModule.startColor = _particleData.Color;
			AtmosphericFireMainModule.startSize = new ParticleSystem.MinMaxCurve(0.5f * _particleData.Size, _particleData.Size);
		}
	}

	public static void Clear()
	{
		lock (AtmosphericFireLookup)
		{
			AllAtmosphericFires.Clear();
			AtmosphericFireLookup.Clear();
		}
	}
}
