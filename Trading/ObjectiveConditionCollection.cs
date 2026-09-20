using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using Networks;
using Objects.RoboticArm;

namespace Trading;

public class ObjectiveConditionCollection : IChecksum
{
	[XmlAttribute("Hidden")]
	public bool Hidden;

	[XmlAttribute("Operator")]
	public LogicOperator LogicOperator;

	[XmlElement("Conditions")]
	public List<ObjectiveConditionCollection> ConditionCollections = new List<ObjectiveConditionCollection>();

	[XmlElement("Contact", typeof(TraderContactCondition))]
	[XmlElement("Network", typeof(NetworkCondition))]
	[XmlElement("Room", typeof(RoomCondition))]
	[XmlElement("Prefab", typeof(ThingPrefabCondition))]
	[XmlElement("ObjectiveComplete", typeof(ObjectiveCompleteCondition))]
	[XmlElement("Player", typeof(EntityStateCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	public int GetChecksum()
	{
		int logicOperator = (int)LogicOperator;
		logicOperator = (logicOperator ^ Conditions.Count) * 41;
		foreach (ConditionData condition in Conditions)
		{
			logicOperator = (logicOperator ^ condition.GetChecksum()) * 41;
		}
		return logicOperator;
	}

	public void Initialise()
	{
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
		foreach (ObjectiveConditionCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.Initialise();
		}
	}

	public void GetEvaluatePrefabHashes(ref List<int> prefabHashes, ref List<Type> types)
	{
		foreach (ConditionData condition in Conditions)
		{
			if (!(condition is EntityStateCondition))
			{
				if (!(condition is ObjectiveCompleteCondition))
				{
					if (!(condition is RoomCondition))
					{
						if (!(condition is ThingPrefabCondition thingPrefabCondition))
						{
							if (!(condition is NetworkCondition networkCondition))
							{
								if (!(condition is TraderContactCondition))
								{
									if (!(condition is CursorThingCondition cursorThingCondition))
									{
										throw new ArgumentOutOfRangeException("condition");
									}
									prefabHashes.Add(cursorThingCondition.PrefabIdHash);
								}
								else
								{
									types.Add(typeof(TraderContact));
								}
							}
							else if (networkCondition.NetworkType == StructureNetworkType.Cable)
							{
								types.Add(typeof(CableNetwork));
							}
							else if (networkCondition.NetworkType != StructureNetworkType.None)
							{
								switch (networkCondition.NetworkType)
								{
								case StructureNetworkType.LandingPad:
									types.Add(typeof(LandingPadNetwork));
									break;
								case StructureNetworkType.Pipe:
									types.Add(typeof(PipeNetwork));
									break;
								case StructureNetworkType.Chute:
									types.Add(typeof(ChuteNetwork));
									break;
								case StructureNetworkType.Rocket:
									types.Add(typeof(RocketNetwork));
									break;
								case StructureNetworkType.RoboticArm:
									types.Add(typeof(RoboticArmNetwork));
									break;
								default:
									throw new ArgumentOutOfRangeException();
								}
							}
						}
						else
						{
							prefabHashes.Add(thingPrefabCondition.PrefabNameHash);
						}
					}
					else
					{
						types.Add(typeof(Room));
					}
				}
				else
				{
					types.Add(typeof(WorldObjectiveState));
				}
			}
			else
			{
				types.Add(typeof(Human));
			}
		}
		foreach (ObjectiveConditionCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.GetEvaluatePrefabHashes(ref prefabHashes, ref types);
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
		foreach (ObjectiveConditionCollection conditionCollection in ConditionCollections)
		{
			conditionCollection.ToolTip(stringBuilder, generations);
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.ToolTip(stringBuilder, generations);
		}
	}

	public bool Evaluate<T>(List<T> tList) where T : IEvaluable
	{
		switch (LogicOperator)
		{
		case LogicOperator.Any:
			foreach (ObjectiveConditionCollection conditionCollection in ConditionCollections)
			{
				if (conditionCollection.Evaluate(tList))
				{
					return true;
				}
			}
			foreach (ConditionData condition in Conditions)
			{
				foreach (T t in tList)
				{
					if (condition.Evaluate(t))
					{
						return true;
					}
				}
			}
			return false;
		case LogicOperator.None:
			foreach (ObjectiveConditionCollection conditionCollection2 in ConditionCollections)
			{
				if (conditionCollection2.Evaluate(tList))
				{
					return false;
				}
			}
			foreach (T t2 in tList)
			{
				foreach (ConditionData condition2 in Conditions)
				{
					if (condition2.Evaluate(t2))
					{
						return false;
					}
				}
			}
			return true;
		default:
			foreach (ObjectiveConditionCollection conditionCollection3 in ConditionCollections)
			{
				if (!conditionCollection3.Evaluate(tList))
				{
					return false;
				}
			}
			foreach (ConditionData condition3 in Conditions)
			{
				bool flag = false;
				foreach (T t3 in tList)
				{
					if (condition3.Evaluate(t3))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}
	}
}
