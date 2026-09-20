using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class AttackWithMessage : ProcessedMessage<AttackWithMessage>
{
	public long AttackParentId;

	public long TargetId;

	public byte ActiveHandSlotId;

	public byte OffHandSlotId;

	public Vector3 AttackPosition;

	public bool IsDestroy;

	public bool IsCopy;

	public float CompletionRatio;

	public override void Process(long hostId)
	{
		Thing thing = Thing.Find(AttackParentId);
		Slot activeHand = thing.Slots[ActiveHandSlotId];
		Thing thing2 = Thing.Find(TargetId);
		if ((bool)thing2)
		{
			Slot otherHand = thing.Slots[OffHandSlotId];
			Attack attack = new Attack(activeHand, otherHand, AttackPosition, thing2, CompletionRatio, null, IsDestroy, IsCopy);
			thing2.AttackWith(attack);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		AttackParentId = reader.ReadInt64();
		TargetId = reader.ReadInt64();
		ActiveHandSlotId = reader.ReadByte();
		OffHandSlotId = reader.ReadByte();
		AttackPosition = reader.ReadVector3();
		IsDestroy = reader.ReadBoolean();
		IsCopy = reader.ReadBoolean();
		CompletionRatio = reader.ReadFloatHalf();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(AttackParentId);
		writer.WriteInt64(TargetId);
		writer.WriteByte(ActiveHandSlotId);
		writer.WriteByte(OffHandSlotId);
		writer.WriteVector3(AttackPosition);
		writer.WriteBoolean(IsDestroy);
		writer.WriteBoolean(IsCopy);
		writer.WriteFloatHalf(CompletionRatio);
	}
}
