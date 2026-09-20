using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class BlendShapeEmoteResetMessage : ProcessedMessage<BlendShapeEmoteResetMessage>
{
	public long HumanNetId;

	public override void Process(long hostId)
	{
		Human human = Thing.Find<Human>(HumanNetId);
		if ((bool)human)
		{
			if ((bool)InventoryManager.Parent && InventoryManager.Parent.ReferenceId != HumanNetId)
			{
				human.CosmeticsBehaviour.ResetExpressions();
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
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(HumanNetId);
	}
}
