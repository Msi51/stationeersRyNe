using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Messages;
using Objects.Rockets.Log.RocketEvents;

namespace Objects.Rockets.Log;

public class RocketLog
{
	private const int MAX_COUNT = 50;

	private List<RocketEvent> _log = new List<RocketEvent>(50);

	private HashSet<int> _logHashes = new HashSet<int>(50);

	private static RocketLog _instance;

	private static RocketLog Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new RocketLog();
			}
			return _instance;
		}
	}

	public static string GetLogText()
	{
		return Instance.GetLog();
	}

	public static async UniTaskVoid Append(RocketEvent rocketEvent)
	{
		if (GameManager.GameState != GameState.Loading)
		{
			await UniTask.SwitchToMainThread();
			Instance.AppendLog(rocketEvent);
		}
	}

	public static void ClearAll()
	{
		Instance._log.Clear();
		Instance._logHashes.Clear();
		RocketEvent.NewToSend.Clear();
	}

	public static void ClearLogLocally()
	{
		Instance._log.Clear();
		Instance._logHashes.Clear();
	}

	private static void PublishUpdateEvent()
	{
		GameManager.EventBus.Publish(new OnRocketLogUpdatedMessage());
	}

	private string GetLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _log.Count; i++)
		{
			RocketEvent rocketEvent = _log[i];
			stringBuilder.AppendLine(rocketEvent.ToString());
		}
		return stringBuilder.ToString();
	}

	private void AppendLog(RocketEvent rocketEvent, bool suppressEvents = false)
	{
		if (!_logHashes.Contains(rocketEvent.Hash))
		{
			_log.Add(rocketEvent);
			_logHashes.Add(rocketEvent.Hash);
			if (_log.Count > 50)
			{
				_log.RemoveAt(0);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				RocketEvent.NewToSend.Add(rocketEvent);
			}
			if (!suppressEvents)
			{
				PublishUpdateEvent();
			}
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (RocketEvent item in Instance._log)
		{
			if (item != null)
			{
				item.Serialize(writer);
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingRocketLog.DisplayString);
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			RocketEvent.Create(reader);
		}
	}
}
