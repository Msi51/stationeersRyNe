using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using Sound;
using UnityEngine;
using Util;

namespace Assets.Scripts.Sound;

[Serializable]
public class GameAudioSource
{
	public string Name;

	public IAudioParent Parent;

	public PooledAudioSource ParentPooledAudioSource;

	public StaticAudioSource ParentStaticAudioSource;

	public AudioSource AudioSource;

	public AudioLowPassFilter LowPassFilter;

	public AudioHighPassFilter HighPassFilter;

	public float SourceVolume;

	public float SourcePitch;

	public AnimationCurve SpatialCurve;

	public AnimationCurve ReverbCurve;

	public OcclusionType OcclusionType;

	[NonSerialized]
	[XmlIgnore]
	public GameAudioClipsData CurrentClips;

	public static readonly AnimationCurve FadeCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0.5f), new Keyframe(1f, 1f, 0f, 0f, 0.5f, 0f));

	public static int TerrainLayer;

	public static LayerMask OcclusionLayerMask;

	public int LocalMixerGroupHash;

	public int ExternalMixerGroupHash;

	public int VacuumMixerGroupHash;

	public int OccludedMixerGroupHash;

	[NonSerialized]
	[XmlIgnore]
	public int CurrentMixerGroupNameHash;

	private static readonly int OccludedHash = Animator.StringToHash("Occluded");

	private static readonly int ExternalHash = Animator.StringToHash("External");

	private static readonly float OccludedVolume = 0.5f;

	private static readonly float OcclusionTime = 0.35f;

	private static readonly PressurekPa AtmosAudioVolumeThreshold = new PressurekPa(3.0);

	private static readonly float RayBuffer = 0.01f;

	private static readonly float AudibleDistanceBuffer = 4f;

	private bool _isOccluded;

	private Task _delayTask;

	private CancellationTokenSource _delayCancel;

	private Task _oneShotTask;

	private CancellationTokenSource _oneShotCancel;

	private bool _waitingForPlay;

	private bool _waitingForStop;

	private bool _waitingForAudioUpdate;

	private bool _stopFadeIn;

	private bool _stopFadeOut;

	private bool _isFadingIn;

	private bool _isFadingOut;

	private readonly List<int> _pauseForConcurrencySettings = new List<int>();

	private float _volumeMultiplier;

	private float _fadeCurveTracker;

	private float _occlusionVolumeMultiplier;

	private float _atmosphericVolumeMultiplier;

	private float _pitchMultiplier = 1f;

	private float _effectVolumeMultiplier;

	private float _concurrencyVolumeMultiplier;

	private int _priority;

	private float _maxDistance;

	private float _minDistance;

	private float _pitch;

	private float _volume;

	private bool _loop;

	private float _time;

	private AudioClip _clip;

	private bool _enabled;

	private float _spatialBlend;

	private CancellationTokenWrapper ConcurrencyFadeToken = new CancellationTokenWrapper();

	private const float ConcurrencyFadeSpeed = 5f;

	public bool PausedForConcurrency => _pauseForConcurrencySettings.Count > 0;

	public bool BypassOcclusion
	{
		get
		{
			if (CurrentMixerGroupNameHash != ExternalHash && CurrentMixerGroupNameHash != OccludedHash)
			{
				return CurrentMixerGroupNameHash != OccludedMixerGroupHash;
			}
			return false;
		}
	}

	public float SqDistanceFromListener
	{
		get
		{
			Vector3 vector = ((Parent != null) ? Parent.Position : ParentPooledAudioSource.Position);
			return Vector3.SqrMagnitude(CameraController.Instance.MainCameraPosition - vector);
		}
	}

	public bool InAudibleRange
	{
		get
		{
			if (Parent == null && ParentPooledAudioSource == null)
			{
				return (object)ParentStaticAudioSource != null;
			}
			Vector3 vector = ((Parent != null) ? Parent.Position : ParentPooledAudioSource.Position);
			return Vector3.SqrMagnitude(CameraController.Instance.MainCameraPosition - vector) < Mathf.Pow(maxDistance + AudibleDistanceBuffer, 2f);
		}
	}

	public bool isPlaying
	{
		get
		{
			if (!AudioSource || GameManager.IsThread)
			{
				return false;
			}
			if (AudioSource.isPlaying)
			{
				return true;
			}
			if (_waitingForPlay)
			{
				return true;
			}
			Task delayTask = _delayTask;
			if (delayTask != null && !delayTask.IsCompleted)
			{
				return true;
			}
			if (PausedForConcurrency)
			{
				return true;
			}
			delayTask = _oneShotTask;
			if (delayTask != null && !delayTask.IsCompleted)
			{
				return true;
			}
			return false;
		}
	}

	public float VolumeMultiplier
	{
		get
		{
			return _volumeMultiplier;
		}
		private set
		{
			_volumeMultiplier = value;
			UpdateVolume();
		}
	}

	public float FadeCurveTracker
	{
		get
		{
			return _fadeCurveTracker;
		}
		set
		{
			_fadeCurveTracker = Mathf.Clamp01(value);
			UpdateVolume();
			UpdatePitch();
		}
	}

	public float OcclusionVolumeMultiplier
	{
		get
		{
			return _occlusionVolumeMultiplier;
		}
		private set
		{
			_occlusionVolumeMultiplier = value;
			UpdateVolume();
		}
	}

	public float AtmosphericVolumeMultiplier
	{
		get
		{
			return _atmosphericVolumeMultiplier;
		}
		private set
		{
			_atmosphericVolumeMultiplier = value;
			UpdateVolume();
		}
	}

	public float PitchMultiplier
	{
		get
		{
			return _pitchMultiplier;
		}
		private set
		{
			_pitchMultiplier = value;
			UpdatePitch();
		}
	}

	public float EffectVolumeMultiplier
	{
		get
		{
			return _effectVolumeMultiplier;
		}
		private set
		{
			_effectVolumeMultiplier = value;
			UpdateVolume();
		}
	}

	public float ConcurrencyVolumeMultiplier
	{
		get
		{
			return _concurrencyVolumeMultiplier;
		}
		private set
		{
			_concurrencyVolumeMultiplier = value;
			UpdateVolume();
		}
	}

	public int priority
	{
		get
		{
			if (!GameManager.IsThread)
			{
				return AudioSource.priority;
			}
			return _priority;
		}
		private set
		{
			_priority = value;
			AudioSource.priority = value;
		}
	}

	public float maxDistance
	{
		get
		{
			return _maxDistance;
		}
		set
		{
			_maxDistance = value;
			AudioSource.maxDistance = value;
		}
	}

	public float minDistance
	{
		get
		{
			return _minDistance;
		}
		set
		{
			_minDistance = value;
			AudioSource.minDistance = value;
		}
	}

	public float pitch
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _pitch;
			}
			_pitch = AudioSource.pitch;
			return _pitch;
		}
		private set
		{
			_pitch = value;
			AudioSource.pitch = _pitch;
		}
	}

	public float volume
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _volume;
			}
			_volume = AudioSource.volume;
			return _volume;
		}
		private set
		{
			_volume = value;
			AudioSource.volume = _volume;
		}
	}

	public bool loop
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _loop;
			}
			_loop = AudioSource.loop;
			return _loop;
		}
		private set
		{
			_loop = value;
			AudioSource.loop = _loop;
		}
	}

	public float time
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _time;
			}
			_time = AudioSource.time;
			return _time;
		}
		private set
		{
			_time = value;
			AudioSource.time = _time;
		}
	}

	public AudioClip clip
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _clip;
			}
			_clip = AudioSource.clip;
			return _clip;
		}
		private set
		{
			_clip = value;
			AudioSource.clip = _clip;
		}
	}

	public bool enabled
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _enabled;
			}
			if (AudioSource == null)
			{
				return false;
			}
			_enabled = AudioSource.enabled;
			return _enabled;
		}
		private set
		{
			if (!(AudioSource == null))
			{
				_enabled = value;
				AudioSource.enabled = _enabled;
				if (HighPassFilter != null)
				{
					HighPassFilter.enabled = _enabled;
				}
				if (LowPassFilter != null)
				{
					LowPassFilter.enabled = _enabled;
				}
			}
		}
	}

	public float spatialBlend
	{
		get
		{
			if (GameManager.IsThread)
			{
				return _spatialBlend;
			}
			_spatialBlend = AudioSource.spatialBlend;
			return _spatialBlend;
		}
		private set
		{
			_spatialBlend = value;
			AudioSource.spatialBlend = _spatialBlend;
		}
	}

	public double ScheduledEndTime { get; private set; }

	public void Init(IAudioParent parent)
	{
		Parent = parent;
		_priority = AudioSource.priority;
		_minDistance = AudioSource.minDistance;
		_maxDistance = AudioSource.maxDistance;
	}

	public GameAudioSource()
	{
		_occlusionVolumeMultiplier = 1f;
		_volumeMultiplier = 1f;
		_fadeCurveTracker = 1f;
		_pitchMultiplier = 1f;
		_atmosphericVolumeMultiplier = 1f;
		_effectVolumeMultiplier = 1f;
		_concurrencyVolumeMultiplier = 1f;
		SourcePitch = 1f;
	}

	public GameAudioSource(AudioSource audioSource)
	{
		AudioSource = audioSource;
		SourceVolume = audioSource.volume;
		SourcePitch = audioSource.pitch;
		LowPassFilter = audioSource.GetComponent<AudioLowPassFilter>();
		HighPassFilter = audioSource.GetComponent<AudioHighPassFilter>();
		AudioSource.playOnAwake = false;
	}

	private float FadeVolumeMultiplier()
	{
		return FadeCurve.Evaluate(FadeCurveTracker);
	}

	private float FadePitchMultiplier()
	{
		if (CurrentClips == null)
		{
			return 1f;
		}
		if (!CurrentClips.FadePitch)
		{
			return 1f;
		}
		return FadeVolumeMultiplier() * 0.5f + 0.5f;
	}

	private void UpdateVolume()
	{
		if (AudioSource != null)
		{
			volume = SourceVolume * VolumeMultiplier * FadeVolumeMultiplier() * OcclusionVolumeMultiplier * AtmosphericVolumeMultiplier * ConcurrencyVolumeMultiplier * EffectVolumeMultiplier;
		}
	}

	private void UpdatePitch()
	{
		if (AudioSource != null && CurrentClips != null)
		{
			pitch = _pitchMultiplier * SourcePitch * FadePitchMultiplier();
		}
	}

	public void SetVolumeMultiplier(int clipDataNameHash, float volumeMultiplier)
	{
		if (SameNameHash(clipDataNameHash))
		{
			if (GameManager.IsThread)
			{
				SetVolumeMultiplierFromThread(clipDataNameHash, volumeMultiplier).Forget();
			}
			else
			{
				VolumeMultiplier = volumeMultiplier;
			}
		}
	}

	public void LerpVolumeMultiplier(float volumeMultiplier, float t)
	{
		if (CurrentClips != null)
		{
			SetVolumeMultiplier(CurrentClips.NameHash, Mathf.Lerp(VolumeMultiplier, volumeMultiplier, t));
		}
	}

	public void SetEffectVolumeMultiplier(float effectVolumeMultiplier)
	{
		if (!GameManager.IsThread)
		{
			EffectVolumeMultiplier = effectVolumeMultiplier;
		}
	}

	public void LerpPitchMultiplier(int clipDataNameHash, float pitchMultiplier, float t)
	{
		SetPitchMultiplier(clipDataNameHash, Mathf.Lerp(PitchMultiplier, pitchMultiplier, t));
	}

	public void SetPitchMultiplier(int clipDataNameHash, float pitchMultiplier)
	{
		if (SameNameHash(clipDataNameHash))
		{
			if (GameManager.IsThread)
			{
				SetPitchMultiplierFromThread(clipDataNameHash, pitchMultiplier).Forget();
			}
			else
			{
				PitchMultiplier = pitchMultiplier;
			}
		}
	}

	public void SetSourceVolume(int clipDataNameHash, float sourceVolume)
	{
		if (SameNameHash(clipDataNameHash))
		{
			if (GameManager.IsThread)
			{
				SetSourceVolumeFromThread(clipDataNameHash, sourceVolume).Forget();
				return;
			}
			SourceVolume = sourceVolume;
			UpdateVolume();
		}
	}

	public void SetEnabled(bool enable)
	{
		if (GameManager.IsThread)
		{
			SetEnabledFromThread(enable).Forget();
		}
		else
		{
			enabled = enable;
		}
	}

	public void SetSpatialBlend(float blend)
	{
		if (GameManager.IsThread)
		{
			SetSpatialBlendFromThread(blend).Forget();
		}
		else
		{
			spatialBlend = blend;
		}
	}

	public void Play(GameAudioClipsData clipData, float volumeMultiplier = 1f, float pitchMultiplier = 1f, float fadeCurveTracker = 1f, bool pregame = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (GameManager.IsThread)
		{
			if (!_waitingForPlay)
			{
				_waitingForPlay = true;
				PlayFromThread(clipData, volumeMultiplier, pitchMultiplier).Forget();
			}
			return;
		}
		_waitingForPlay = false;
		if (!AudioSource)
		{
			return;
		}
		if (CurrentClips != null && CurrentClips.NameHash != clipData.NameHash && isPlaying)
		{
			StopInternal();
		}
		SetOccludedState(occluded: false, fadeVolume: false);
		CurrentClips = clipData;
		if (!InitAudioSource())
		{
			StopInternal();
			return;
		}
		if (CurrentClips.RandomPitch)
		{
			pitchMultiplier = UnityEngine.Random.Range(CurrentClips.RandomPitchRangeLow, CurrentClips.RandomPitchRangeHigh) * pitchMultiplier;
		}
		CancelDelay();
		if (CurrentClips.FadeDown)
		{
			FadeDownVolumeMultiplier(CurrentClips.FadeDownTarget, CurrentClips.FadeDownTime).Forget();
		}
		if (clipData.Delay > 0f && !pregame)
		{
			if (isPlaying)
			{
				StopInternal();
			}
			_delayCancel = new CancellationTokenSource();
			_delayTask = SchedulePlay(clipData.Delay, volumeMultiplier, pitchMultiplier, fadeCurveTracker, _delayCancel.Token).AsTask();
		}
		else if (CurrentClips.LoopingOneShots)
		{
			PrepareNextOneShot(CurrentClips, volumeMultiplier, pitchMultiplier);
			if (_isFadingIn || _isFadingOut)
			{
				fadeCurveTracker = FadeCurveTracker;
			}
			Play(fadeCurveTracker, volumeMultiplier, pitchMultiplier);
		}
		else if (CurrentClips.FadeIn && !pregame)
		{
			FadeIn(CurrentClips, CurrentClips.FadeInTime, CurrentClips.RandomStartTime, volumeMultiplier, pitchMultiplier).Forget();
		}
		else
		{
			Play(fadeCurveTracker, volumeMultiplier, pitchMultiplier);
		}
	}

	public void PlayScheduled(GameAudioClipsData clipData, double startTime, double endTime, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		if (CurrentClips != null && isPlaying)
		{
			StopInternal();
		}
		SetOccludedState(occluded: false, fadeVolume: false);
		CurrentClips = clipData;
		if (!InitAudioSource())
		{
			StopInternal();
			return;
		}
		CancelDelay();
		if (GameManager.IsBatchMode)
		{
			return;
		}
		FadeCurveTracker = 1f;
		EffectVolumeMultiplier = 1f;
		ConcurrencyVolumeMultiplier = 1f;
		PitchMultiplier = pitchMultiplier;
		VolumeMultiplier = volumeMultiplier;
		if (!(AudioSource == null))
		{
			enabled = true;
			AudioSource.PlayScheduled(startTime);
			ScheduledEndTime = endTime + (double)release * 0.0010000000474974513;
			AudioSource.SetScheduledEndTime(ScheduledEndTime);
			_pauseForConcurrencySettings.Clear();
			Singleton<AudioManager>.Instance.AddPlayingAudioSource(this);
			Singleton<AudioManager>.Instance.AddConcurrencySubscriptions(this);
			ManageOcclusion(onPlayOrResume: true);
			CalculateAndSetAtmosphericVolume(onPlay: true);
			if (attack > 30)
			{
				FadeCurveTracker = 0f;
				FadeIn(CurrentClips, (float)attack * 0.001f, randomStartTime: false, volumeMultiplier, pitchMultiplier, Math.Max(0.0, startTime - AudioSettings.dspTime)).Forget();
			}
			if (release > 30)
			{
				DoFadeOutScheduled(clipData, (float)(endTime - AudioSettings.dspTime), release).Forget();
			}
		}
	}

	public void Stop(int clipDataHash, bool immediate = false)
	{
		if (SameNameHash(clipDataHash))
		{
			Stop(immediate);
		}
	}

	public void Stop(bool immediate = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (ThreadedManager.IsThread)
		{
			if (!_waitingForStop)
			{
				_waitingForStop = true;
				WaitForStopFromThread().Forget();
			}
			return;
		}
		_waitingForStop = false;
		if (CurrentClips != null)
		{
			if (CurrentClips.FadeOut && !immediate)
			{
				FadeOut(CurrentClips.FadeOutTime).Forget();
			}
			else
			{
				StopInternal();
			}
		}
		else if (isPlaying)
		{
			StopInternal();
		}
	}

	public void SetMixerGroup(IAudioParent parent)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (ThreadedManager.IsThread)
		{
			if (!_waitingForAudioUpdate)
			{
				_waitingForAudioUpdate = true;
				SetAudioMixerFromThread(parent).Forget();
			}
		}
		else
		{
			if (AudioSource == null)
			{
				return;
			}
			if (parent == null)
			{
				AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(ExternalMixerGroupHash);
				CurrentMixerGroupNameHash = ExternalMixerGroupHash;
				return;
			}
			if (parent.IsSoundLocal())
			{
				AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(LocalMixerGroupHash);
				CurrentMixerGroupNameHash = LocalMixerGroupHash;
			}
			else
			{
				AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(ExternalMixerGroupHash);
				CurrentMixerGroupNameHash = ExternalMixerGroupHash;
			}
			_waitingForAudioUpdate = false;
		}
	}

	public void SetMixerGroupToDefault()
	{
		if (GameManager.IsThread)
		{
			SetMixerGroupToDefaultFromThread().Forget();
			return;
		}
		AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(ExternalMixerGroupHash);
		CurrentMixerGroupNameHash = ExternalMixerGroupHash;
	}

	public async UniTaskVoid SetMixerGroupToDefaultFromThread()
	{
		await UniTask.SwitchToMainThread();
		SetMixerGroupToDefault();
	}

	public bool StopForConcurrency(AudioClipsConcurrency concurrency)
	{
		if (AudioSource == null)
		{
			return false;
		}
		if (concurrency == null)
		{
			return false;
		}
		if (!HasConcurrencySetting(concurrency))
		{
			return false;
		}
		if (!isPlaying)
		{
			return false;
		}
		StopInternal();
		return true;
	}

	public bool PauseForConcurrency(AudioClipsConcurrency concurrency, bool fadeVolume)
	{
		if (AudioSource == null)
		{
			return false;
		}
		if (concurrency == null)
		{
			return false;
		}
		if (!concurrency.CanPause)
		{
			return false;
		}
		if (!HasConcurrencySetting(concurrency))
		{
			return false;
		}
		if (!_pauseForConcurrencySettings.Contains(concurrency.NameHash))
		{
			_pauseForConcurrencySettings.Add(concurrency.NameHash);
			ConcurrencyFadeToken.CancelAndInitialize();
			if (fadeVolume)
			{
				FadeOutForConcurrency(ConcurrencyFadeToken.Token).Forget();
			}
			else
			{
				ConcurrencyVolumeMultiplier = 0f;
				AudioSource.Pause();
			}
		}
		return true;
	}

	public bool ResumeForConcurrency(AudioClipsConcurrency concurrency)
	{
		if (AudioSource == null)
		{
			return false;
		}
		if (concurrency == null)
		{
			return false;
		}
		if (!HasConcurrencySetting(concurrency))
		{
			return false;
		}
		if (!concurrency.CanPause)
		{
			return false;
		}
		if (!_pauseForConcurrencySettings.Contains(concurrency.NameHash))
		{
			return true;
		}
		_pauseForConcurrencySettings.Remove(concurrency.NameHash);
		if (PausedForConcurrency)
		{
			return true;
		}
		ConcurrencyFadeToken.CancelAndInitialize();
		FadeInForConcurrency(ConcurrencyFadeToken.Token).Forget();
		ManageOcclusion(onPlayOrResume: true);
		return true;
	}

	public int SortByDistanceClosestToFarthest(GameAudioSource other)
	{
		if (SqDistanceFromListener < other.SqDistanceFromListener)
		{
			return 1;
		}
		if (SqDistanceFromListener > other.SqDistanceFromListener)
		{
			return -1;
		}
		return 0;
	}

	public int SortByPriorityHighestToLowest(GameAudioSource other)
	{
		if (priority < other.priority)
		{
			return 1;
		}
		if (priority > other.priority)
		{
			return -1;
		}
		return 0;
	}

	public void CalculateAndSetAtmosphericVolume(bool onPlay = false)
	{
		if (CurrentMixerGroupNameHash != ExternalHash && CurrentMixerGroupNameHash != OccludedHash)
		{
			AtmosphericVolumeMultiplier = 1f;
		}
		else if (isPlaying && !PausedForConcurrency && InAudibleRange)
		{
			Atmosphere atmosphere = null;
			if (Parent != null)
			{
				atmosphere = GridController.World?.AtmosphericsController.SampleGlobalAtmosphere(Parent.WorldGrid);
			}
			else if (ParentPooledAudioSource != null)
			{
				atmosphere = GridController.World?.AtmosphericsController?.SampleGlobalAtmosphere(new WorldGrid(ParentPooledAudioSource.Transform.position));
			}
			if (Parent != null || ParentPooledAudioSource != null)
			{
				AtmosphericVolumeMultiplier = ((atmosphere != null) ? Mathf.Clamp01((atmosphere.PressureGassesAndLiquids / AtmosAudioVolumeThreshold).ToFloat()) : 0f);
			}
			else
			{
				AtmosphericVolumeMultiplier = 1f;
			}
		}
	}

	public void ManageOcclusion(bool onPlayOrResume)
	{
		if (BypassOcclusion)
		{
			SetOccludedState(occluded: false, fadeVolume: false);
		}
		else
		{
			if (!isPlaying || PausedForConcurrency || (AudioSource.isVirtual && !onPlayOrResume) || !InAudibleRange)
			{
				return;
			}
			bool occluded = false;
			switch (OcclusionType)
			{
			case OcclusionType.Los:
			{
				Vector3 mainCameraPosition = CameraController.Instance.MainCameraPosition;
				Vector3 position = AudioSource.transform.position;
				if (!Physics.Raycast(mainCameraPosition, (position - mainCameraPosition).normalized, out var hitInfo, Vector3.Distance(position, mainCameraPosition) - RayBuffer, OcclusionLayerMask.value, QueryTriggerInteraction.Ignore))
				{
					break;
				}
				occluded = true;
				if (Parent != null)
				{
					Thing thing = Thing.Find(hitInfo.collider);
					if ((bool)thing && thing == Parent)
					{
						occluded = false;
					}
				}
				break;
			}
			case OcclusionType.Room:
			{
				Room room = null;
				if (Parent != null)
				{
					room = RoomController.World.GetRoom(Parent.WorldGrid);
				}
				if (ParentPooledAudioSource != null)
				{
					room = GridController.World.RoomController.GetRoom(ParentPooledAudioSource.Transform.position);
				}
				if (InventoryManager.ParentHuman != null && room != InventoryManager.ParentHuman.Room)
				{
					occluded = true;
				}
				break;
			}
			case OcclusionType.None:
				occluded = false;
				break;
			}
			SetOccludedState(occluded, !onPlayOrResume);
		}
	}

	private void Play(float fadeCurveTracker = 1f, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (!GameManager.IsBatchMode)
		{
			FadeCurveTracker = fadeCurveTracker;
			PitchMultiplier = pitchMultiplier;
			VolumeMultiplier = volumeMultiplier;
			ConcurrencyVolumeMultiplier = 1f;
			EffectVolumeMultiplier = 1f;
			if (!(AudioSource == null))
			{
				enabled = true;
				AudioSource.Play();
				_pauseForConcurrencySettings.Clear();
				Singleton<AudioManager>.Instance.AddPlayingAudioSource(this);
				Singleton<AudioManager>.Instance.AddConcurrencySubscriptions(this);
				ManageOcclusion(onPlayOrResume: true);
				CalculateAndSetAtmosphericVolume(onPlay: true);
			}
		}
	}

	private async UniTask WaitForStopFromThread()
	{
		await UniTask.SwitchToMainThread();
		await UniTask.NextFrame();
		Stop();
	}

	private void StopInternal()
	{
		if (!GameManager.IsBatchMode)
		{
			ResetValues();
			if (AudioSource != null)
			{
				AudioSource.Stop();
			}
			AudioManager.RemoveFromPlayingAudioSources(this);
			if (AudioSource != null)
			{
				enabled = false;
			}
		}
	}

	public void ResetValues()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			CancelDelay();
			_stopFadeIn = true;
			_isFadingIn = false;
			_stopFadeOut = true;
			_isFadingOut = false;
			FadeCurveTracker = 0f;
			ScheduledEndTime = 0.0;
			_pauseForConcurrencySettings.Clear();
			AudioManager.RemoveConcurrencySubscriptions(this, manageConcurrency: true);
			CurrentClips = null;
		}
	}

	private async UniTask PlayFromThread(GameAudioClipsData audioClips, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		await UniTask.SwitchToMainThread();
		await UniTask.NextFrame();
		Play(audioClips, volumeMultiplier, pitchMultiplier);
	}

	private bool InitAudioSource()
	{
		if (AudioSource == null)
		{
			return false;
		}
		clip = CurrentClips.Clips.Pick();
		if (!clip)
		{
			return false;
		}
		AudioSource.loop = CurrentClips.Looping;
		if (CurrentClips.RandomStartTime)
		{
			float num = UnityEngine.Random.Range(0.001f, clip.length * 0.8f);
			if (num > 0f && num < clip.length)
			{
				time = Mathf.Min(num, clip.length - 0.01f);
			}
		}
		else
		{
			time = 0f;
		}
		return true;
	}

	private void CancelDelay()
	{
		CancellationTokenSource delayCancel = _delayCancel;
		if (delayCancel != null && !delayCancel.IsCancellationRequested)
		{
			_delayCancel.Cancel();
		}
		delayCancel = _oneShotCancel;
		if (delayCancel != null && !delayCancel.IsCancellationRequested)
		{
			_oneShotCancel.Cancel();
		}
	}

	private async UniTask SchedulePlay(float delay, float volumeMultiplier, float pitchMultiplier, float fadeCurveTracker, CancellationToken delayCancelToken)
	{
		await UniTask.Delay((int)(delay * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, delayCancelToken);
		if (!delayCancelToken.IsCancellationRequested)
		{
			if (CurrentClips.FadeIn)
			{
				FadeIn(CurrentClips, CurrentClips.FadeInTime, CurrentClips.RandomStartTime, 1f, 1f, 0.0, force: true).Forget();
			}
			else
			{
				Play(fadeCurveTracker, volumeMultiplier, pitchMultiplier);
			}
		}
	}

	private void PrepareNextOneShot(GameAudioClipsData clipData, float volumeMultiplier, float pitchMultiplier)
	{
		CancelDelay();
		_oneShotCancel = new CancellationTokenSource();
		_oneShotTask = PlayDelayedLoopingOnesShots(clip.length, clipData, volumeMultiplier, pitchMultiplier, _oneShotCancel.Token).AsTask();
	}

	private async UniTask PlayDelayedLoopingOnesShots(float delay, GameAudioClipsData clipData, float volumeMultiplier, float pitchMultiplier, CancellationToken cancelToken)
	{
		await UniTask.Delay((int)(delay * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (!cancelToken.IsCancellationRequested && SameNameHash(clipData) && InitAudioSource())
		{
			float fadeCurveTracker = 1f;
			if (_isFadingIn || _isFadingOut)
			{
				fadeCurveTracker = FadeCurveTracker;
			}
			if (CurrentClips.RandomPitch)
			{
				pitchMultiplier = UnityEngine.Random.Range(CurrentClips.RandomPitchRangeLow, CurrentClips.RandomPitchRangeHigh);
			}
			Play(fadeCurveTracker, volumeMultiplier, pitchMultiplier);
			PrepareNextOneShot(clipData, volumeMultiplier, pitchMultiplier);
		}
	}

	private async UniTask SetVolumeMultiplierFromThread(int clipDataNameHash, float volumeMultiplier)
	{
		await UniTask.SwitchToMainThread();
		SetVolumeMultiplier(clipDataNameHash, volumeMultiplier);
	}

	private async UniTask SetPitchMultiplierFromThread(int clipDataNameHash, float pitchMultiplier)
	{
		await UniTask.SwitchToMainThread();
		SetPitchMultiplier(clipDataNameHash, pitchMultiplier);
	}

	private async UniTask SetSourceVolumeFromThread(int clipDataNameHash, float sourceVolume)
	{
		await UniTask.SwitchToMainThread();
		SetSourceVolume(clipDataNameHash, sourceVolume);
	}

	private async UniTask SetEnabledFromThread(bool enable)
	{
		await UniTask.SwitchToMainThread();
		SetEnabled(enable);
	}

	private async UniTask SetSpatialBlendFromThread(float blend)
	{
		await UniTask.SwitchToMainThread();
		SetSpatialBlend(blend);
	}

	private bool SameNameHash(GameAudioClipsData clipData)
	{
		if (CurrentClips == null)
		{
			return false;
		}
		return clipData.NameHash == CurrentClips.NameHash;
	}

	private bool SameNameHash(int clipDataNameHash)
	{
		if (CurrentClips == null)
		{
			return false;
		}
		return clipDataNameHash == CurrentClips.NameHash;
	}

	private async UniTaskVoid FadeOutForConcurrency(CancellationToken token)
	{
		while (GameManager.GameState != GameState.None && isPlaying && ConcurrencyVolumeMultiplier > 0f && !token.IsCancellationRequested)
		{
			await UniTask.NextFrame(token);
			ConcurrencyVolumeMultiplier -= Time.unscaledDeltaTime * 5f;
		}
		ConcurrencyVolumeMultiplier = 0f;
		AudioSource.Pause();
	}

	private async UniTaskVoid FadeInForConcurrency(CancellationToken token)
	{
		AudioSource.UnPause();
		while (GameManager.GameState != GameState.None && isPlaying && ConcurrencyVolumeMultiplier < 1f && !token.IsCancellationRequested)
		{
			await UniTask.NextFrame(token);
			ConcurrencyVolumeMultiplier += Time.unscaledDeltaTime * 5f;
		}
		ConcurrencyVolumeMultiplier = 1f;
	}

	private async UniTask FadeIn(GameAudioClipsData clipsData = null, float fadeInTime = 1f, bool randomStartTime = true, float volumeMultiplier = 1f, float pitchMultiplier = 1f, double waitTime = 0.0, bool force = false)
	{
		if (GameManager.IsBatchMode || _isFadingIn)
		{
			return;
		}
		_isFadingIn = true;
		_stopFadeOut = true;
		_isFadingOut = false;
		_stopFadeIn = false;
		if (!isPlaying || force)
		{
			int num = 0;
			if (clipsData != null && clipsData.RandomPitch)
			{
				pitchMultiplier = UnityEngine.Random.Range(clipsData.RandomPitchRangeLow, clipsData.RandomPitchRangeHigh) * pitchMultiplier;
			}
			if (fadeInTime <= 0f)
			{
				num = 1;
				_isFadingIn = false;
				Play(num, volumeMultiplier, pitchMultiplier);
				return;
			}
			Play(num, volumeMultiplier, pitchMultiplier);
		}
		double startTime = waitTime + (double)GameManager.GameTime - (double)Time.deltaTime;
		while (FadeCurveTracker < 1f && !_stopFadeIn)
		{
			if (startTime > (double)GameManager.GameTime)
			{
				await UniTask.NextFrame();
			}
			FadeCurveTracker += Time.unscaledDeltaTime / fadeInTime;
			if (FadeCurveTracker >= 1f)
			{
				_isFadingIn = false;
			}
			await UniTask.NextFrame();
		}
	}

	private async UniTask DoFadeOutScheduled(GameAudioClipsData clipsData, float waitTime, float fadeOutTime)
	{
		await UniTask.Delay((int)(waitTime * 1000f));
		if (isPlaying)
		{
			FadeOut(fadeOutTime).Forget();
		}
	}

	private async UniTask FadeOut(float fadeOutTime = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		Task delayTask = _delayTask;
		if (delayTask != null && !delayTask.IsCompleted && AudioSource != null && !AudioSource.isPlaying)
		{
			StopInternal();
		}
		else
		{
			if (!isPlaying || _isFadingOut)
			{
				return;
			}
			_isFadingOut = true;
			_stopFadeOut = false;
			_stopFadeIn = true;
			_isFadingIn = false;
			if (fadeOutTime <= 0f)
			{
				StopInternal();
				return;
			}
			while (FadeCurveTracker > 0f && !_stopFadeOut)
			{
				FadeCurveTracker -= Time.unscaledDeltaTime / fadeOutTime;
				if (FadeCurveTracker <= 0f)
				{
					StopInternal();
				}
				await UniTask.NextFrame();
			}
		}
	}

	public async UniTask FadeDownVolumeMultiplier(float targetVolume, float fadeTime)
	{
		if (GameManager.IsBatchMode || AudioSource == null || fadeTime < float.Epsilon)
		{
			return;
		}
		await UniTask.NextFrame();
		if (CurrentClips == null)
		{
			return;
		}
		int clipsDataHash = CurrentClips.NameHash;
		while (AudioSource != null && CurrentClips != null && clipsDataHash == CurrentClips.NameHash && VolumeMultiplier >= targetVolume && AudioSource.isPlaying && !_isFadingOut)
		{
			VolumeMultiplier -= Time.unscaledDeltaTime / fadeTime;
			if (VolumeMultiplier < targetVolume)
			{
				VolumeMultiplier = targetVolume;
			}
			await UniTask.NextFrame();
		}
	}

	private void SetOccludedState(bool occluded, bool fadeVolume)
	{
		if (occluded == _isOccluded)
		{
			return;
		}
		_isOccluded = occluded;
		if (!GameManager.IsBatchMode)
		{
			if (BypassOcclusion)
			{
				OcclusionVolumeMultiplier = 1f;
			}
			else if (!isPlaying || AudioSource.isVirtual || !fadeVolume)
			{
				OcclusionVolumeMultiplier = (_isOccluded ? OccludedVolume : 1f);
				SetOccludedMixerGroup(_isOccluded);
			}
			else if (_isOccluded)
			{
				DoStartOcclusion().Forget();
			}
			else
			{
				DoStopOcclusion().Forget();
			}
		}
	}

	private void SetOccludedMixerGroup(bool isOccluded)
	{
		if (Parent == null || !Parent.IsBeingDestroyed)
		{
			if (isOccluded)
			{
				int num = ((OccludedMixerGroupHash != 0) ? OccludedMixerGroupHash : OccludedHash);
				AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(num);
				CurrentMixerGroupNameHash = num;
			}
			else
			{
				AudioSource.outputAudioMixerGroup = Singleton<AudioManager>.Instance.GetMixerGroup(ExternalMixerGroupHash);
				CurrentMixerGroupNameHash = ExternalMixerGroupHash;
			}
		}
	}

	private async UniTask DoStartOcclusion()
	{
		while (OcclusionVolumeMultiplier > OccludedVolume && _isOccluded && !(Parent?.IsBeingDestroyed ?? true))
		{
			OcclusionVolumeMultiplier -= Time.unscaledDeltaTime / OcclusionTime;
			if (OcclusionVolumeMultiplier < OccludedVolume)
			{
				OcclusionVolumeMultiplier = OccludedVolume;
				SetOccludedMixerGroup(isOccluded: true);
			}
			await UniTask.NextFrame();
		}
	}

	private async UniTask DoStopOcclusion()
	{
		SetOccludedMixerGroup(isOccluded: false);
		while (OcclusionVolumeMultiplier < 1f && !_isOccluded && !(Parent?.IsBeingDestroyed ?? true))
		{
			OcclusionVolumeMultiplier += Time.unscaledDeltaTime / OcclusionTime;
			if (OcclusionVolumeMultiplier > 1f)
			{
				OcclusionVolumeMultiplier = 1f;
			}
			await UniTask.NextFrame();
		}
	}

	private async UniTask SetAudioMixerFromThread(IAudioParent parent)
	{
		await UniTask.SwitchToMainThread();
		SetMixerGroup(parent);
	}

	private bool HasConcurrencySetting(AudioClipsConcurrency concurrency)
	{
		if (CurrentClips == null)
		{
			return false;
		}
		foreach (int concurrencyId in CurrentClips.ConcurrencyIds)
		{
			if (concurrency.NameHash == concurrencyId)
			{
				return true;
			}
		}
		return false;
	}

	private void Deserialize(ChannelData audioChannelData, IAudioParent parent, PooledAudioSource parentPooledAudioSource)
	{
		Parent = parent;
		ParentPooledAudioSource = parentPooledAudioSource;
		priority = audioChannelData.Priority;
		SourceVolume = audioChannelData.Volume;
		SourcePitch = audioChannelData.Pitch;
		AudioSource.volume = audioChannelData.Volume;
		AudioSource.pitch = audioChannelData.Pitch;
		AudioSource.spatialBlend = (float)audioChannelData.SpatialReference;
		AudioSource.bypassReverbZones = audioChannelData.BypassReverbZones;
		ReverbCurve = null;
		LocalMixerGroupHash = Animator.StringToHash(audioChannelData.LocalMixerGroup);
		ExternalMixerGroupHash = Animator.StringToHash(audioChannelData.ExternalMixerGroup);
		VacuumMixerGroupHash = Animator.StringToHash(audioChannelData.VacuumMixerGroup);
		OccludedMixerGroupHash = Animator.StringToHash(audioChannelData.OccludedMixerGroup);
		OcclusionType = audioChannelData.OcclusionType;
		if (audioChannelData.SpatialSoundData != null)
		{
			audioChannelData.SpatialSoundData.Apply(AudioSource);
			maxDistance = audioChannelData.SpatialSoundData.MaxDistance;
			minDistance = audioChannelData.SpatialSoundData.MinDistance;
			switch (audioChannelData.Reverb)
			{
			case ReverbType.Flat:
				AudioSource.reverbZoneMix = audioChannelData.SpatialSoundData.ReverbZoneMix;
				break;
			case ReverbType.Spatial:
				if (audioChannelData.SpatialSoundData.MaxDistance <= 10f)
				{
					ReverbCurve = ChannelData.SmallReverbCurve;
				}
				else if (audioChannelData.SpatialSoundData.MaxDistance <= 20f)
				{
					ReverbCurve = ChannelData.MediumReverbCurve;
				}
				else
				{
					ReverbCurve = ChannelData.LargeReverbCurve;
				}
				AudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, ReverbCurve);
				break;
			case ReverbType.None:
				AudioSource.reverbZoneMix = 0f;
				break;
			case ReverbType.Quiet:
				AudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, ChannelData.QuietCurve);
				ReverbCurve = ChannelData.QuietCurve;
				break;
			case ReverbType.FlatLoud:
				AudioSource.reverbZoneMix = 1.05f;
				break;
			}
		}
		Name = audioChannelData.Name;
	}

	public bool DeserializeRuntime(ChannelData audioChannelData, IAudioParent parent, PooledAudioSource parentPooledAudioSource)
	{
		if (audioChannelData == null)
		{
			return false;
		}
		if (parentPooledAudioSource == null)
		{
			return false;
		}
		if (parent != null && parent.IsBeingDestroyed)
		{
			return false;
		}
		if (audioChannelData.SpatialCurve != null && audioChannelData.SpatialCurve.length > 0)
		{
			AudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, audioChannelData.SpatialCurve);
		}
		Deserialize(audioChannelData, parent, parentPooledAudioSource);
		return true;
	}

	public bool DeserializeRuntime(ChannelData audioChannelData, StaticAudioSource staticAudioSource)
	{
		if (audioChannelData == null)
		{
			return false;
		}
		if (staticAudioSource == null)
		{
			return false;
		}
		if (audioChannelData.SpatialCurve != null && audioChannelData.SpatialCurve.length > 0)
		{
			AudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, audioChannelData.SpatialCurve);
		}
		ParentStaticAudioSource = staticAudioSource;
		Deserialize(audioChannelData, null, null);
		return true;
	}
}
