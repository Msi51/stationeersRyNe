using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Trading;

public class PlayableAreaData : DataCollection
{
	[XmlAttribute("Rule")]
	public PlayableAreaRule Rule;

	[XmlElement("Region", typeof(RegionCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	public override void Initialize(ModAbout mod)
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
}
