using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;

namespace Trading;

public class ConditionDataCollection : IChecksum
{
	[XmlAttribute("Hidden")]
	public bool Hidden;

	[XmlAttribute("Operator")]
	public LogicOperator LogicOperator;

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Room", typeof(RoomCondition))]
	[XmlElement("CustomName", typeof(CustomNameCondition))]
	[XmlElement("Prefab", typeof(ThingPrefabCondition))]
	[XmlElement("Contact", typeof(TraderContactCondition))]
	[XmlElement("Size", typeof(SizeCondition))]
	[XmlElement("Temperature", typeof(TemperatureComparableCondition))]
	[XmlElement("GrowthState", typeof(GrowthStateCondition))]
	[XmlElement("PlantStatus", typeof(PlantStatusCondition))]
	[XmlElement("PlantRecord", typeof(PlantRecordCondition))]
	[XmlElement("LogicType", typeof(LogicCondition))]
	[XmlElement("Reagents", typeof(ReagentCondition))]
	[XmlElement("BuildState", typeof(BuildStateCondition))]
	[XmlElement("Interactable", typeof(InteractableCondition))]
	[XmlElement("Decay", typeof(DecayCondition))]
	[XmlElement("Quantity", typeof(QuantityCondition))]
	[XmlElement("Gas", typeof(GasCondition))]
	[XmlElement("Pressure", typeof(PressureCondition))]
	[XmlElement("TemperatureRange", typeof(TemperatureRangeCondition))]
	[XmlElement("Item", typeof(ChildItemPrefabCondition))]
	[XmlElement("Percent", typeof(PercentCondition))]
	[XmlElement("Moles", typeof(MoleCondition))]
	[XmlElement("Charge", typeof(EnergyCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	[XmlElement("Species", typeof(SpeciesCondition))]
	[XmlElement("PreSpawned", typeof(PreSpawnedCondition))]
	[XmlElement("InCell", typeof(InCellCondition))]
	[XmlElement("Region", typeof(RegionCondition))]
	[XmlElement("Surface", typeof(SurfaceCondition))]
	[XmlElement("Depth", typeof(DepthCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	public int GetChecksum()
	{
		int logicOperator = (int)LogicOperator;
		logicOperator = (logicOperator ^ ConditionCollections.Count) * 41;
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			logicOperator = (logicOperator ^ conditionCollection.GetChecksum()) * 41;
		}
		logicOperator = (logicOperator ^ Conditions.Count) * 41;
		foreach (ConditionData condition in Conditions)
		{
			logicOperator = (logicOperator ^ condition.GetChecksum()) * 41;
		}
		return logicOperator;
	}

	public void Initialise()
	{
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.Initialise();
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
	}

	public bool Evaluate<T>(T t) where T : IEvaluable
	{
		switch (LogicOperator)
		{
		case LogicOperator.Any:
			foreach (ConditionDataCollection conditionCollection in ConditionCollections)
			{
				if (conditionCollection.Evaluate(t))
				{
					return true;
				}
			}
			foreach (ConditionData condition in Conditions)
			{
				if (condition.Evaluate(t))
				{
					return true;
				}
			}
			return false;
		case LogicOperator.None:
			foreach (ConditionDataCollection conditionCollection2 in ConditionCollections)
			{
				if (conditionCollection2.Evaluate(t))
				{
					return false;
				}
			}
			foreach (ConditionData condition2 in Conditions)
			{
				if (condition2.Evaluate(t))
				{
					return false;
				}
			}
			return true;
		default:
			foreach (ConditionDataCollection conditionCollection3 in ConditionCollections)
			{
				if (!conditionCollection3.Evaluate(t))
				{
					return false;
				}
			}
			foreach (ConditionData condition3 in Conditions)
			{
				if (!condition3.Evaluate(t))
				{
					return false;
				}
			}
			return true;
		}
	}

	public void ToolTip(StringBuilder stringBuilder, int generations)
	{
		string value = LogicOperator switch
		{
			LogicOperator.Any => GameStrings.TradeOperatorAny.DisplayString, 
			LogicOperator.None => GameStrings.TradeOperatorNone.DisplayString, 
			LogicOperator.All => GameStrings.TradeOperatorAll.DisplayString, 
			_ => string.Empty, 
		};
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(value.AsColor("white"));
		generations++;
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.ToolTip(stringBuilder, generations);
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.ToolTip(stringBuilder, generations);
		}
	}

	public bool HasChildItem()
	{
		foreach (ConditionData condition in Conditions)
		{
			if (condition.HasChildItem())
			{
				return true;
			}
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			if (conditionCollection.HasChildItem())
			{
				return true;
			}
		}
		return false;
	}

	public void ToTreeString(TreeString myString)
	{
		TreeString myString2 = TreeString.Node(LogicOperator.ToString(), myString);
		foreach (ConditionData condition in Conditions)
		{
			condition.ToTreeString(myString2);
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.ToTreeString(myString2);
		}
	}

	private bool Evaluate(List<DynamicThing> inventory)
	{
		foreach (DynamicThing item in inventory)
		{
			if (item is ITradable t && Evaluate(t))
			{
				return true;
			}
		}
		return false;
	}

	public void EvaluateToTree(TreeString myString, List<DynamicThing> inventory)
	{
		bool flag = Evaluate(inventory);
		TreeString myString2 = TreeString.Node($"{LogicOperator} is {flag}", myString);
		foreach (ConditionData condition in Conditions)
		{
			condition.EvaluateToTree(myString2, inventory);
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.EvaluateToTree(myString2, inventory);
		}
	}
}
