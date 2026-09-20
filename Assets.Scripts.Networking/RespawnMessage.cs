using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;

namespace Assets.Scripts.Networking;

public class RespawnMessage : ProcessedMessage<RespawnMessage>
{
	private ulong HumanId { get; set; }

	private PlayerCosmetics Cosmetics { get; set; }

	public RespawnMessage()
	{
	}

	public RespawnMessage(ulong humanId, PlayerCosmetics cosmetics)
	{
		HumanId = humanId;
		Cosmetics = cosmetics;
	}

	public override void Process(long hostId)
	{
		Client client = Client.Find(HumanId);
		SerializedClientInfo clientInfo = GameManager.GetClientInfo(HumanId);
		bool isRespawn = clientInfo != null;
		StartLocationData startLocation = ((clientInfo != null) ? DataCollection.Get<StartLocationData>(clientInfo.StartLocationHash) : null);
		ISpawnPoint spawnPoint = ((clientInfo != null) ? Referencable.Find<ISpawnPoint>(clientInfo.SpawnPointReference) : null);
		Human.CreateCharacter(HumanId, client?.name ?? "Unknown", Cosmetics, isRespawn, startLocation, spawnPoint);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanId = reader.ReadUInt64();
		if (Cosmetics == null)
		{
			PlayerCosmetics playerCosmetics = (Cosmetics = new PlayerCosmetics());
		}
		Cosmetics.Read(reader);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteUInt64(HumanId);
		if (Cosmetics == null)
		{
			PlayerCosmetics playerCosmetics = (Cosmetics = new PlayerCosmetics());
		}
		Cosmetics.Write(writer);
	}
}
