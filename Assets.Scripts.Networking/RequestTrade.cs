using Assets.Scripts.Localization2;
using TraderUI;
using Trading;

namespace Assets.Scripts.Networking;

public class RequestTrade : ProcessedMessage<RequestTrade>
{
	public long TransactionDataInstanceId;

	public long CreditCardId;

	public long TraderContactId;

	public ulong PlayerId;

	public int Amount;

	public float Cost;

	public bool Buying;

	public override void Process(long hostId)
	{
		if (NetworkManager.IsServer)
		{
			CreditCard creditCard = Referencable.Find<CreditCard>(CreditCardId);
			TraderContact contact = Referencable.Find<TraderContact>(TraderContactId);
			Assets.Scripts.Localization2.GameString errorMessage;
			bool success = ((!Buying) ? TradeDataHelper.SellItem(Referencable.Find<BuyDataInstance>(TransactionDataInstanceId), creditCard, contact, Amount, Cost, out errorMessage) : TradeDataHelper.BuyItem(Referencable.Find<SellDataInstance>(TransactionDataInstanceId), creditCard, contact, Amount, Cost, out errorMessage));
			TradingResultMessageFromServer tradingResultMessageFromServer = new TradingResultMessageFromServer();
			tradingResultMessageFromServer.Success = success;
			tradingResultMessageFromServer.MessageKey = errorMessage?.Key ?? 0;
			tradingResultMessageFromServer.ClientId = PlayerId;
			tradingResultMessageFromServer.SendToClients();
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		TransactionDataInstanceId = reader.ReadInt64();
		CreditCardId = reader.ReadInt64();
		TraderContactId = reader.ReadInt64();
		PlayerId = reader.ReadUInt64();
		Amount = reader.ReadInt32();
		Cost = reader.ReadSingle();
		Buying = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(TransactionDataInstanceId);
		writer.WriteInt64(CreditCardId);
		writer.WriteInt64(TraderContactId);
		writer.WriteUInt64(PlayerId);
		writer.WriteInt32(Amount);
		writer.WriteSingle(Cost);
		writer.WriteBoolean(Buying);
	}
}
