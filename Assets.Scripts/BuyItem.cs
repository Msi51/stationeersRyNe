using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Trading;

namespace Assets.Scripts;

[XmlType("BuyItem")]
public class BuyItem : TradableItem
{
	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Temperature", typeof(TemperatureComparableCondition))]
	[XmlElement("Reagents", typeof(ReagentCondition))]
	[XmlElement("Interactable", typeof(InteractableCondition))]
	[XmlElement("Quantity", typeof(QuantityCondition))]
	[XmlElement("Decay", typeof(DecayCondition))]
	[XmlElement("Gas", typeof(GasCondition))]
	[XmlElement("Pressure", typeof(PressureCondition))]
	[XmlElement("TemperatureRange", typeof(TemperatureRangeCondition))]
	[XmlElement("Percent", typeof(PercentCondition))]
	[XmlElement("Item", typeof(ChildItemPrefabCondition))]
	[XmlElement("Moles", typeof(MoleCondition))]
	[XmlElement("Charge", typeof(EnergyCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	[XmlElement("Species", typeof(SpeciesCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ ConditionCollections.Count) * 41;
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			checksum = (checksum ^ conditionCollection.GetChecksum()) * 41;
		}
		checksum = (checksum ^ Conditions.Count) * 41;
		foreach (ConditionData condition in Conditions)
		{
			checksum = (checksum ^ condition.GetChecksum()) * 41;
		}
		return checksum;
	}

	public override void Initialize()
	{
		base.Initialize();
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.Initialise();
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
	}

	public bool BuyConditionsMet<T>(T t) where T : IEvaluable
	{
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			if (!conditionCollection.Evaluate(t))
			{
				return false;
			}
		}
		foreach (ConditionData condition in Conditions)
		{
			if (!condition.Evaluate(t))
			{
				return false;
			}
		}
		return true;
	}

	public bool HasChildItem()
	{
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			if (conditionCollection.HasChildItem())
			{
				return true;
			}
		}
		foreach (ConditionData condition in Conditions)
		{
			if (condition.HasChildItem())
			{
				return true;
			}
		}
		return false;
	}

	public void ToolTip(StringBuilder stringBuilder)
	{
		int num = 0;
		if (HasChildItem())
		{
			stringBuilder.AppendLine(GameStrings.TradeItemParent.AsString(Prefab.ToTooltip()));
			num++;
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.ToolTip(stringBuilder, num);
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.ToolTip(stringBuilder, num);
		}
	}

	private bool Evaluate(List<DynamicThing> inventory)
	{
		foreach (DynamicThing item in inventory)
		{
			if (item is ITradable tradable && ((object)Prefab == null || tradable.GetPrefabHash() == Prefab.GetPrefabHash()) && BuyConditionsMet(tradable))
			{
				return true;
			}
		}
		return false;
	}

	public void EvaluateToTree(TreeString contactString, List<DynamicThing> inventory)
	{
		bool flag = Evaluate(inventory);
		TreeString myString = TreeString.Node($"{PrefabName} is {flag}", contactString);
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.EvaluateToTree(myString, inventory);
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.EvaluateToTree(myString, inventory);
		}
	}
}
