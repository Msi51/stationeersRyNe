using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Trading;

namespace Assets.Scripts;

public class SpawnDataHelper
{
	private static List<PendingSpawnActionSaveData> _pendingActions = new List<PendingSpawnActionSaveData>();

	public static void AddPendingAction(DelayedAction action, Thing thing, Entity player)
	{
		PendingSpawnActionSaveData item = new PendingSpawnActionSaveData(thing, player, action, (float)action.Delay.Seconds);
		_pendingActions.Add(item);
	}

	public static void ProcessPendingActions(float deltaTime)
	{
		if (_pendingActions.Count <= 0)
		{
			return;
		}
		for (int num = _pendingActions.Count - 1; num >= 0; num--)
		{
			PendingSpawnActionSaveData pendingSpawnActionSaveData = _pendingActions[num];
			if (pendingSpawnActionSaveData.TimeRemaining <= 0f)
			{
				pendingSpawnActionSaveData.Action.Execute(pendingSpawnActionSaveData.Thing, pendingSpawnActionSaveData.Player);
				_pendingActions.Remove(pendingSpawnActionSaveData);
			}
			else
			{
				pendingSpawnActionSaveData.TimeRemaining -= deltaTime;
			}
		}
	}

	public static List<PendingSpawnActionSaveData> SerializeSave()
	{
		return _pendingActions;
	}

	public static void LoadSave(XmlSaveLoad.WorldData worldData)
	{
		_pendingActions.Clear();
		foreach (PendingSpawnActionSaveData pendingSpawnAction in worldData.PendingSpawnActions)
		{
			if (pendingSpawnAction.Validate())
			{
				_pendingActions.Add(pendingSpawnAction);
			}
		}
	}

	public static void ClearAll()
	{
		_pendingActions.Clear();
	}
}
