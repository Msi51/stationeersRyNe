using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Entities;
using Networks;
using Objects.RoboticArm;

namespace Trading;

public class WorldObjective : DataCollection
{
	[XmlElement("Info")]
	public LocalizedStringReference Info;

	[XmlElement("Notice")]
	public List<LocalizedStringReference> Notices = new List<LocalizedStringReference>();

	[XmlElement("Trigger")]
	public ObjectiveConditionCollection TriggerConditions;

	[XmlAttribute("Expand")]
	public bool ExpandOnTrigger;

	[XmlAttribute("Time")]
	public float Time;

	[XmlElement("CursorThing", typeof(CursorThingCondition))]
	[XmlElement("Contact", typeof(TraderContactCondition))]
	[XmlElement("Network", typeof(NetworkCondition))]
	[XmlElement("ObjectiveComplete", typeof(ObjectiveCompleteCondition))]
	[XmlElement("Prefab", typeof(ThingPrefabCondition))]
	[XmlElement("Room", typeof(RoomCondition))]
	[XmlElement("Player", typeof(EntityStateCondition))]
	[XmlElement("ThingCount", typeof(ThingCountCondition))]
	public List<ConditionData> Conditions = new List<ConditionData>();

	[XmlElement("Conditions")]
	public List<ObjectiveConditionCollection> PrefabConditionCollections = new List<ObjectiveConditionCollection>();

	[XmlElement("CompleteTutorialPopup", typeof(CompletedTutorialPopupAction))]
	[XmlElement("Popup", typeof(PopupAction))]
	public List<ActionData> ObjectiveActions = new List<ActionData>();

	public override bool IsValid()
	{
		if (Conditions.Count <= 0 && PrefabConditionCollections.Count <= 0)
		{
			return Name != null;
		}
		return true;
	}

	public string GetName()
	{
		return DataCollection.Get<WorldObjective>(Id).Name;
	}

	public string GetInfo()
	{
		return DataCollection.Get<WorldObjective>(Id).Info;
	}

	public List<LocalizedStringReference> GetNotices()
	{
		return DataCollection.Get<WorldObjective>(Id).Notices;
	}

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		foreach (ConditionData condition in Conditions)
		{
			condition.Initialise();
		}
		foreach (ObjectiveConditionCollection prefabConditionCollection in PrefabConditionCollections)
		{
			prefabConditionCollection.Initialise();
		}
		TriggerConditions?.Initialise();
		DataCollection.Register(this, mod);
	}

	public void GetEvaluatePrefabHashesAndTypes(ref List<int> prefabHashes, ref List<Type> types, ref List<int> triggerHashes, ref List<Type> triggerTypes)
	{
		WorldObjective worldObjective = (IsValid() ? this : DataCollection.Get<WorldObjective>(Id));
		if (worldObjective == null || !worldObjective.IsValid())
		{
			ConsoleWindow.PrintError(((worldObjective != null) ? worldObjective.Id : "NULL OBJECTIVE") + " is invalid");
			return;
		}
		foreach (ConditionData condition in worldObjective.Conditions)
		{
			if (!(condition is ThingPrefabCondition thingPrefabCondition))
			{
				if (!(condition is EntityStateCondition))
				{
					if (!(condition is ObjectiveCompleteCondition))
					{
						if (!(condition is RoomCondition))
						{
							if (!(condition is NetworkCondition networkCondition))
							{
								if (!(condition is TraderContactCondition))
								{
									if (!(condition is CursorThingCondition cursorThingCondition))
									{
										if (!(condition is ThingCountCondition thingCountCondition))
										{
											throw new ArgumentOutOfRangeException("condition");
										}
										prefabHashes.Add(thingCountCondition.PrefabNameHash);
									}
									else
									{
										prefabHashes.Add(cursorThingCondition.PrefabIdHash);
									}
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
			else
			{
				prefabHashes.Add(thingPrefabCondition.PrefabNameHash);
			}
		}
		foreach (ObjectiveConditionCollection prefabConditionCollection in worldObjective.PrefabConditionCollections)
		{
			prefabConditionCollection.GetEvaluatePrefabHashes(ref prefabHashes, ref types);
		}
		worldObjective.TriggerConditions?.GetEvaluatePrefabHashes(ref triggerHashes, ref triggerTypes);
	}

	public bool IsTriggered<T>(List<T> tList) where T : IEvaluable
	{
		WorldObjective worldObjective = (IsValid() ? this : DataCollection.Get<WorldObjective>(Id));
		if (worldObjective.TriggerConditions == null)
		{
			return true;
		}
		return worldObjective.TriggerConditions.Evaluate(tList);
	}

	public bool Evaluate<T>(List<T> tList) where T : IEvaluable
	{
		WorldObjective worldObjective = (IsValid() ? this : DataCollection.Get<WorldObjective>(Id));
		foreach (ConditionData condition in worldObjective.Conditions)
		{
			bool flag = false;
			if (condition is ThingCountCondition thingCountCondition)
			{
				flag = thingCountCondition.Evaluate(tList);
			}
			else
			{
				foreach (T t in tList)
				{
					if (condition.Evaluate(t))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		foreach (ObjectiveConditionCollection prefabConditionCollection in worldObjective.PrefabConditionCollections)
		{
			if (!prefabConditionCollection.Evaluate(tList))
			{
				return false;
			}
		}
		foreach (ActionData objectiveAction in ObjectiveActions)
		{
			objectiveAction.Execute(default(T), null);
		}
		return true;
	}
}
