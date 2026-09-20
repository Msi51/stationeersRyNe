using System;
using Assets.Scripts;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sound;

public class PooledAudioSource : GameBase, IListable, IGamePoolable<PooledAudioSource>, IPoolable<PooledAudioSource>
{
	[FormerlySerializedAs("GameAudioSource")]
	[SerializeField]
	private GameAudioSource gameAudioSource;

	private Vector3 _position;

	private GameObjectPool<PooledAudioSource> _pool;

	public GameAudioSource GameAudioSource => gameAudioSource;

	public bool IsPlaying => GameAudioSource?.isPlaying ?? false;

	public Vector3 Position
	{
		get
		{
			if (!GameManager.IsThread)
			{
				return Transform.position;
			}
			return _position;
		}
		set
		{
			Transform.position = value;
			_position = value;
		}
	}

	public int PoolId { get; set; }

	public string DebugName { get; set; }

	public bool IsActive
	{
		get
		{
			return IsPlaying;
		}
		set
		{
		}
	}

	public ObjectPool<PooledAudioSource> Pool { get; set; }

	public GameObjectPool<PooledAudioSource> GamePool { get; set; }

	public void SetPosition(Vector3 worldPosition)
	{
		Position = worldPosition;
		GameObject.transform.position = worldPosition;
	}

	public void Stop(bool immediate = false)
	{
		GameAudioSource?.Stop(immediate);
	}

	public void Stop(int clipsDataHash)
	{
		GameAudioSource?.Stop(clipsDataHash);
	}

	public void ReturnToPool()
	{
		GameAudioSource.Parent?.Remove(this);
		GamePool.Return(this);
	}

	private void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			Debug.LogError("error pooled AudioSource '" + DebugName + "' was destroyed");
		}
	}

	public void DrawInList(ref int index)
	{
		throw new NotImplementedException();
	}
}
