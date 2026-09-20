using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;

namespace Assets.Scripts.Networking;

public class HumanIdentityMessage : ProcessedMessage<HumanIdentityMessage>
{
	public long HumanId;

	public PlayerCosmetics Cosmetics;

	public override void Process(long hostId)
	{
		Human human = Thing.Find<Human>(HumanId);
		human.CosmeticData = Cosmetics;
		human.CosmeticsBehaviour.UpdateIdentity(Cosmetics);
		human.NetworkUpdateFlags |= 2048;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanId = reader.ReadInt64();
		Cosmetics = new PlayerCosmetics();
		Cosmetics.Read(reader);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanId);
		Cosmetics.Write(writer);
	}
}
