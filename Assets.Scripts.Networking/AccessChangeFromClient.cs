using System;
using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class AccessChangeFromClient : ProcessedMessage<AccessChangeFromClient>
{
	public long ThingId;

	public int Access;

	public AccessChange Operation;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		Thing thing = Thing.Find<Thing>(ThingId);
		if (!(thing == null) && thing.HasAccessState)
		{
			switch (Operation)
			{
			case AccessChange.Add:
				thing.GiveAccess(Access);
				break;
			case AccessChange.Remove:
				thing.RemoveAccess(Access);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		Access = reader.ReadInt32();
		Operation = (AccessChange)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteInt32(Access);
		writer.WriteByte((byte)Operation);
	}
}
