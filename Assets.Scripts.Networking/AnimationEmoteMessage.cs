using Assets.Scripts.Emotes;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class AnimationEmoteMessage : ProcessedMessage<AnimationEmoteMessage>
{
	public long HumanNetId;

	public int AnimationIndex;

	public int Delay;

	public byte EmoteType;

	public override void Process(long hostId)
	{
		Human human = Thing.Find<Human>(HumanNetId);
		if ((bool)human)
		{
			if ((bool)InventoryManager.Parent && InventoryManager.Parent.ReferenceId != HumanNetId)
			{
				human.EmoteController?.DoEmote(new EmoteData(AnimationIndex, Delay, (EmoteType)EmoteType));
			}
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanNetId = reader.ReadInt64();
		AnimationIndex = reader.ReadInt32();
		Delay = reader.ReadInt32();
		EmoteType = reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanNetId);
		writer.WriteInt32(AnimationIndex);
		writer.WriteInt32(Delay);
		writer.WriteByte(EmoteType);
	}
}
