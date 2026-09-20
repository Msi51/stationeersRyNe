using System;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts;

public readonly struct AudioEvent : ISyncListable
{
	private enum EventType : byte
	{
		AudioParent,
		WorldPosition
	}

	public static SyncList<AudioEvent> NewEvents;

	private readonly EventType _type;

	private readonly Vector3 _worldPosition;

	private readonly long _referenceId;

	private readonly int _soundHash;

	private static void DeserializeEvent(RocketBinaryReader reader)
	{
		new AudioEvent(reader).Execute();
	}

	public static async UniTaskVoid Schedule(IReferencable parent, int soundHash, bool execute = true)
	{
		await UniTask.SwitchToMainThread();
		Create(parent, soundHash, execute);
	}

	public static async UniTaskVoid Schedule(long referenceId, int soundHash, bool execute = true)
	{
		await UniTask.SwitchToMainThread();
		Create(referenceId, soundHash, execute);
	}

	public static async UniTaskVoid Schedule(Vector3 worldPosition, int soundHash, bool execute = true)
	{
		await UniTask.SwitchToMainThread();
		Create(worldPosition, soundHash, execute);
	}

	public static void Create(IReferencable parent, int soundHash, bool execute = true)
	{
		Create(parent.ReferenceId, soundHash, execute);
	}

	public static void Create(long referenceId, int soundHash, bool execute = true)
	{
		if (ThreadedManager.IsThread)
		{
			Schedule(referenceId, soundHash, execute).Forget();
			return;
		}
		AudioEvent newItem = new AudioEvent(referenceId, soundHash);
		if (execute)
		{
			newItem.Execute();
		}
		if (Client.Count != 0)
		{
			NewEvents.Add(newItem);
		}
	}

	public static void Create(Vector3 worldPosition, int soundHash, bool execute = true)
	{
		if (ThreadedManager.IsThread)
		{
			Schedule(worldPosition, soundHash, execute).Forget();
			return;
		}
		AudioEvent newItem = new AudioEvent(worldPosition, soundHash);
		if (execute)
		{
			newItem.Execute();
		}
		if (Client.Count != 0)
		{
			NewEvents.Add(newItem);
		}
	}

	private AudioEvent(long referenceId, int soundHash)
	{
		_type = EventType.AudioParent;
		_referenceId = referenceId;
		_soundHash = soundHash;
		_worldPosition = Vector3.zero;
	}

	private AudioEvent(Vector3 worldPosition, int soundHash)
	{
		_type = EventType.WorldPosition;
		_worldPosition = worldPosition;
		_soundHash = soundHash;
		_referenceId = 0L;
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)_type);
		writer.WriteInt32(_soundHash);
		switch (_type)
		{
		case EventType.AudioParent:
			Network.WritePackedId(writer, _referenceId);
			break;
		case EventType.WorldPosition:
			writer.WriteVector3(_worldPosition);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public AudioEvent(RocketBinaryReader reader)
	{
		_type = (EventType)reader.ReadByte();
		_soundHash = reader.ReadInt32();
		switch (_type)
		{
		case EventType.AudioParent:
			Network.ReadPackedId(reader, out _referenceId);
			_worldPosition = Vector3.zero;
			break;
		case EventType.WorldPosition:
			_worldPosition = reader.ReadVector3();
			_referenceId = 0L;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void Execute()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		switch (_type)
		{
		case EventType.AudioParent:
			if (!(Thing.Find<IReferencable>(_referenceId) is IAudioParent audioParent))
			{
				ConsoleWindow.Print("warning cannot play audio event '" + StringManager.Get(_soundHash) + "' for referencable '" + StringManager.Get(_referenceId) + "' because it does not exist");
			}
			else
			{
				Singleton<AudioManager>.Instance.PlayAudioClipsData(audioParent, _soundHash, audioParent.SoundPosition);
			}
			break;
		case EventType.WorldPosition:
			Singleton<AudioManager>.Instance.PlayAudioClipsData(_soundHash, _worldPosition);
			break;
		}
	}

	static AudioEvent()
	{
		NewEvents = new SyncList<AudioEvent>(DeserializeEvent);
	}
}
