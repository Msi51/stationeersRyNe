using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using Objects.Rockets.UI;
using Sound;
using UnityEngine;
using UnityEngine.Audio;

namespace Assets.Scripts.Sound;

public class AudioManager : Singleton<AudioManager>
{
	public PooledAudioSource AudioSourcePrefab;

	public EnvironmentalAudioHandler environmentalAudioHandler;

	public AtmosphericAudioHandler atmosphericAudioHandler;

	private static readonly HashSet<GameAudioSource> _playingAudioSources = new HashSet<GameAudioSource>();

	private static readonly float AudioCullTickTime = 0.2f;

	private static readonly int OcclusionTickTime = 150;

	public AudioMixer masterMixer;

	public AudioData[] pooledAudioData = Array.Empty<AudioData>();

	private Dictionary<int, GameAudioClipsData> _clipsDataHashLookup = new Dictionary<int, GameAudioClipsData>();

	private Dictionary<int, GameAudioClipsData> _clipsDataSoundAlertLookup = new Dictionary<int, GameAudioClipsData>();

	public Dictionary<int, ChannelData> LoadedChannelData = new Dictionary<int, ChannelData>();

	public Dictionary<int, AudioMixerGroup> MixerGroups = new Dictionary<int, AudioMixerGroup>();

	private static readonly string[] PooledAudioXmlPaths = new string[6] { "Assets/Sounds/PooledAudioData.xml", "Assets/Sounds/CollisionAudioData.xml", "Assets/Sounds/ShuttleAudioData.xml", "Assets/Sounds/UIAudioData.xml", "Assets/Sounds/EquipAudioData.xml", "Assets/Sounds/MusicMachineAudioData.xml" };

	private static readonly int SpaceMapMusic = Animator.StringToHash("SpaceMapMusic");

	public StaticAudioSource mapMusic;

	private const float FadeInTime = 2f;

	private const float FadeOutTime = 3f;

	private float _mapMusicSuppressedUntil;

	private readonly HashSet<GameAudioSource> _workingSet = new HashSet<GameAudioSource>();

	private void LateUpdate()
	{
		if (GameManager.GameState == GameState.Running)
		{
			Interactable.PlayScheduledSounds();
		}
		if (!GameManager.IsBatchMode)
		{
			UpdateMapMusic();
		}
	}

	public void SuppressMapMusic(float duration)
	{
		float num = Time.unscaledTime + duration;
		if (num > _mapMusicSuppressedUntil)
		{
			_mapMusicSuppressedUntil = num;
		}
	}

	public void UpdateMapMusic()
	{
		bool flag = (bool)InventoryManager.Parent && !WorldManager.HasGravityAtHeight(InventoryManager.Parent.Position.y) && InventoryManager.Parent.Room == null;
		bool flag2 = false;
		if (InventoryManager.Parent?.ParentSlot?.Parent is IRocketInternals rocketInternals)
		{
			Rocket rocket = rocketInternals.RocketNetwork?.Rocket;
			if (rocket != null)
			{
				flag2 = rocket.RocketState == RocketState.InSpace || rocket.GetAltitude() > 2500f;
			}
		}
		flag = flag || flag2;
		if (((RocketCanvas.Instance.IsVisible && RocketCanvas.Instance._mapPanel.IsVisible) || flag) && Time.unscaledTime >= _mapMusicSuppressedUntil)
		{
			if (!mapMusic.GameAudioSource.isPlaying)
			{
				if (mapMusic.GameAudioSource.CurrentClips == null)
				{
					mapMusic.Play(SpaceMapMusic);
				}
				else
				{
					mapMusic.GameAudioSource.AudioSource.Play();
				}
			}
			if (mapMusic.GameAudioSource.FadeCurveTracker < 1f)
			{
				mapMusic.GameAudioSource.FadeCurveTracker += Time.unscaledDeltaTime / 2f;
			}
		}
		else if (mapMusic.GameAudioSource.FadeCurveTracker > 0f)
		{
			mapMusic.GameAudioSource.FadeCurveTracker -= Time.unscaledDeltaTime / 3f;
			if (mapMusic.GameAudioSource.FadeCurveTracker == 0f)
			{
				mapMusic.GameAudioSource.AudioSource.Pause();
			}
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		GameAudioSource.OcclusionLayerMask = LayerMask.GetMask("BlockSound", "Terrain");
		GameAudioSource.TerrainLayer = LayerMask.NameToLayer("Terrain");
		LoadAudioData(pooledAudioData);
		LoadMixerGroups();
		AudioTick().Forget();
		OcclusionTick(OcclusionTickTime).Forget();
		environmentalAudioHandler.Initialize();
		atmosphericAudioHandler.Initialize();
		AudioPool.InitializeAllPools(AudioSourcePrefab);
		DynamicThing.InitCollisionSoundMap();
	}

	public static void UpdateVolume(SettingType volumeSettingType)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		Settings.ApplyVolumeSetting(volumeSettingType);
		if (volumeSettingType == SettingType.MusicVolume || volumeSettingType == SettingType.MasterVolume)
		{
			if (GameManager.GameState == GameState.None && Settings.CurrentData.MusicVolume != 0 && Settings.CurrentData.MasterVolume != 0)
			{
				UIAudioManager.PlayMainMenuMusic(2f);
			}
			else if (Settings.CurrentData.MusicVolume <= 0)
			{
				Singleton<GameManager>.Instance.MenuMusic.Stop();
			}
		}
	}

