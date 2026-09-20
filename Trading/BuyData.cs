using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;

namespace Trading;

public class BuyData : TransactionData
{
	[XmlElement("Required")]
	public StockData Required;

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Item")]
	public BuyItem BuyingItem;

	[XmlElement("Select")]
	public SelectBuyData SelectData;

	[XmlElement("Temperature", typeof(TemperatureComparableCondition))]
	[XmlElement("LogicType", typeof(LogicCondition))]
	[XmlElement("Interactable", typeof(InteractableCondition))]
	[XmlElement("Quantity", typeof(QuantityCondition))]
	[XmlElement("Decay", typeof(DecayCondition))]
	[XmlElement("Gas", typeof(GasCondition))]
	[XmlElement("Pressure", typeof(PressureCondition))]
	[XmlElement("TemperatureRange", typeof(TemperatureRangeCondition))]
	[XmlElement("Percent", typeof(PercentCondition))]
	[XmlElement("Moles", typeof(MoleCondition))]
	[XmlElement("Charge", typeof(EnergyCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	[XmlElement("Species", typeof(SpeciesCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ (Required?.GetChecksum() ?? 0)) * 41;
		checksum = (checksum ^ ConditionCollections.Count) * 41;
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			checksum = (checksum ^ (conditionCollection?.GetChecksum() ?? 0)) * 41;
		}
		checksum = (checksum ^ Conditions.Count) * 41;
		foreach (ConditionData condition in Conditions)
		{
			checksum = (checksum ^ (condition?.GetChecksum() ?? 0)) * 41;
		}
		checksum = (checksum ^ (BuyingItem?.GetChecksum() ?? 0)) * 41;
		return (checksum ^ (SelectData?.GetChecksum() ?? 0)) * 41;
	}

	public override void Apply(TransactionData transactionData)
	{
		base.Apply(transactionData);
		if (transactionData is BuyData buyData)
		{
			if (Required == null)
			{
				Required = buyData.Required;
			}
			ConditionCollections.AddRange(buyData.ConditionCollections);
			Conditions.AddRange(buyData.Conditions);
			BuyingItem = buyData.BuyingItem;
			SelectData = buyData.SelectData;
		}
	}

	public override void Initialize(ModAbout mod)
	{
		BuyingItem?.Initialize();
		if (SelectData?.TradeItems != null)
		{
			foreach (TradableItem tradeItem in SelectData.TradeItems)
			{
				tradeItem.Initialize();
			}
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.Initialise();
		}
		base.Initialize(mod);
	}

	public override bool IsValid()
	{
		if (!base.IsValid())
		{
			return false;
		}
		if (BuyingItem == null && SelectData == null && Conditions.Count > 0)
		{
			foreach (ConditionData condition in Conditions)
			{
				if (condition is GasCondition)
				{
					return true;
				}
			}
		}
		if (Required == null)
		{
			return false;
		}
		if (SelectData != null)
		{
			return SelectData.IsValid();
		}
		if (BuyingItem != null)
		{
			return BuyingItem.IsValid();
		}
		return false;
	}
}
