using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class EntityStateMessage : ProcessedMessage<EntityStateMessage>
{
	public long ThingId;

	public EntityState EntityState;

	public override void Process(long hostId)
	{
		if (!NetworkManager.IsClient)
		{
			Entity entity = Thing.Find<Entity>(ThingId);
			if (entity == null)
			{
				ConsoleWindow.PrintError($"EntityStateMessage: entity #{ThingId} not found");
			}
			else
			{
				entity.State = EntityState;
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		EntityState = (EntityState)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteByte((byte)EntityState);
	}
}
