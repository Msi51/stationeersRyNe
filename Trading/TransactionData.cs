using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Util;

namespace Trading;

public abstract class TransactionData : DataCollection
{
	[XmlAttribute("Value")]
	public float Value = 1f;

	[XmlElement("Type")]
	public List<SlotIdReference> SlotTypes = new List<SlotIdReference>();

	[XmlElement("Thumbnail")]
	public StringReference Thumbnail;

	[XmlAttribute("CustomToolTipKey")]
	public string CustomToolTipGameStringKey;

	[XmlElement("RandomPool")]
	public RandomPoolData ItemPoolData;

	[XmlElement("Chance")]
	public ChanceData ChanceData;

	[XmlElement("World", typeof(WorldCondition))]
	[XmlElement("Worlds", typeof(WorldCollection))]
	public WorldConditionBase WorldCondition;

	public float GetCost()
	{
		return Value;
	}

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ (int)(Value * 1000f)) * 41;
		foreach (SlotIdReference slotType in SlotTypes)
		{
			checksum = (checksum ^ slotType.GetChecksum()) * 41;
		}
		checksum = (checksum ^ (ItemPoolData?.GetChecksum() ?? 0)) * 41;
		checksum = (checksum ^ (ChanceData?.GetChecksum() ?? 0)) * 41;
		return (checksum ^ (WorldCondition?.GetChecksum() ?? 0)) * 41;
	}

	public virtual void Apply(TransactionData transactionData)
	{
		if (transactionData != null)
		{
			if (base.IdHash != transactionData.IdHash)
			{
				ConsoleWindow.PrintError("Error");
			}
			if (RocketMath.Approximately(Value, 1f))
			{
				Value = transactionData.Value;
			}
			if (Thumbnail == null)
			{
				Thumbnail = transactionData.Thumbnail;
			}
			if (Name == null)
			{
				Name = transactionData.Name;
			}
			if (string.IsNullOrEmpty(CustomToolTipGameStringKey))
			{
				CustomToolTipGameStringKey = transactionData.CustomToolTipGameStringKey;
			}
			if (ItemPoolData == null)
			{
				ItemPoolData = transactionData.ItemPoolData;
			}
			if (ChanceData == null)
			{
				ChanceData = transactionData.ChanceData;
			}
			if (WorldCondition == null)
			{
				WorldCondition = transactionData.WorldCondition;
			}
		}
	}

	public override void Initialize(ModAbout mod)
	{
		if (IsValid() && !string.IsNullOrEmpty(Id))
		{
			DataCollection.Register(this, mod);
		}
		foreach (SlotIdReference slotType in SlotTypes)
		{
			slotType.Initialise();
		}
	}
}
