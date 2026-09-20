using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class ChatStatusMessage : ProcessedMessage<ChatStatusMessage>
{
	public long HumanId;

	public bool IsTyping;

	public override void Process(long hostId)
	{
		if (!(InventoryManager.Parent != null) || InventoryManager.Parent.netId != HumanId)
		{
			Human human = Thing.Find<Human>(HumanId);
			if (!(human == null))
			{
				human.SetChatStatus(IsTyping);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanId = reader.ReadInt64();
		IsTyping = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanId);
		writer.WriteBoolean(IsTyping);
	}
}