	public void LoadMixerGroups()
	{
		AudioMixerGroup[] array = masterMixer.FindMatchingGroups("");
		foreach (AudioMixerGroup audioMixerGroup in array)
		{
			MixerGroups[Animator.StringToHash(audioMixerGroup.name)] = audioMixerGroup;
		}
	}

	public AudioMixerGroup GetMixerGroup(int nameHash)
	{
		MixerGroups.ContainsKey(nameHash);
		return MixerGroups[nameHash];
	}

	public void LoadAudioData(AudioData[] audioData)
	{
		foreach (AudioData audioData2 in audioData)
		{
			foreach (ChannelData channelDatum in audioData2.ChannelData)
			{
				LoadedChannelData.Add(Animator.StringToHash(channelDatum.Name), channelDatum);
			}
			foreach (GameAudioClipsData audioClipsDatum in audioData2.AudioClipsData)
			{
				_clipsDataHashLookup.Add(audioClipsDatum.NameHash, audioClipsDatum);
				if (audioClipsDatum.SoundAlert != SoundAlert.None)
				{
					_clipsDataSoundAlertLookup.Add((int)audioClipsDatum.SoundAlert, audioClipsDatum);
				}
			}
		}
	}

	public static GameAudioClipsData Find(SoundAlert soundAlert)
	{
		Singleton<AudioManager>.Instance._clipsDataSoundAlertLookup.TryGetValue((int)soundAlert, out var value);
		return value;
	}

	public GameAudioClipsData GetClipData(int nameHash)
	{
		if (!_clipsDataHashLookup.ContainsKey(nameHash))
		{
			return null;
		}
		return _clipsDataHashLookup[nameHash];
	}

	public ChannelData GetChannelData(int nameHash)
	{
		if (!LoadedChannelData.ContainsKey(nameHash))
		{
			return null;
		}
		return LoadedChannelData[nameHash];
	}

	private async UniTask AudioTick()
	{
		while (true)
		{
			await UniTask.Delay(200, DelayType.UnscaledDeltaTime);
			UpdateAudio();
			AudioClipsConcurrency[] concurrencySettings = AudioClipsConcurrency.ConcurrencySettings;
			for (int i = 0; i < concurrencySettings.Length; i++)
			{
				concurrencySettings[i].RemoveNullSubscribers();
			}
			SortConcurrencyLists();
			concurrencySettings = AudioClipsConcurrency.ConcurrencySettings;
			foreach (AudioClipsConcurrency audioClipsConcurrency in concurrencySettings)
			{
				if (audioClipsConcurrency.CanPause)
				{
					audioClipsConcurrency.ManageConcurrency(fadeVolume: true);
				}
			}
		}
	}

	private async UniTaskVoid OcclusionTick(int waitTime)
	{
		while (true)
		{
			await UniTask.Delay(waitTime);
			foreach (GameAudioSource playingAudioSource in _playingAudioSources)
			{
				playingAudioSource?.ManageOcclusion(onPlayOrResume: false);
			}
		}
	}

	public void UpdateAudio()
	{
		_playingAudioSources.RemoveWhere((GameAudioSource x) => x == null || x.AudioSource == null);
		_workingSet.Clear();
		foreach (GameAudioSource playingAudioSource in _playingAudioSources)
		{
			if (playingAudioSource.isPlaying)
			{
				playingAudioSource.CalculateAndSetAtmosphericVolume();
				continue;
			}
			if (playingAudioSource.ParentPooledAudioSource != null)
			{
				playingAudioSource.ResetValues();
				playingAudioSource.ParentPooledAudioSource.ReturnToPool();
			}
			else
			{
				playingAudioSource.ResetValues();
				playingAudioSource.SetEnabled(enable: false);
			}
			RemoveConcurrencySubscriptions(playingAudioSource, manageConcurrency: false);
			_workingSet.Add(playingAudioSource);
		}
		_playingAudioSources.ExceptWith(_workingSet);
	}

	public void AddPlayingAudioSource(GameAudioSource gameAudioSource)
	{
		if (gameAudioSource.Parent != null || !(gameAudioSource.ParentPooledAudioSource == null))
		{
			_playingAudioSources.Add(gameAudioSource);
		}
	}

