using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using Networks;
using Sound;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class PipeLeak : IEquatable<PipeLeak>
{
	public static List<PipeLeak> AllPipeLeaks = new List<PipeLeak>();

	public static Dictionary<INetworkedAtmospherics, PipeLeak> LookUp = new Dictionary<INetworkedAtmospherics, PipeLeak>();

	public static ParticleSystem PipeLeakParticleSystem;

	public static Transform PipeLeakVisualizerTransform;

	public static ParticleSystem.MainModule PipeLeakMainModule;

	private Vector3 _positionOffset;

	private PooledAudioSource _leakSound;

	private const int MAX_PARTICLES = 1000;

	private const float PRESSURE_TO_AUDIO_PITCH = 1000f;

	private INetworkedAtmospherics LeakSource { get; }

	private Vector3 Position => LeakSource.GetAsThing.Position + _positionOffset;

	private Quaternion Rotation => LeakSource.GetAsThing.Rotation;

	public bool IsValid
	{
		get
		{
			if (LeakSource != null)
			{
				return LeakSource.IsBurst != PipeBurst.None;
			}
			return false;
		}
	}

	public bool Equals(PipeLeak other)
	{
		if (LeakSource != null && other?.LeakSource != null)
		{
			return LeakSource.ReferenceId == other.LeakSource.ReferenceId;
		}
		return false;
	}

	public PipeLeak(INetworkedAtmospherics leakSource, Vector3 positionOffset)
	{
		_positionOffset = positionOffset;
		LeakSource = leakSource;
	}

	public static void Initialise(ParticleSystem pipeLeakSystem)
	{
		PipeLeakParticleSystem = pipeLeakSystem;
		PipeLeakVisualizerTransform = pipeLeakSystem.transform;
		PipeLeakMainModule = pipeLeakSystem.main;
		PipeLeakMainModule.maxParticles = 1000;
	}

	public static void Register(PipeLeak pipeLeak)
	{
		if (!pipeLeak.IsValid)
		{
			return;
		}
		lock (LookUp)
		{
			if (LookUp.TryAdd(pipeLeak.LeakSource, pipeLeak))
			{
				AllPipeLeaks.Add(pipeLeak);
			}
		}
	}

	public static void DeRegister(INetworkedAtmospherics atmospherics)
	{
		lock (LookUp)
		{
			if (LookUp.TryGetValue(atmospherics, out var value))
			{
				AllPipeLeaks.Remove(value);
				LookUp.Remove(atmospherics);
				if (value?._leakSound != null)
				{
					value._leakSound.Stop(immediate: true);
				}
			}
		}
	}

	public Vector3 EmissionPosition()
	{
		return Position;
	}

	public static void Clear()
	{
		lock (LookUp)
		{
			LookUp.Clear();
			AllPipeLeaks.Clear();
		}
	}

	private bool IsEmitting(out PressurekPa pressureDelta)
	{
		pressureDelta = PressurekPa.Zero;
		if (!IsValid || !GridController.World.CanContainAtmos(LeakSource.WorldGrid))
		{
			return false;
		}
		Atmosphere atmosphere = (LeakSource.StructureNetwork as AtmosphericsNetwork)?.Atmosphere;
		Atmosphere atmosphere2 = AtmosphericsController.World.SampleGlobalAtmosphere(LeakSource.WorldGrid);
		if (atmosphere2 != null && atmosphere != null)
		{
			pressureDelta = atmosphere.PressureGassesAndLiquids - atmosphere2.PressureGassesAndLiquids;
			return atmosphere.PressureGassesAndLiquids > atmosphere2.PressureGassesAndLiquids + Chemistry.OneAtmosphere;
		}
		return false;
	}

	public static void EmitAtLocation(Vector3 position, Vector3 rotation, int numberOfParticles = 1)
	{
		ParticleSystem.ShapeModule shape = PipeLeakParticleSystem.shape;
		shape.position = position;
		shape.rotation = rotation;
		PipeLeakParticleSystem.Emit(numberOfParticles);
	}

	public static void Emit()
	{
		if (AllPipeLeaks.Count <= 0)
		{
			return;
		}
		for (int num = AllPipeLeaks.Count - 1; num >= 0; num--)
		{
			try
			{
				PipeLeak pipeLeak = AllPipeLeaks[num];
				if (pipeLeak == null || !pipeLeak.IsEmitting(out var pressureDelta))
				{
					if (pipeLeak?._leakSound != null)
					{
						pipeLeak._leakSound.Stop();
					}
				}
				else
				{
					if (pipeLeak._leakSound == null && pipeLeak.LeakSource is IAudioParent parent)
					{
						pipeLeak._leakSound = Singleton<AudioManager>.Instance.PlayAudioClipsData(parent, Defines.Sounds.PipeLeakHash, Vector3.zero);
					}
					if (pipeLeak._leakSound != null)
					{
						pipeLeak._leakSound.GameAudioSource.LerpPitchMultiplier(Defines.Sounds.PipeLeakHash, Mathf.Lerp(0.5f, 1f, pressureDelta.ToFloat() / 1000f), Time.fixedDeltaTime);
					}
					ParticleSystem.ShapeModule shape = PipeLeakParticleSystem.shape;
					shape.position = pipeLeak.Position;
					shape.rotation = pipeLeak.Rotation.eulerAngles;
					PipeLeakParticleSystem.Emit(1);
				}
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
	}
}
