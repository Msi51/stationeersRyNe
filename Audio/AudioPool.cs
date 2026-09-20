using System.Collections.Generic;
using Assets.Scripts.Serialization;
using Sound;
using UnityEngine;

namespace Audio;

public class AudioPool
{
	private static List<AudioPool> _allAudioPools = new List<AudioPool>(5);

	public static AudioPool SourcePool = new AudioPool("SourcePool", 128);

	public static AudioPool CollisionPool = new AudioPool("CollisionPool", 32);

	public static List<PooledAudioSource> AllAudioPoolItems;

	private readonly GameObjectPool<PooledAudioSource> _pool;

	private readonly int _audioSourcesPerInstance;

	private int _size;

	public int Available => _pool.Available;

	public int Size => _size;

	public PooledAudioSource Get()
	{
		return _pool.Get();
	}

	public AudioPool(string name, int size, int audioSourcesPerInstance = 1)
	{
		_pool = new GameObjectPool<PooledAudioSource>(name);
		_allAudioPools.Add(this);
		_audioSourcesPerInstance = audioSourcesPerInstance;
		_size = size;
	}

	public void Populate(PooledAudioSource prefab)
	{
		AllAudioPoolItems.Add(_pool.PopulateOnce(prefab));
	}

	public static void InitializeAllPools(PooledAudioSource audioSourcePrefab)
	{
		AllAudioPoolItems = new List<PooledAudioSource>(512);
		AudioConfiguration configuration = AudioSettings.GetConfiguration();
		configuration.numRealVoices = Settings.CurrentData.RealVoices;
		configuration.numVirtualVoices = Settings.CurrentData.VirtualVoices;
		AudioSettings.Reset(configuration);
		foreach (AudioPool allAudioPool in _allAudioPools)
		{
			allAudioPool._pool.Initialize(allAudioPool._size);
			allAudioPool._pool.PopulateAll(audioSourcePrefab);
		}
	}
}