	public static void RemoveFromPlayingAudioSources(GameAudioSource gameAudioSource)
	{
		if (gameAudioSource.ParentPooledAudioSource != null)
		{
			gameAudioSource.ParentPooledAudioSource.ReturnToPool();
			gameAudioSource.Parent = null;
		}
		_playingAudioSources.Remove(gameAudioSource);
	}

	public void AddConcurrencySubscriptions(GameAudioSource gameAudioSource)
	{
		if (gameAudioSource?.CurrentClips == null || gameAudioSource.CurrentClips.ConcurrencyIds.Count == 0)
		{
			return;
		}
		foreach (int concurrencyId in gameAudioSource.CurrentClips.ConcurrencyIds)
		{
			AudioClipsConcurrency.RemoveNullSubscribers(concurrencyId);
		}
		if (gameAudioSource?.CurrentClips == null)
		{
			return;
		}
		foreach (int concurrencyId2 in gameAudioSource.CurrentClips.ConcurrencyIds)
		{
			AudioClipsConcurrency.Register(concurrencyId2, gameAudioSource);
		}
		if (gameAudioSource?.CurrentClips == null)
		{
			return;
		}
		foreach (int concurrencyId3 in gameAudioSource.CurrentClips.ConcurrencyIds)
		{
			AudioClipsConcurrency.ManageConcurrency(concurrencyId3, fadeVolume: false);
		}
	}

	public static void RemoveConcurrencySubscriptions(GameAudioSource gameAudioSource, bool manageConcurrency)
	{
		if (gameAudioSource.CurrentClips == null)
		{
			return;
		}
		foreach (int concurrencyId in gameAudioSource.CurrentClips.ConcurrencyIds)
		{
			AudioClipsConcurrency audioClipsConcurrency = AudioClipsConcurrency.Get(concurrencyId);
			audioClipsConcurrency.Remove(gameAudioSource);
			if (manageConcurrency && audioClipsConcurrency.CanPause)
			{
				AudioClipsConcurrency.ManageConcurrency(concurrencyId);
			}
		}
	}

	private void SortConcurrencyLists()
	{
		AudioClipsConcurrency[] concurrencySettings = AudioClipsConcurrency.ConcurrencySettings;
		for (int i = 0; i < concurrencySettings.Length; i++)
		{
			concurrencySettings[i].Sort();
		}
	}

	private bool IsValidSoundClips(int clipsDataHash, out GameAudioClipsData clipsData, ref ChannelData channelData)
	{
		clipsData = null;
		clipsData = GetClipData(clipsDataHash);
		if (clipsData == null)
		{
			return false;
		}
		if (channelData != null)
		{
			return true;
		}
		channelData = GetChannelData(Animator.StringToHash(clipsData.ChannelName));
		if (channelData == null)
		{
			return false;
		}
		return true;
	}

	private bool GetAudioAndAssign(int clipsDataHash, IAudioParent parent, ChannelData channelData, AudioPool componentPool, out PooledAudioSource pooledAudio, out GameAudioClipsData clipsData)
	{
		pooledAudio = componentPool.Get();
		clipsData = null;
		if (pooledAudio == null)
		{
			return false;
		}
		if (!IsValidSoundClips(clipsDataHash, out clipsData, ref channelData))
		{
			return false;
		}
		if (!pooledAudio.GameAudioSource.DeserializeRuntime(channelData, parent, pooledAudio))
		{
			pooledAudio.ReturnToPool();
			pooledAudio = null;
			return false;
		}
		parent?.Add(pooledAudio);
		return true;
	}

	private bool GetParent(long id, out Thing parent)
	{
		parent = null;
		if (id <= 0)
		{
			return false;
		}
		parent = Thing.Find(id);
		if (parent == null)
		{
			return false;
		}
		return true;
	}

	public PooledAudioSource PlayAudioClipsData(long parentId, int clipsDataHash, Vector3 localPosition, ChannelData channelData = null, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (!GetParent(parentId, out var parent))
		{
			return null;
		}
		return PlayAudioClipsData(parent, clipsDataHash, localPosition, channelData, volumeMultiplier, pitchMultiplier);
	}

