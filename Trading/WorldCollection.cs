using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

namespace Trading;

[XmlRoot("Worlds")]
public class WorldCollection : WorldConditionBase
{
	[XmlAttribute("Operator")]
	public LogicOperator Operator = LogicOperator.Any;

	[XmlElement("World")]
	public List<SerializedId> WorldIds = new List<SerializedId>(5);

	public override int GetChecksum()
	{
		int num = (int)Operator;
		num = (num ^ WorldIds.Count) * 41;
		foreach (SerializedId worldId in WorldIds)
		{
			num = (num ^ ((!string.IsNullOrEmpty(worldId)) ? Animator.StringToHash(worldId) : 0)) * 41;
		}
		return num;
	}

	public override bool Evaluate()
	{
		if (string.IsNullOrEmpty(WorldSetting.Current.Id))
		{
			return true;
		}
		return Evaluate(WorldSetting.Current);
	}

	public bool Evaluate(WorldSetting worldSetting)
	{
		switch (Operator)
		{
		case LogicOperator.All:
			foreach (SerializedId worldId in WorldIds)
			{
				if (Animator.StringToHash(worldId) != Animator.StringToHash(worldSetting.Id))
				{
					return false;
				}
			}
			return true;
		case LogicOperator.Any:
			foreach (SerializedId worldId2 in WorldIds)
			{
				if (Animator.StringToHash(worldId2) == Animator.StringToHash(worldSetting.Id))
				{
					return true;
				}
			}
			return false;
		case LogicOperator.None:
			foreach (SerializedId worldId3 in WorldIds)
			{
				if (Animator.StringToHash(worldId3) == Animator.StringToHash(worldSetting.Id))
				{
					return false;
				}
			}
			return true;
		default:
			return true;
		}
	}
}
