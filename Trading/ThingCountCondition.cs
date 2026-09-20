using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Trading;

public class ThingCountCondition : ConditionComparable
{
	[XmlAttribute("Id")]
	public string PrefabName;

	[XmlAttribute("Count")]
	public int Count;

	[XmlIgnore]
	public int PrefabNameHash;

	public override string DebugName => "Item " + PrefabName;

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ ((!string.IsNullOrEmpty(PrefabName)) ? Animator.StringToHash(PrefabName) : 0)) * 41;
	}

	public override void Initialise()
	{
		base.Initialise();
		if (!string.IsNullOrEmpty(PrefabName))
		{
			PrefabNameHash = Animator.StringToHash(PrefabName);
		}
	}

	public virtual bool Evaluate<T>(List<T> tList) where T : IEvaluable
	{
		int num = 0;
		foreach (T t in tList)
		{
			if (!(t is Thing thing) || thing.PrefabHash != PrefabNameHash)
			{
				continue;
			}
			bool flag = true;
			foreach (ConditionDataCollection conditionCollection in ConditionCollections)
			{
				if (!conditionCollection.Evaluate(t))
				{
					flag = false;
				}
			}
			foreach (ConditionData condition in Conditions)
			{
				if (!condition.Evaluate(t))
				{
					flag = false;
				}
			}
			if (flag)
			{
				num++;
			}
		}
		return Compare(num, Count);
	}

	public override bool Evaluate<T>(T t)
	{
		return true;
	}
}
