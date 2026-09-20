using Assets.Scripts.Sound;
using Assets.Scripts.Util;

namespace Assets.Scripts.Networking;

public class PlayAudioClipsDataMessage : ProcessedMessage<PlayAudioClipsDataMessage>
{
	public long ParentId;

	public int ClipsDataNameHash;

	public override void Process(long hostId)
	{
		if ((bool)Singleton<AudioManager>.Instance)
		{
			AudioEvent.Create(ParentId, ClipsDataNameHash);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ParentId = reader.ReadInt64();
		ClipsDataNameHash = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentId);
		writer.WriteInt32(ClipsDataNameHash);
	}
}
