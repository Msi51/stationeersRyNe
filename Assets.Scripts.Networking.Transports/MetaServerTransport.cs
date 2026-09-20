using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking.Transports;

public class MetaServerTransport
{
	private SteamTransport _steamTransport;

	private CancellationTokenSource _sessionListFetchCancelToken;

	public bool IsInitialised { get; private set; }

	public string PublicIp { get; set; }

	public void InitClient()
	{
		_steamTransport = new SteamTransport();
		_steamTransport.InitClient();
		IsInitialised = true;
	}

	public void Shutdown()
	{
		IsInitialised = false;
	}

	public string GetAppInstallDir()
	{
		return StationSaveUtils.ExeDirectory.FullName;
	}

	public bool IsDlcInstalled(uint dlcId)
	{
		if (_steamTransport.IsInitialised)
		{
			return _steamTransport.IsDlcInstalled(dlcId);
		}
		return false;
	}

	public void OpenWebPageOverlay(string url)
	{
		if (_steamTransport.IsInitialised)
		{
			_steamTransport.OpenWebPageOverlay(url);
		}
		else
		{
			Application.OpenURL(url);
		}
	}

	public string GetFriendName(ulong id)
	{
		return _steamTransport.GetFriendName(id);
	}

	public UniTask<Texture2D> GetAvatarAsync(ulong clientId, AvatarSize size)
	{
		return _steamTransport.GetAvatarAsync(clientId, size);
	}

	public async UniTask<int> RegisterGameSession(GameSession gameSession)
	{
		int num = (await PostJson(EndPoint.New, gameSession))["SessionId"]?.Value<int>() ?? (-1);
		NetworkManager.CurrentGameSession.SessionId = num;
		ConsoleWindow.PrintAction($"registered with session #{num}");
		return num;
	}

	public async UniTaskVoid Ping(GameSession gameSession)
	{
		int sessionId = (await PostJson(EndPoint.Ping, gameSession))["SessionId"]?.Value<int>() ?? (-1);
		NetworkManager.CurrentGameSession.SessionId = sessionId;
	}

	public void UnRegisterGameSession(GameSession gameSession)
	{
		if (NetworkManager.IsServer)
		{
			PostJson(EndPoint.Close, gameSession).Forget();
		}
	}

	public async UniTask<List<GameSession>> GetGameSessionList()
	{
		_sessionListFetchCancelToken = new CancellationTokenSource();
		var (flag, jObject) = await PostJson(EndPoint.List, new ListRequest()).AttachExternalCancellation(_sessionListFetchCancelToken.Token).SuppressCancellationThrow();
		if (flag)
		{
			CancelServerListRequest();
			return null;
		}
		List<GameSession> list = new List<GameSession>();
		if (!(jObject.SelectToken("GameSessions") is JArray jArray))
		{
			return list;
		}
		foreach (JToken item in jArray)
		{
			GameSession gameSession = item.ToObject<GameSession>();
			if (gameSession != null)
			{
				list.Add(gameSession);
			}
		}
		return list;
	}

	public void CancelServerListRequest()
	{
		_sessionListFetchCancelToken?.Cancel();
		_sessionListFetchCancelToken = null;
	}

	private static async UniTask<JObject> PostJson(EndPoint endPoint, object body)
	{
		using UnityWebRequest request = new UnityWebRequest(NetworkManager.Config.GetUrl(endPoint), "POST");
		string s = JsonConvert.SerializeObject(body);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		try
		{
			UnityWebRequest unityWebRequest = await request.SendWebRequest();
			if (unityWebRequest.result != UnityWebRequest.Result.Success)
			{
				throw new WebException(unityWebRequest.error);
			}
			string text = unityWebRequest.downloadHandler.text;
			if (!string.IsNullOrEmpty(text))
			{
				return JObject.Parse(text);
			}
		}
		catch (WebException exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
		catch (UnityWebRequestException ex)
		{
			if (ex.Result == UnityWebRequest.Result.ConnectionError)
			{
				ConsoleWindow.AsyncPrintError($"error failed to connect to master server (ResponseCode: {ex.ResponseCode})", suppressStacktrace: true).Forget();
			}
			else
			{
				ConsoleWindow.PrintError(ex).Forget();
			}
		}
		catch (JsonException exception2)
		{
			ConsoleWindow.PrintError(exception2).Forget();
		}
		catch (Exception exception3)
		{
			ConsoleWindow.PrintError(exception3).Forget();
			throw;
		}
		return new JObject();
	}
}
