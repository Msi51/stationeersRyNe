using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;
using Trading;
using UnityEngine;

namespace TerrainSystem;

public class MinablesGenerationData : DataCollection
{
	[XmlAttribute("OreDensity")]
	public float OreDensity = 1f;

	[XmlElement("OreVein")]
	public List<VeinGenerationData> VeinData = new List<VeinGenerationData>();

	[XmlElement("Conditions")]
	public List<ConditionDataCollection> ConditionCollections = new List<ConditionDataCollection>();

	[XmlElement("Region", typeof(RegionCondition))]
	[XmlElement("Difficulty", typeof(DifficultyCondition))]
	[XmlElement("Surface", typeof(SurfaceCondition))]
	[XmlElement("Depth", typeof(DepthCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	[XmlIgnore]
	public float TotalWeight;

	public const float MAX_ORE_DENSITY = 10f;

	public float OreDensityClamped()
	{
		return Mathf.Clamp(OreDensity, 0f, 10f);
	}

	public override bool IsValid()
	{
		return VeinData.Count > 0;
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
		if (!IsValid())
		{
			return;
		}
		DataCollection.Register(this, mod);
		bool flag = false;
		foreach (VeinGenerationData veinDatum in VeinData)
		{
			veinDatum.Initialize(mod);
			if (!veinDatum.IsValid())
			{
				flag = true;
			}
		}
		if (flag)
		{
			DataResolver.AddResolutionTask(new MinablesGenerationResolutionTask(this));
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

	public static VeinGenerationData GetRandomVeinType(MinablesGenerationData minablesData, System.Random random)
	{
		float num = (float)random.NextDouble() * minablesData.TotalWeight;
		foreach (VeinGenerationData veinDatum in minablesData.VeinData)
		{
			if (num <= veinDatum.Weight)
			{
				return veinDatum;
			}
			num -= veinDatum.Weight;
		}
		throw new IndexOutOfRangeException("Failed to get Random vein type");
	}

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ (int)(OreDensity * 65535f)) * 41;
		foreach (VeinGenerationData veinDatum in VeinData)
		{
			checksum = (checksum ^ veinDatum.GetChecksum()) * 41;
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			checksum = (checksum ^ conditionCollection.GetChecksum()) * 41;
		}
		foreach (ConditionData condition in Conditions)
		{
			checksum = (checksum ^ condition.GetChecksum()) * 41;
		}
		return checksum;
	}

	public void CalculateTotalWeight()
	{
		float num = 0f;
		foreach (VeinGenerationData veinDatum in VeinData)
		{
			num += veinDatum.Weight;
		}
		TotalWeight = num;
	}
}
