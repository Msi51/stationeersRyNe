using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI.HelperHints;
using Networks;
using Objects.RoboticArm;
using Trading;

public static class HelperHintsManager
{
	private static Dictionary<int, List<IEvaluable>> _thingsByPrefabHash = new Dictionary<int, List<IEvaluable>>(128);

	private static Dictionary<Type, List<IEvaluable>> _iEvaluablesByType = new Dictionary<Type, List<IEvaluable>>
	{
		{
			typeof(Human),
			new List<IEvaluable>(32)
		},
		{
			typeof(WorldObjectiveState),
			new List<IEvaluable>(128)
		},
		{
			typeof(Room),
			new List<IEvaluable>(256)
		},
		{
			typeof(TraderContact),
			new List<IEvaluable>(8)
		},
		{
			typeof(CableNetwork),
			new List<IEvaluable>(128)
		},
		{
			typeof(StructureNetwork),
			new List<IEvaluable>(256)
		},
		{
			typeof(LandingPadNetwork),
			new List<IEvaluable>(32)
		},
		{
			typeof(PipeNetwork),
			new List<IEvaluable>(128)
		},
		{
			typeof(ChuteNetwork),
			new List<IEvaluable>(128)
		},
		{
			typeof(RocketNetwork),
			new List<IEvaluable>(32)
		},
		{
			typeof(RoboticArmNetwork),
			new List<IEvaluable>(32)
		}
	};

	private static readonly List<IEvaluable> TriggeredObjects = new List<IEvaluable>(2048);

	private static readonly List<IEvaluable> CurrentEvaluationObjects = new List<IEvaluable>(2048);

	public static void Initialize()
	{
		if (GameManager.RunSimulation)
		{
			SetupWorldObjectives(WorldSetting.Current.Data);
		}
		HelperHintsTextController.SetWorldObjectives(WorldObjectiveState.CurrentWorldObjectives);
	}

	private static void SetupWorldObjectives(WorldSettingData data)
	{
		foreach (WorldObjectiveCollection worldObjectiveCollection in data.WorldObjectiveCollections)
		{
			List<WorldObjective> objectives = worldObjectiveCollection.GetObjectives();
			for (int i = 0; i < objectives.Count; i++)
			{
				WorldObjective worldObjective = objectives[i];
				if (!worldObjective.IsValid())
				{
					worldObjective = DataCollection.Get<WorldObjective>(worldObjective.Id);
				}
				LoadObjective(worldObjective);
			}
		}
	}

	private static void LoadObjective(WorldObjective objective)
	{
		foreach (WorldObjectiveState currentWorldObjective in WorldObjectiveState.CurrentWorldObjectives)
		{
			if (currentWorldObjective.WorldObjective.IdHash == objective.IdHash)
			{
				return;
			}
		}
		WorldObjectiveState.CurrentWorldObjectives.Add(new WorldObjectiveState(objective, 0L));
	}

	public static void RegisterPrefabHash(int prefabHash)
	{
		if (GameManager.RunSimulation && !_thingsByPrefabHash.ContainsKey(prefabHash))
		{
			_thingsByPrefabHash.Add(prefabHash, new List<IEvaluable>());
		}
	}

