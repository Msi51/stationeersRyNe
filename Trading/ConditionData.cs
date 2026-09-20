using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;

namespace Trading;

public abstract class ConditionData : IChecksum
{
	[XmlAttribute("Hidden")]
	public bool Hidden;

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Room", typeof(RoomCondition))]
	[XmlElement("Network", typeof(NetworkCondition))]
	[XmlElement("SurvivalProperty", typeof(SurvivalPropertyCondition))]
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
	[XmlElement("PreSpawned", typeof(PreSpawnedCondition))]
	[XmlElement("InCell", typeof(InCellCondition))]
	[XmlElement("Region", typeof(RegionCondition))]
	[XmlElement("Surface", typeof(SurfaceCondition))]
	[XmlElement("Depth", typeof(DepthCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	public const string SEPARATOR = "    ";

	public virtual string DebugName => GetType().Name;

	public virtual void Initialise()
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

	public virtual int GetChecksum()
	{
		int num = Conditions.Count;
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			num = (num ^ conditionCollection.GetChecksum()) * 41;
		}
		foreach (ConditionData condition in Conditions)
		{
			num = (num ^ condition.GetChecksum()) * 41;
		}
		return num;
	}

	public virtual bool Evaluate<T>(T t) where T : IEvaluable
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

	public void ToolTip(StringBuilder stringBuilder, int generations)
	{
		AppendToolTip(stringBuilder, generations);
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

	public virtual void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}

	protected virtual void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
	}

	public bool HasChildItem()
	{
		if (this is ChildItemPrefabCondition)
		{
			return true;
		}
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

	public void ToTreeString(TreeString myString)
	{
		TreeString myString2 = TreeString.Node(DebugName, myString);
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
			if (Evaluate(item))
			{
				return true;
			}
		}
		return false;
	}

	public void EvaluateToTree(TreeString myString, List<DynamicThing> inventory)
	{
		bool value = Evaluate(inventory);
		StringBuilder stringBuilder = new StringBuilder(DebugName);
		stringBuilder.Append(" is ").Append(value);
		TreeString myString2 = TreeString.Node(stringBuilder.ToString(), myString);
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
