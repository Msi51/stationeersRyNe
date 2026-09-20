using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class AtmosphericFog : IEquatable<AtmosphericFog>
{
	public static List<AtmosphericFog> AllAtmosphericFogs = new List<AtmosphericFog>();

	public static Dictionary<Atmosphere, AtmosphericFog> AtmosphericFogLookup = new Dictionary<Atmosphere, AtmosphericFog>();

	public static ParticleSystem AtmosphericFogParticleSystem;

	public static ParticleSystem.Particle[] AtmosphereFogParticles;

	public static Transform AtmosphereFogVisualizerTransform;

	public static ParticleSystem.MainModule AtmosphericFogMainModule;

	public static Vector3[] FogParticlePositions;

	public static Vector3[] FogAtmosphereVelocities;

	private float _lastEmitTime;

	private const float COOLDOWN_TIME = 0.2f;

	public static readonly int MAXFogParticles = 2000;

	private const float MAX_DISTANCE_TO_RENDER = 25f;

	private const float FOG_VISUALIZER_MAX_RENDER_DISTANCE_SQUARED = 625f;

	private static int _lastIndex = -1;

	private const float MAX_EMIT_PER_TICK = 30f;

	private Atmosphere Atmosphere { get; }

	private Vector3 Position => Atmosphere.WorldPosition;

	private bool IsEmitCooldown => _lastEmitTime + 0.2f > Time.fixedTime;

	public bool IsValid()
	{
		AtmosphereHelper.AtmosphereMode? atmosphereMode = Atmosphere?.Mode;
		if (atmosphereMode.HasValue && atmosphereMode.GetValueOrDefault() == AtmosphereHelper.AtmosphereMode.World)
		{
			return Atmosphere.Condensation;
		}
		return false;
	}

	public AtmosphericFog(Atmosphere atmosphere)
	{
		Atmosphere = atmosphere;
		_lastEmitTime = 0f;
	}

	public bool Equals(AtmosphericFog other)
	{
		if (Atmosphere != null && other?.Atmosphere != null)
		{
			return Atmosphere.ReferenceId == other.Atmosphere.ReferenceId;
		}
		return false;
	}

	public static void Initialise(ParticleSystem atmosphereFogParticleSystem)
	{
		AtmosphericFogParticleSystem = atmosphereFogParticleSystem;
		AtmosphereFogVisualizerTransform = atmosphereFogParticleSystem.transform;
		AtmosphericFogMainModule = atmosphereFogParticleSystem.main;
		AtmosphericFogMainModule.maxParticles = MAXFogParticles;
		AtmosphereFogParticles = new ParticleSystem.Particle[MAXFogParticles];
		FogParticlePositions = new Vector3[MAXFogParticles];
		FogAtmosphereVelocities = new Vector3[MAXFogParticles];
	}

	public static void Register(AtmosphericFog atmosphericFog)
	{
		lock (AtmosphericFogLookup)
		{
			if (AtmosphericFogLookup.TryAdd(atmosphericFog.Atmosphere, atmosphericFog))
			{
				AllAtmosphericFogs.Add(atmosphericFog);
			}
		}
	}

	public static void DeRegister(Atmosphere atmosphere)
	{
		lock (AtmosphericFogLookup)
		{
			if (AtmosphericFogLookup.TryGetValue(atmosphere, out var value))
			{
				AllAtmosphericFogs.Remove(value);
				AtmosphericFogLookup.Remove(atmosphere);
			}
		}
	}

	public Vector3 EmissionPosition()
	{
		return Position + UnityEngine.Random.insideUnitSphere * 0.75f;
	}

	public bool IsEmitting()
	{
		if (IsValid())
		{
			return Atmosphere.SquareDistanceToPlayer <= 625f;
		}
		return false;
	}

	public static void Clear()
	{
		lock (AtmosphericFogLookup)
		{
			AllAtmosphericFogs.Clear();
			AtmosphericFogLookup.Clear();
		}
	}

	public static void EmitAtmosphericFogParticles()
	{
		if (AllAtmosphericFogs.Count <= 0)
		{
			return;
		}
		int num = MAXFogParticles - AtmosphericFogParticleSystem.particleCount;
		int num2 = ((_lastIndex != -1) ? _lastIndex : (AllAtmosphericFogs.Count - 1));
		if (num2 > AllAtmosphericFogs.Count - 1)
		{
			num2 = AllAtmosphericFogs.Count - 1;
		}
		int num3 = 0;
		for (int num4 = num2; num4 >= 0; num4--)
		{
			try
			{
				if (num <= 0 || (float)num3 >= 30f)
				{
					_lastIndex = num4;
					break;
				}
				_lastIndex = num4;
				AtmosphericFog atmosphericFog = AllAtmosphericFogs[num4];
				if (atmosphericFog.IsEmitting() && !atmosphericFog.IsEmitCooldown)
				{
					AtmosphereFogVisualizerTransform.position = atmosphericFog.EmissionPosition();
					AtmosphericFogParticleSystem.Emit(1);
					atmosphericFog._lastEmitTime = Time.fixedTime;
					num--;
					num3++;
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				_lastIndex = num4;
			}
			catch (IndexOutOfRangeException)
			{
				_lastIndex = num4;
			}
		}
		if (num > 0)
		{
			_lastIndex = -1;
		}
	}
}
