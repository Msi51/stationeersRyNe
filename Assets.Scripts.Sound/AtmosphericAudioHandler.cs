using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects;
using Sound;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Sound;

public class AtmosphericAudioHandler : MonoBehaviour
{
	public static AtmosphericAudioHandler Instance;

	[SerializeField]
	private StaticAudioSource _windLocalAudio;

	[SerializeField]
	private StaticAudioSource _windLocalLightAudio;

	[SerializeField]
	private StaticAudioSource _windLocalHeavyAudio;

	[SerializeField]
	private StaticAudioSource _stormSheltered;

	[SerializeField]
	private StaticAudioSource _storm;

	public float WindThreshold = 1f;

	public float WindMax = 400f;

	private static readonly float MovementWindEffectMultiplier = 0.5f;

	public AnimationCurve LightWindCurve;

	public AnimationCurve WindCurve;

	public AnimationCurve HeavyWindCurve;

	public float NeighbourAtmosWeighting = 1.5f;

	public float LocalAtmosWeighting = 2f;

	private float _velocitySmoothed;

	private float _windMagnitude;

	private float _windSmoothed;

	private static readonly float LerpSpeedUp = 15f;

	private static readonly float LerpSpeedDown = 1.5f;

	private static readonly PressurekPa MinimumLocalAtmosPressure = new PressurekPa(1.0);

	private static readonly float MovementWindVelocityThreshold = 4.1f;

	private static readonly float MaxVelocity = 20f;

	private static readonly float SmoothSpeed = 2f;

	public float StormWindVolumeMultiplier = 0.75f;

	public float InStormWindModifier;

	public float GlobalWindMultiplier = 130f;

	public float GlobalWindLightMultiplier = 0.5f;

	private readonly int _stormHash = Animator.StringToHash("Storm");

	private readonly int _stormShelteredHash = Animator.StringToHash("StormSheltered");

	private readonly int _windLocalHash = Animator.StringToHash("WindLocal");

	private readonly int _windLocalLightHash = Animator.StringToHash("WindLocalLight");

	private readonly int _windLocalHeavyHash = Animator.StringToHash("WindLocalHeavy");

	private bool _playWindSounds;

	private bool _playWeatherSounds;

	public bool DebugDisablePressureSounds;

	public List<INetworkedAtmospherics> StressedPipes = new List<INetworkedAtmospherics>();

	public List<Wall> StressedWalls = new List<Wall>();

	public SoundEmitterSlot[] StressedEmitterSlots = new SoundEmitterSlot[MaxStressedEmitters];

	private List<int> _availableIndices = new List<int>();

	public static readonly int MaxStressedEmitters = 24;

	public static readonly int MaxStressedWalls = 16;

	public int MaxEmittersPerGrid = 1;

	public static readonly int PipeStressHash = Animator.StringToHash("PipeStress");

	public static readonly int MetalWallStressHash = Animator.StringToHash("MetalWallStress");

	public static readonly int CompositeWallStressHash = Animator.StringToHash("CompositeWallStress");

	public static readonly int GlassWallStressHash = Animator.StringToHash("GlassWallStress");

	private static readonly float _audibleDistance = 400f;

	public bool PlayWindSounds
	{
		get
		{
			return _playWindSounds;
		}
		set
		{
			if (value == _playWindSounds)
			{
				return;
			}
			if (value)
			{
				_ = InventoryManager.ParentHuman.NetworkId;
				_windLocalAudio.Play(_windLocalHash);
				_windLocalLightAudio.Play(_windLocalLightHash);
				_windLocalHeavyAudio.Play(_windLocalHeavyHash);
			}
			else
			{
				if (_windLocalAudio != null)
				{
					_windLocalAudio.GameAudioSource?.Stop(_windLocalHash);
				}
				if (_windLocalLightAudio != null)
				{
					_windLocalLightAudio.GameAudioSource?.Stop(_windLocalLightHash);
				}
				if (_windLocalHeavyAudio != null)
				{
					_windLocalHeavyAudio.GameAudioSource?.Stop(_windLocalHeavyHash);
				}
			}
			_playWindSounds = value;
		}
	}

