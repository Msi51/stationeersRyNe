using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class RequestTradeMessage : ProcessedMessage<RequestTradeMessage>
{
	public struct ItemToTrade
	{
		public int TradeItemSerialNumber;

		public bool add;

		public int QuantityToPurchase;

		public void Read(RocketBinaryReader reader)
		{
			TradeItemSerialNumber = reader.ReadInt32();
			add = reader.ReadBoolean();
			QuantityToPurchase = reader.ReadInt32();
		}

		public void Write(RocketBinaryWriter writer)
		{
			writer.WriteInt32(TradeItemSerialNumber);
			writer.WriteBoolean(add);
			writer.WriteInt32(QuantityToPurchase);
		}
	}

	public long ContactReferenceId;

	public long ParentMotherboardReferenceId;

	public long CreditCardReferenceId;

	public float NewPlayerCurrency;

	public float TradersNewCurrency;

	public List<ItemToTrade> ItemsToTrade;

	public override void Process(long hostId)
	{
		Thing.Find<CommsMotherboard>(ParentMotherboardReferenceId);
		Thing.Find<CreditCard>(CreditCardReferenceId);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ContactReferenceId = reader.ReadInt64();
		ParentMotherboardReferenceId = reader.ReadInt64();
		CreditCardReferenceId = reader.ReadInt64();
		NewPlayerCurrency = reader.ReadSingle();
		TradersNewCurrency = reader.ReadSingle();
		ItemsToTrade = new List<ItemToTrade>();
		short num = reader.ReadInt16();
		for (int i = 0; i < num; i++)
		{
			ItemToTrade item = default(ItemToTrade);
			item.Read(reader);
			ItemsToTrade.Add(item);
		}
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ContactReferenceId);
		writer.WriteInt64(ParentMotherboardReferenceId);
		writer.WriteInt64(CreditCardReferenceId);
		writer.WriteSingle(NewPlayerCurrency);
		writer.WriteSingle(TradersNewCurrency);
		int count = ItemsToTrade.Count;
		writer.WriteInt16((short)count);
		for (int i = 0; i < count; i++)
		{
			ItemsToTrade[i].Write(writer);
		}
	}

	public override string ToString()
	{
		return $"ContactIndex:{ContactReferenceId} - ParentMotherboardReferenceId:{ParentMotherboardReferenceId} - CreditCardReferenceId:{CreditCardReferenceId} - NewPlayerCurrency:{NewPlayerCurrency} - TradersNewCurrency:{TradersNewCurrency}";
	}
}
