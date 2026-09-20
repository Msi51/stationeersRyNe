using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class RequestUseItemToServer : ProcessedMessage<RequestUseItemToServer>
{
	public long ParentReferenceId;

	public byte ActiveHandSlotId;

	private byte _completedRatio;

	public float CompletedRation
	{
		get
		{
			return (float)(int)_completedRatio / 100f;
		}
		set
		{
			_completedRatio = (byte)Mathf.Floor(value * 100f);
		}
	}

	public override void Process(long hostId)
	{
		OnServer.UseItemSecondary(Thing.Find<Thing>(ParentReferenceId), ActiveHandSlotId, CompletedRation);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ParentReferenceId = reader.ReadInt64();
		ActiveHandSlotId = reader.ReadByte();
		_completedRatio = reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentReferenceId);
		writer.WriteByte(ActiveHandSlotId);
		writer.WriteByte(_completedRatio);
	}
}
