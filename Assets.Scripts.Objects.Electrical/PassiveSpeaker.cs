using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class PassiveSpeaker : Device, ISmartRotatable, ISoundAlert, IAudioInput, ILogicable, IReferencable, IEvaluable
{
	private static readonly Vector3 SpeakerAudioOffset = new Vector3(0f, 0.5f, 0.3f);

	private const float AUDIBLE_SQUARE_DISTANCE = 1700f;

	public const float NORMALISED_VOLUME = 50f;

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public IAudioInput AudioOutput => null;

	public byte SoundVolume
	{
		get
		{
			return _soundVolume;
		}
		set
		{
			_soundVolume = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			if (_playingAudio != null)
			{
				_playingAudio.GameAudioSource.SetVolumeMultiplier(AudioManager.Find((SoundAlert)SoundAlert).NameHash, (float)(int)SoundVolume / 100f);
			}
		}
	}

	public byte SoundAlert
	{
		get
		{
			return _soundAlert;
		}
		set
		{
			_soundAlert = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			if (!GameManager.IsBatchMode)
			{
				WaitThenPlay().Forget();
			}
		}
	}

	public PooledAudioSource InputAudioScheduled(int clipsDataHash, double startTime, double endTime, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		if (InventoryManager.ParentHuman != null && Vector3.SqrMagnitude(base.Position - InventoryManager.ParentHuman.Position) < 1700f)
		{
			return Singleton<AudioManager>.Instance.PlayScheduledAudioClipsData(this, clipsDataHash, startTime, endTime, SpeakerAudioOffset, volumeMultiplier * ((float)(int)SoundVolume / 100f), pitchMultiplier, attack, release);
		}
		return null;
	}

	public PooledAudioSource InputAudio(int clipsDataHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		return Singleton<AudioManager>.Instance.PlayAudioClipsData(this, clipsDataHash, SpeakerAudioOffset, null, volumeMultiplier * ((float)(int)SoundVolume / 50f), pitchMultiplier);
	}

	private async UniTaskVoid WaitThenPlay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (_playingAudio != null && PooledAudioSources.Contains(_playingAudio))
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		if (SoundAlert != 0)
		{
			_playingAudio = Thing.PlayPooledAudioSound(this);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte(SoundVolume);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte(SoundAlert);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			SoundVolume = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SoundAlert = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(SoundVolume);
		writer.WriteByte(SoundAlert);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SoundVolume = reader.ReadByte();
		SoundAlert = reader.ReadByte();
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Volume => (int)SoundVolume, 
			LogicType.SoundAlert => (int)SoundAlert, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Volume:
			SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100);
			break;
		case LogicType.SoundAlert:
			SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1);
			break;
		}
	}
}
