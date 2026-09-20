using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Trading;

public class WorldObjectiveState : IReferencable, IEvaluable, ISyncListable
{
	public static List<WorldObjectiveState> CurrentWorldObjectives = new List<WorldObjectiveState>(128);

	public static SyncList<WorldObjectiveState> UpdatedObjectives = new SyncList<WorldObjectiveState>(DeSerialize);

	public WorldObjective WorldObjective;

	public readonly List<int> RelatedHashes;

	public readonly List<Type> RelatedTypes;

	public readonly List<int> TriggerHashes;

	public readonly List<Type> TriggerTypes;

	private bool _triggered;

	private bool _completed;

	private bool _dismissed;

	private readonly float _triggerDelaySeconds;

	public const float DELAY_TIME_OBJECTIVE_COMPLETE = 1.5f;

	public const float DELAY_TIME_FIRST_OBJECTIVE = 2.5f;

	public bool Triggered
	{
		get
		{
			return _triggered;
		}
		set
		{
			if (_triggered != value && NetworkManager.IsServer && NetworkServer.HasClients())
			{
				UpdatedObjectives.Add(this);
			}
			_triggered = value;
		}
	}

	public bool Completed
	{
		get
		{
			return _completed;
		}
		set
		{
			if (_completed != value && NetworkManager.IsServer && NetworkServer.HasClients())
			{
				UpdatedObjectives.Add(this);
			}
			_completed = value;
		}
	}

	public bool Dismissed
	{
		get
		{
			return _dismissed;
		}
		set
		{
			if (_dismissed != value && NetworkManager.IsServer && NetworkServer.HasClients())
			{
				UpdatedObjectives.Add(this);
			}
			_dismissed = value;
		}
	}

	public float EvaluateTime { get; private set; }

	public float TriggerTime { get; private set; }

	public string DisplayName
	{
		get
		{
			LocalizedStringReference localizedStringReference = WorldObjective?.Name;
			if (localizedStringReference == null)
			{
				return "INVALID";
			}
			return localizedStringReference;
		}
	}

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public bool CanComplete
	{
		get
		{
			if (IsValid())
			{
				if (WorldObjective.Conditions.Count <= 0)
				{
					return WorldObjective.PrefabConditionCollections.Count > 0;
				}
				return true;
			}
			return false;
		}
	}

	public bool ShouldCompleteOnDismissed => !CanComplete;

	public bool Evaluate<T>(List<T> tList, float tickTime) where T : IEvaluable
	{
		bool flag = WorldObjective.Evaluate(tList);
		if (flag)
		{
			EvaluateTime += tickTime;
		}
		return EvaluateTime >= WorldObjective.Time && flag;
	}

	public bool IsTriggered<T>(List<T> tList, float tickTime) where T : IEvaluable
	{
		bool flag = WorldObjective.IsTriggered(tList);
		if (flag)
		{
			TriggerTime += tickTime;
		}
		return TriggerTime >= _triggerDelaySeconds && flag;
	}

	public bool IsValid()
	{
		return WorldObjective != null;
	}

	public WorldObjectiveState(WorldObjective objective, long referenceId = 0L)
	{
		WorldObjective = objective;
		RelatedHashes = new List<int>();
		RelatedTypes = new List<Type>();
		TriggerHashes = new List<int>();
		TriggerTypes = new List<Type>();
		WorldObjective.GetEvaluatePrefabHashesAndTypes(ref RelatedHashes, ref RelatedTypes, ref TriggerHashes, ref TriggerTypes);
		if (WorldObjective.TriggerConditions != null)
		{
			foreach (ConditionData condition in WorldObjective.TriggerConditions.Conditions)
			{
				if (condition is ObjectiveCompleteCondition)
				{
					_triggerDelaySeconds = 1.5f;
					break;
				}
			}
		}
		else
		{
			_triggerDelaySeconds = 2.5f;
		}
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	public void SetDismissed(bool value)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.DismissHelperHint(this, value);
		}
		Dismissed = value;
	}

	public static WorldObjectiveState Create(WorldObjectiveSaveData saveData)
	{
		WorldObjective worldObjective = DataCollection.Get<WorldObjective>(saveData.IdHash);
		if (worldObjective == null)
		{
			return null;
		}
		return new WorldObjectiveState(worldObjective, saveData.ReferenceId)
		{
			Triggered = saveData.Triggered,
			Completed = saveData.Completed,
			Dismissed = saveData.Dismissed
		};
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, this);
		writer.WriteInt32(WorldObjective.IdHash);
		writer.WriteBoolean(Triggered);
		writer.WriteBoolean(Completed);
		writer.WriteBoolean(Dismissed);
	}

	public static WorldObjectiveState Create(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		int idHash = reader.ReadInt32();
		bool triggered = reader.ReadBoolean();
		bool completed = reader.ReadBoolean();
		bool dismissed = reader.ReadBoolean();
		return new WorldObjectiveState(DataCollection.Get<WorldObjective>(idHash), referenceId)
		{
			Triggered = triggered,
			Completed = completed,
			Dismissed = dismissed
		};
	}

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public void OnAssignedReference()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		HelperHintsManager.Register(this);
		if (RelatedHashes != null)
		{
			foreach (int relatedHash in RelatedHashes)
			{
				HelperHintsManager.RegisterPrefabHash(relatedHash);
			}
		}
		if (TriggerHashes == null)
		{
			return;
		}
		foreach (int triggerHash in TriggerHashes)
		{
			HelperHintsManager.RegisterPrefabHash(triggerHash);
		}
	}

	public static List<WorldObjectiveSaveData> Save()
	{
		List<WorldObjectiveSaveData> list = new List<WorldObjectiveSaveData>();
		foreach (WorldObjectiveState currentWorldObjective in CurrentWorldObjectives)
		{
			list.Add(new WorldObjectiveSaveData(currentWorldObjective));
		}
		return list;
	}

	public static void Load(XmlSaveLoad.WorldData saveData)
	{
		foreach (WorldObjectiveSaveData objective in saveData.Objectives)
		{
			CurrentWorldObjectives.Add(Create(objective));
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, this);
		writer.WriteBoolean(Triggered);
		writer.WriteBoolean(Completed);
		writer.WriteBoolean(Dismissed);
	}

	public static void DeSerialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		bool triggered = reader.ReadBoolean();
		bool completed = reader.ReadBoolean();
		bool dismissed = reader.ReadBoolean();
		WorldObjectiveState worldObjectiveState = Referencable.Find<WorldObjectiveState>(referenceId);
		if (worldObjectiveState != null)
		{
			worldObjectiveState.Triggered = triggered;
			worldObjectiveState.Completed = completed;
			worldObjectiveState.Dismissed = dismissed;
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (WorldObjectiveState currentWorldObjective in CurrentWorldObjectives)
		{
			currentWorldObjective.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingWorldObjectives.DisplayString);
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			CurrentWorldObjectives.Add(Create(reader));
		}
		HelperHintsManager.Initialize();
	}

	private void Clear()
	{
		RelatedTypes.Clear();
		RelatedHashes.Clear();
	}

	public static void ClearAll()
	{
		foreach (WorldObjectiveState currentWorldObjective in CurrentWorldObjectives)
		{
			currentWorldObjective.Clear();
		}
		CurrentWorldObjectives.Clear();
		UpdatedObjectives.Clear();
	}
}