	public bool PlayWeatherSounds
	{
		get
		{
			return _playWeatherSounds;
		}
		set
		{
			if (value != _playWeatherSounds)
			{
				if (value)
				{
					_storm.Play(_stormHash);
					_stormSheltered.Play(_stormShelteredHash);
				}
				else
				{
					_storm.Stop();
					_stormSheltered.Stop();
				}
				_playWeatherSounds = value;
			}
		}
	}

	public void Initialize()
	{
		Instance = this;
		WindAudioUpdate().Forget();
		PressureAudioUpdate().Forget();
	}

	public async UniTaskVoid WindAudioUpdate()
	{
		while (true)
		{
			await UniTask.Delay(33);
			try
			{
				ManageWindAudio();
			}
			catch (Exception)
			{
			}
		}
	}

	private void ManageWindAudio()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		if (GameManager.GameState != GameState.Running || InventoryManager.Parent == null)
		{
			PlayWindSounds = false;
			PlayWeatherSounds = false;
			return;
		}
		Atmosphere worldAtmosphere = InventoryManager.Parent.WorldAtmosphere;
		if (worldAtmosphere == null || worldAtmosphere.PressureGasses < MinimumLocalAtmosPressure)
		{
			PlayWindSounds = false;
			PlayWeatherSounds = false;
			return;
		}
		if (CameraController.IsUnderWater)
		{
			PlayWindSounds = false;
			PlayWeatherSounds = false;
			return;
		}
		bool flag = WeatherManager.CurrentEventAffects(InventoryManager.ParentPosition.y) && WeatherManager.CurrentWeatherEvent.WindSound;
		bool flag2 = InventoryManager.Parent.Room == null && flag;
		PlayWeatherSounds = flag;
		if (PlayWeatherSounds)
		{
			float num = (flag2 ? 1f : 0f);
			float volumeMultiplier = 1f - num;
			_storm.GameAudioSource.LerpVolumeMultiplier(num, 0.033f);
			_stormSheltered.GameAudioSource.LerpVolumeMultiplier(volumeMultiplier, 0.033f);
		}
		float num2 = Vector3.Magnitude(worldAtmosphere.Direction) * LocalAtmosWeighting;
		if (!GameManager.RunSimulation)
		{
			Span<Grid3> bufClosed = stackalloc Grid3[6];
			Span<Grid3> bufOpen = stackalloc Grid3[6];
			worldAtmosphere.GetOpenAirNeighbors(bufClosed, bufOpen);
		}
		lock (worldAtmosphere.OpenNeighbors)
		{
			int count = worldAtmosphere.OpenNeighbors.Count;
			int num3 = ((count < 1) ? 1 : count);
			for (int i = 0; i < count; i++)
			{
				Grid3 position = worldAtmosphere.OpenNeighbors[i];
				Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(new WorldGrid(position));
				if (atmosphere != null)
				{
					float num4 = Vector3.Magnitude(atmosphere.Direction) * NeighbourAtmosWeighting;
					num2 += num4;
				}
			}
			num2 /= (float)num3;
		}
		num2 += (flag2 ? float.Epsilon : 0f);
		float num5 = 0f;
		if (!flag2 && !EnvironmentalAudioHandler.Enclosed)
		{
			num5 = WindTurbineGenerator.GetNoise(GlobalWindMultiplier);
		}
		num2 = Mathf.Clamp(num2, 0f, WindMax);
		if (float.IsNaN(num2))
		{
			num2 = 0f;
		}
		float num6 = ((num2 > _windMagnitude) ? LerpSpeedUp : LerpSpeedDown);
		_windMagnitude = num2;
		_windSmoothed = Mathf.Lerp(_windSmoothed, num2, 33f * num6);
		_velocitySmoothed = Mathf.Lerp(_velocitySmoothed, InventoryManager.Parent.VelocityMagnitude, 33f * SmoothSpeed);
		PlayWindSounds = _windSmoothed >= WindThreshold || _velocitySmoothed > MovementWindVelocityThreshold || num5 > WindThreshold;
		if (PlayWindSounds)
		{
			float num7 = (flag2 ? StormWindVolumeMultiplier : 1f);
			float num8 = (Mathf.Clamp(_velocitySmoothed, MovementWindVelocityThreshold, MaxVelocity) - MovementWindVelocityThreshold) / (MaxVelocity - MovementWindVelocityThreshold) * WindMax * MovementWindEffectMultiplier;
			_windLocalLightAudio?.GameAudioSource?.SetVolumeMultiplier(_windLocalLightHash, Mathf.Lerp(0f, 0.4f, num7 * LightWindCurve.Evaluate(_windSmoothed + num5 * GlobalWindLightMultiplier)));
			_windLocalLightAudio?.GameAudioSource?.SetPitchMultiplier(_windLocalLightHash, Mathf.Lerp(0.75f, 1f, WindCurve.Evaluate(_windSmoothed)));
			_windLocalAudio?.GameAudioSource?.SetVolumeMultiplier(_windLocalHash, Mathf.Lerp(0f, 1f, num7 * WindCurve.Evaluate(Mathf.Clamp(_windSmoothed + num8 + num5, 0f, WindMax))));
			_windLocalAudio?.GameAudioSource?.SetPitchMultiplier(_windLocalHash, Mathf.Lerp(0.7f, 1f, WindCurve.Evaluate(Mathf.Clamp(_windSmoothed + num8 + num5, 0f, WindMax))));
			_windLocalHeavyAudio?.GameAudioSource?.SetVolumeMultiplier(_windLocalHeavyHash, Mathf.Lerp(0f, 1f, num7 * HeavyWindCurve.Evaluate(_windSmoothed)));
			_windLocalHeavyAudio?.GameAudioSource?.SetPitchMultiplier(_windLocalHeavyHash, Mathf.Lerp(0.5f, 1f, HeavyWindCurve.Evaluate(_windSmoothed)));
		}
	}

	public async UniTaskVoid PressureAudioUpdate()
	{
		while (true)
		{
			await UniTask.Delay(300);
			ManagePressureAudio();
		}
	}

	private void ManagePressureAudio()
	{
		_availableIndices.Clear();
		for (int i = 0; i < StressedEmitterSlots.Length; i++)
		{
			if (StressedEmitterSlots[i].Thing == null || StressedEmitterSlots[i].IsFree)
			{
				_availableIndices.Add(i);
				StressedEmitterSlots[i].CoolDownTime = 0f;
				StressedEmitterSlots[i].Thing = null;
				StressedEmitterSlots[i].EmitType = SoundEmitterSlot.EmitterType.Default;
			}
		}
		if (_availableIndices.Count == 0)
		{
			return;
		}
		lock (StressedPipes)
		{
			for (int num = StressedPipes.Count - 1; num >= 0; num--)
			{
				if (StressedPipes[num] == null || StressedPipes[num].IsBurst != PipeBurst.None)
				{
					StressedPipes.RemoveAt(num);
				}
			}
			StressedPipes.Sort((INetworkedAtmospherics a, INetworkedAtmospherics b) => b.GetAsThing.SqDistanceFromListenerComparator(a.GetAsThing));
			for (int num2 = 0; num2 < StressedPipes.Count && IsWithinAudibleDistance(StressedPipes[num2].GetAsThing); num2++)
			{
				if (!Grid3HasMaxEmitters(StressedPipes[num2].GetAsThing, SoundEmitterSlot.EmitterType.Pipe) && !IsAlreadyEmmiting(StressedPipes[num2].GetAsThing))
				{
					int num3 = _availableIndices.Count - 1;
					if (num3 < 0)
					{
						return;
					}
					PlayStressedPipeSound(StressedPipes[num2].GetAsThing, _availableIndices[num3]);
					_availableIndices.RemoveAt(num3);
				}
			}
		}
		lock (StressedWalls)
		{
			for (int num4 = StressedWalls.Count - 1; num4 >= 0; num4--)
			{
				if (StressedWalls[num4] == null || StressedWalls[num4].DamageState.TotalRatio <= 0f)
				{
					StressedWalls.RemoveAt(num4);
				}
			}
			StressedWalls.Sort((Wall a, Wall b) => b.SqDistanceFromListenerComparator(a));
			for (int num5 = 0; num5 < StressedWalls.Count && IsWithinAudibleDistance(StressedWalls[num5]); num5++)
			{
				if (!Grid3HasMaxEmitters(StressedWalls[num5], SoundEmitterSlot.EmitterType.Wall) && !IsAlreadyEmmiting(StressedWalls[num5]))
				{
					int num6 = _availableIndices.Count - 1;
					if (num6 < MaxStressedEmitters - MaxStressedWalls)
					{
						break;
					}
					PlayStressedWallSound(StressedWalls[num5], _availableIndices[num6]);
					_availableIndices.RemoveAt(num6);
				}
			}
		}
	}

	private bool Grid3HasMaxEmitters(Thing thing, SoundEmitterSlot.EmitterType emitType)
	{
		int num = 0;
		SoundEmitterSlot[] stressedEmitterSlots = StressedEmitterSlots;
		for (int i = 0; i < stressedEmitterSlots.Length; i++)
		{
			SoundEmitterSlot soundEmitterSlot = stressedEmitterSlots[i];
			if (!(soundEmitterSlot.Thing == null) && !soundEmitterSlot.IsFree && thing.WorldGrid == soundEmitterSlot.Thing.WorldGrid && soundEmitterSlot.EmitType >= emitType)
			{
				num++;
				if (num == MaxEmittersPerGrid)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void PlayStressedPipeSound(Thing emittingThing, int stressedEmittersArrayIndex)
	{
		Singleton<AudioManager>.Instance.PlayAudioClipsData(emittingThing, PipeStressHash, Vector3.zero);
		StressedEmitterSlots[stressedEmittersArrayIndex].UpdateEmitterData(emittingThing, SoundEmitterSlot.EmitterType.Pipe);
	}

	private void PlayStressedWallSound(Wall emittingWall, int stressedEmittersArrayIndex)
	{
		int num = -1;
		num = emittingWall.WallMaterialType switch
		{
			Wall.WallMaterial.Default => MetalWallStressHash, 
			Wall.WallMaterial.Metal => MetalWallStressHash, 
			Wall.WallMaterial.Composite => CompositeWallStressHash, 
			Wall.WallMaterial.Glass => GlassWallStressHash, 
			_ => MetalWallStressHash, 
		};
		Singleton<AudioManager>.Instance.PlayAudioClipsData(emittingWall, num, Vector3.zero);
		StressedEmitterSlots[stressedEmittersArrayIndex].UpdateEmitterData(emittingWall, SoundEmitterSlot.EmitterType.Wall);
	}

	public void AddStressedPipe(INetworkedAtmospherics stressedPipe)
	{
		lock (StressedPipes)
		{
			if (!StressedPipes.Contains(stressedPipe))
			{
				StressedPipes.Add(stressedPipe);
			}
		}
	}

	public void RemoveStressedPipe(INetworkedAtmospherics stressedPipe)
	{
		lock (StressedPipes)
		{
			StressedPipes.Remove(stressedPipe);
		}
	}

	public void AddStressedWall(Wall stressedWall)
	{
		lock (StressedWalls)
		{
			if (!StressedWalls.Contains(stressedWall))
			{
				StressedWalls.Add(stressedWall);
			}
		}
	}

	public void RemoveStressedWall(Wall stressedWall)
	{
		lock (StressedWalls)
		{
			StressedWalls.Remove(stressedWall);
		}
	}

	public bool IsAlreadyEmmiting(Thing thing)
	{
		SoundEmitterSlot[] stressedEmitterSlots = StressedEmitterSlots;
		for (int i = 0; i < stressedEmitterSlots.Length; i++)
		{
			if (stressedEmitterSlots[i].Thing == thing)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsWithinAudibleDistance(Thing thing)
	{
		if (!CameraController.Instance || !CameraController.Instance.MainCameraTransform)
		{
			return false;
		}
		return Vector3.SqrMagnitude(CameraController.Instance.MainCameraTransform.position - thing.Position) < _audibleDistance;
	}
}
