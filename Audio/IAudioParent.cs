using Assets.Scripts.GridSystem;
using Sound;
using UnityEngine;

namespace Audio;

public interface IAudioParent
{
	Vector3 Position { get; }

	Transform Transform { get; }

	WorldGrid WorldGrid { get; }

	bool IsBeingDestroyed { get; }

	Transform SoundPosition { get; }

	void Add(PooledAudioSource pooledAudio);

	void Remove(PooledAudioSource pooledAudio);

	void OnDestroy();

	bool IsSoundLocal();
}
