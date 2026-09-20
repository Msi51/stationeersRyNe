using Assets.Scripts.Inventory;
using TraderUI;

namespace Assets.Scripts.Networking;

public class TradingResultMessageFromServer : ProcessedMessage<TradingResultMessageFromServer>
{
	public bool Success;

	public int MessageKey;

	public ulong ClientId;

	public override void Deserialize(RocketBinaryReader reader)
	{
		Success = reader.ReadBoolean();
		MessageKey = reader.ReadInt32();
		ClientId = reader.ReadUInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(Success);
		writer.WriteInt32(MessageKey);
		writer.WriteUInt64(ClientId);
	}

	public override void Process(long hostId)
	{
		if ((bool)InventoryManager.ParentHuman?.OrganBrain && InventoryManager.ParentHuman.OrganBrain.ClientId == ClientId)
		{
			TraderCanvas.Instance.TradeResultMessage(Success, MessageKey);
		}
	}
}
