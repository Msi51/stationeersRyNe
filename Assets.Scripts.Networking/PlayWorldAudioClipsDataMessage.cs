using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class PlayWorldAudioClipsDataMessage : ProcessedMessage<PlayWorldAudioClipsDataMessage>
{
	public int ClipsDataNameHash;

	public Vector3 Position;

	public float VolumeMultiplier = 1f;

	public float PitchMultiplier = 1f;

	public override void Process(long hostId)
	{
		if ((bool)Singleton<AudioManager>.Instance)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(ClipsDataNameHash, Position, VolumeMultiplier, PitchMultiplier);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ClipsDataNameHash = reader.ReadInt32();
		Position = reader.ReadVector3();
		VolumeMultiplier = reader.ReadSingle();
		PitchMultiplier = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(ClipsDataNameHash);
		writer.WriteVector3(Position);
		writer.WriteSingle(VolumeMultiplier);
		writer.WriteSingle(PitchMultiplier);
	}
}
