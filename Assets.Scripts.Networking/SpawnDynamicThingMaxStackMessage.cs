namespace Assets.Scripts.Networking;

public class SpawnDynamicThingMaxStackMessage : ProcessedMessage<SpawnDynamicThingMaxStackMessage>
{
	public long ParentId { get; set; }

	public string PrefabName { get; set; }

	public override void Process(long hostId)
	{
		base.Process(hostId);
		OnServer.SpawnDynamicThingMaxStack(ParentId, PrefabName);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ParentId = reader.ReadInt64();
		PrefabName = reader.ReadString();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentId);
		writer.WriteString(PrefabName);
	}
}