	private async UniTaskVoid WaitPlayAudioClipsData(IAudioParent parent, int clipsDataHash, Vector3 position, ChannelData channelData, float volumeMultiplier, float pitchMultiplier)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		PlayAudioClipsData(parent, clipsDataHash, position, channelData, volumeMultiplier, pitchMultiplier);
	}

	public PooledAudioSource PlayAudioClipsData(IAudioParent parent, int clipsDataHash, Vector3 localPosition, ChannelData channelData = null, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (ThreadedManager.IsThread)
		{
			WaitPlayAudioClipsData(parent, clipsDataHash, localPosition, channelData, volumeMultiplier, pitchMultiplier).Forget();
			return null;
		}
		if (!GetAudioAndAssign(clipsDataHash, parent, channelData, AudioPool.SourcePool, out var pooledAudio, out var clipsData))
		{
			return null;
		}
		pooledAudio.Transform.SetParent(parent.Transform);
		pooledAudio.Transform.localPosition = localPosition;
		pooledAudio.GameAudioSource.SetMixerGroup(parent);
		pooledAudio.GameAudioSource.Play(clipsData, volumeMultiplier, pitchMultiplier);
		return pooledAudio;
	}

	private async UniTaskVoid WaitPlayAudioClipsData(IAudioParent parent, int clipsDataHash, Transform localParent, ChannelData channelData, float volumeMultiplier, float pitchMultiplier)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		PlayAudioClipsData(parent, clipsDataHash, localParent, channelData, volumeMultiplier, pitchMultiplier);
	}

	public PooledAudioSource PlayAudioClipsData(IAudioParent parent, int clipsDataHash, Transform localParent, ChannelData channelData = null, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (ThreadedManager.IsThread)
		{
			WaitPlayAudioClipsData(parent, clipsDataHash, localParent, channelData, volumeMultiplier, pitchMultiplier).Forget();
			return null;
		}
		if (!GetAudioAndAssign(clipsDataHash, parent, channelData, AudioPool.SourcePool, out var pooledAudio, out var clipsData))
		{
			return null;
		}
		pooledAudio.Transform.SetParent(localParent);
		pooledAudio.Transform.localPosition = Vector3.zero;
		pooledAudio.GameAudioSource.SetMixerGroup(parent);
		pooledAudio.GameAudioSource.Play(clipsData, volumeMultiplier, pitchMultiplier);
		return pooledAudio;
	}

	private async UniTaskVoid WaitPlayScheduledAudioClipsData(IAudioParent parent, int clipsDataHash, double startTime, double endTime, Vector3 position, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		PlayScheduledAudioClipsData(parent, clipsDataHash, startTime, endTime, position, volumeMultiplier, pitchMultiplier, attack, release);
	}

	public PooledAudioSource PlayScheduledAudioClipsData(IAudioParent parent, int clipsDataHash, double startTime, double endTime, Vector3 position, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (ThreadedManager.IsThread)
		{
			WaitPlayScheduledAudioClipsData(parent, clipsDataHash, startTime, endTime, position, volumeMultiplier, pitchMultiplier, attack, release).Forget();
			return null;
		}
		if (!GetAudioAndAssign(clipsDataHash, parent, null, AudioPool.SourcePool, out var pooledAudio, out var clipsData))
		{
			return null;
		}
		pooledAudio.Transform.SetParent(parent.Transform);
		pooledAudio.Transform.localPosition = position;
		pooledAudio.GameAudioSource.SetMixerGroup(parent);
		pooledAudio.GameAudioSource.PlayScheduled(clipsData, startTime, endTime, volumeMultiplier, pitchMultiplier, attack, release);
		return pooledAudio;
	}

	private async UniTaskVoid WaitPlayAudioClipsData(int clipsDataHash, Vector3 worldPosition, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		PlayAudioClipsData(clipsDataHash, worldPosition, volumeMultiplier, pitchMultiplier);
	}

	public PooledAudioSource PlayAudioClipsData(int clipsDataHash, Vector3 worldPosition, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (GameManager.IsBatchMode)
		{
			return null;
		}
		if (ThreadedManager.IsThread)
		{
			WaitPlayAudioClipsData(clipsDataHash, worldPosition, volumeMultiplier, pitchMultiplier).Forget();
			return null;
		}
		if (!GetAudioAndAssign(clipsDataHash, null, null, AudioPool.SourcePool, out var pooledAudio, out var clipsData))
		{
			return null;
		}
		pooledAudio.Position = worldPosition;
		pooledAudio.GameAudioSource.SetMixerGroupToDefault();
		pooledAudio.GameAudioSource.Play(clipsData, volumeMultiplier, pitchMultiplier);
		return pooledAudio;
	}

	public void PlayCollisionSound(IAudioParent parent, int clipsDataHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (!GameManager.IsBatchMode && !ThreadedManager.IsThread && GetAudioAndAssign(clipsDataHash, parent, null, AudioPool.CollisionPool, out var pooledAudio, out var clipsData))
		{
			pooledAudio.Transform.SetParent(parent.Transform);
			pooledAudio.Transform.localPosition = Vector3.zero;
			pooledAudio.GameAudioSource.SetMixerGroup(parent);
			pooledAudio.GameAudioSource.Play(clipsData, volumeMultiplier, pitchMultiplier);
		}
	}
}
