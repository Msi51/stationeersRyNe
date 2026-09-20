using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using Reagents;
using Trading;

namespace TerrainSystem;

public class DeepMinablesGenerationData : DataCollection
{
	[XmlElement("Region", typeof(RegionCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Quantity")]
	public IntRangeData Quantity;

	[XmlElement("Time")]
	public IntRangeData Time;

	[XmlElement("ReagentMix")]
	public ReagentAction ReagentAction;

	private static readonly Random _random = new Random();

	public override bool IsValid()
	{
		if (Quantity != null && Time != null)
		{
			return ReagentAction != null;
		}
		return false;
	}

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
		ReagentAction?.Initialize();
		if (IsValid())
		{
			DataCollection.Register(this, mod);
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

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Quantity.GetChecksum()) * 41;
	}

	public void SetValues(DirtyOre minedOre)
	{
		ReagentAction?.Execute(minedOre);
		minedOre.SetQuantity(Quantity.GenerateValue(_random));
	}

	public float GetTimeToMine()
	{
		return Time.GenerateValue(_random);
	}
}