	public static void Register(IEvaluable iEvaluable)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (!(iEvaluable is Human item))
		{
			if (!(iEvaluable is Thing thing))
			{
				if (!(iEvaluable is Room item2))
				{
					if (!(iEvaluable is CableNetwork item3))
					{
						if (!(iEvaluable is StructureNetwork structureNetwork))
						{
							if (!(iEvaluable is TraderContact item4))
							{
								if (!(iEvaluable is WorldObjectiveState item5))
								{
									throw new NotImplementedException();
								}
								_iEvaluablesByType[typeof(WorldObjectiveState)].Add(item5);
							}
							else
							{
								_iEvaluablesByType[typeof(TraderContact)].Add(item4);
							}
							return;
						}
						switch (structureNetwork.NetworkType)
						{
						case StructureNetworkType.LandingPad:
							_iEvaluablesByType[typeof(LandingPadNetwork)].Add(structureNetwork);
							break;
						case StructureNetworkType.Pipe:
							_iEvaluablesByType[typeof(PipeNetwork)].Add(structureNetwork);
							break;
						case StructureNetworkType.Chute:
							_iEvaluablesByType[typeof(ChuteNetwork)].Add(structureNetwork);
							break;
						case StructureNetworkType.Rocket:
							_iEvaluablesByType[typeof(RocketNetwork)].Add(structureNetwork);
							break;
						case StructureNetworkType.RoboticArm:
							_iEvaluablesByType[typeof(RoboticArmNetwork)].Add(structureNetwork);
							break;
						default:
							throw new ArgumentOutOfRangeException();
						case StructureNetworkType.None:
							break;
						}
					}
					else
					{
						_iEvaluablesByType[typeof(CableNetwork)].Add(item3);
					}
				}
				else
				{
					_iEvaluablesByType[typeof(Room)].Add(item2);
				}
			}
			else if (_thingsByPrefabHash.ContainsKey(thing.PrefabHash))
			{
				_thingsByPrefabHash[thing.PrefabHash].Add(thing);
			}
		}
		else
		{
			_iEvaluablesByType[typeof(Human)].Add(item);
		}
	}

	public static void Deregister(IEvaluable iEvaluable)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (!(iEvaluable is Human item))
		{
			if (!(iEvaluable is Thing thing))
			{
				if (!(iEvaluable is Room item2))
				{
					if (iEvaluable is WorldObjectiveState item3 && _iEvaluablesByType.ContainsKey(typeof(WorldObjectiveState)))
					{
						_iEvaluablesByType[typeof(WorldObjectiveState)].Remove(item3);
					}
				}
				else if (_iEvaluablesByType.ContainsKey(typeof(Room)))
				{
					_iEvaluablesByType[typeof(Room)].Remove(item2);
				}
			}
			else if (_thingsByPrefabHash.ContainsKey(thing.PrefabHash))
			{
				_thingsByPrefabHash[thing.PrefabHash].Remove(thing);
			}
		}
		else if (_iEvaluablesByType.ContainsKey(typeof(Human)))
		{
			_iEvaluablesByType[typeof(Human)].Remove(item);
		}
	}

	public static void ClearAll()
	{
		_thingsByPrefabHash.Clear();
		foreach (KeyValuePair<Type, List<IEvaluable>> item in _iEvaluablesByType)
		{
			item.Value.Clear();
		}
		WorldObjectiveState.ClearAll();
		HelperHintsTextController.ClearAll();
	}

	public static void DismissCompleted()
	{
		foreach (WorldObjectiveState currentWorldObjective in WorldObjectiveState.CurrentWorldObjectives)
		{
			if (currentWorldObjective.Completed)
			{
				currentWorldObjective.SetDismissed(value: true);
			}
		}
	}

	public static void UnDismissAll()
	{
		foreach (WorldObjectiveState currentWorldObjective in WorldObjectiveState.CurrentWorldObjectives)
		{
			currentWorldObjective.SetDismissed(value: false);
		}
	}

	public static void EvaluateObjectives(float tickDeltaTime)
	{
		if (!GameManager.RunSimulation || GameManager.GameState != GameState.Running)
		{
			return;
		}
		foreach (WorldObjectiveState currentWorldObjective in WorldObjectiveState.CurrentWorldObjectives)
		{
			if (!currentWorldObjective.IsValid() || currentWorldObjective.Completed || currentWorldObjective.Dismissed)
			{
				continue;
			}
			CurrentEvaluationObjects.Clear();
			TriggeredObjects.Clear();
			foreach (int relatedHash in currentWorldObjective.RelatedHashes)
			{
				if (_thingsByPrefabHash.TryGetValue(relatedHash, out var value))
				{
					CurrentEvaluationObjects.AddRange(value);
				}
			}
			foreach (Type relatedType in currentWorldObjective.RelatedTypes)
			{
				if (_iEvaluablesByType.TryGetValue(relatedType, out var value2))
				{
					CurrentEvaluationObjects.AddRange(value2);
				}
			}
			foreach (int triggerHash in currentWorldObjective.TriggerHashes)
			{
				if (_thingsByPrefabHash.TryGetValue(triggerHash, out var value3))
				{
					TriggeredObjects.AddRange(value3);
				}
			}
			foreach (Type triggerType in currentWorldObjective.TriggerTypes)
			{
				if (_iEvaluablesByType.TryGetValue(triggerType, out var value4))
				{
					TriggeredObjects.AddRange(value4);
				}
			}
			if (!currentWorldObjective.Triggered)
			{
				currentWorldObjective.Triggered = currentWorldObjective.IsTriggered(TriggeredObjects, tickDeltaTime);
			}
			if (currentWorldObjective.Triggered && currentWorldObjective.CanComplete)
			{
				currentWorldObjective.Completed = currentWorldObjective.Evaluate(CurrentEvaluationObjects, tickDeltaTime);
			}
		}
	}
}
