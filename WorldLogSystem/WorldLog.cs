using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Messages;

namespace WorldLogSystem;

public class WorldLog
{
	private List<WorldEvent> _log = new List<WorldEvent>();

	private List<WorldEvent> _worldEventsToSync = new List<WorldEvent>();

	private static WorldLog _instance;

	private const int MAX_COUNT = 50;

	private static WorldLog Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new WorldLog();
			}
			return _instance;
		}
	}

	public static void SerializeNew(RocketBinaryWriter writer)
	{
		int count = Instance._worldEventsToSync.Count;
		writer.WriteByte((byte)count);
		foreach (WorldEvent item in Instance._worldEventsToSync)
		{
			writer.WriteByte((byte)item.WorldEventType);
			item.Serialize(writer);
		}
		Instance._worldEventsToSync.Clear();
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		byte b = reader.ReadByte();
		for (int i = 0; i < b; i++)
		{
			WorldEventType eventType = (WorldEventType)reader.ReadByte();
			Instance.AppendLog(WorldLogHelper.DeserializeWorldEvent(eventType, reader));
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteUInt16((ushort)Instance._log.Count);
		foreach (WorldEvent item in Instance._log)
		{
			writer.WriteByte((byte)item.WorldEventType);
			item.Serialize(writer);
		}
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializeWorldLog.DisplayString);
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			WorldEventType eventType = (WorldEventType)reader.ReadByte();
			Instance.AppendLog(WorldLogHelper.DeserializeWorldEvent(eventType, reader), suppressEvents: true);
		}
		PublishUpdateEvent();
	}

	public static string GetLogText()
	{
		return Instance.GetLog();
	}

	public static void Load(List<WorldEventData> data)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		_instance = new WorldLog();
		foreach (WorldEventData datum in data)
		{
			_instance.AppendLog(datum.ToEvent(), suppressEvents: true);
		}
		PublishUpdateEvent();
	}

	public static List<WorldEventData> GetSaveData()
	{
		List<WorldEventData> list = new List<WorldEventData>();
		foreach (WorldEvent item in Instance._log)
		{
			list.Add(item.ToData());
		}
		return list;
	}

	public static void Append(WorldEvent worldEvent)
	{
		if (GameManager.RunSimulation)
		{
			Instance.AppendLog(worldEvent);
		}
	}

	private static void PublishUpdateEvent()
	{
		GameManager.EventBus.Publish(new OnWorldLogUpdatedMessage());
	}

	private string GetLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int num = _log.Count - 1; num >= 0; num--)
		{
			WorldEvent worldEvent = _log[num];
			stringBuilder.AppendLine("<color=grey>" + worldEvent.DateTime + "</color> <color=" + worldEvent.TextColor + ">" + worldEvent.Text + "</color>");
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}

	private void AppendLog(WorldEvent worldEvent, bool suppressEvents = false)
	{
		_log.Add(worldEvent);
		if (_log.Count > 50)
		{
			_log.RemoveAt(0);
		}
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			_worldEventsToSync.Add(worldEvent);
		}
		if (!suppressEvents)
		{
			PublishUpdateEvent();
		}
	}
}
