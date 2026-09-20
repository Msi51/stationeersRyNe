using System;
using Assets.Scripts;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;
using Steamworks;
using UI;
using UnityEngine;
using Util.Commands;

namespace Networking;

public class DiscordClient : Singleton<DiscordClient>
{
	public static Action<string> OnClientJoinedLobby;

	private const string SERVER_IP = "ServerIp";

	[SerializeField]
	private long _appId;

	[SerializeField]
	private string _capsuleImageName;

	[SerializeField]
	private string _productName;

	private global::Discord.Discord _discord;

	private ActivityManager _activityManager;

	private LobbyManager _lobbyManager;

	private OverlayManager _overlayManager;

	private Activity _currentActivity;

	private string _joinActivitySecret;

	private int _failureAttempts;

	public bool IsInitialised => _discord != null;

	public long LobbyId => long.Parse(_currentActivity.Party.Id ?? "0");

	public string CurrentActivityJson => JObject.FromObject(_currentActivity).ToString();

	public override void ManagerStart()
	{
		base.ManagerStart();
		RuntimePlatform platform = Application.platform;
		if (platform == RuntimePlatform.LinuxServer || platform == RuntimePlatform.WindowsServer || CommandLine.TryGetArg("-nodiscord", out var value) || CommandLine.TryGetArg("--nodiscord", out value))
		{
			return;
		}
		try
		{
			_discord = new global::Discord.Discord(_appId, 1uL);
		}
		catch (ResultException ex)
		{
			ConsoleWindow.Print("Discord init failed: " + ex.Message, ConsoleColor.Red);
			return;
		}
		_discord.SetLogHook(LogLevel.Debug, delegate(LogLevel level, string message)
		{
			if (level == LogLevel.Error)
			{
				ConsoleWindow.PrintError($"Log[{level}] {message}");
			}
			else
			{
				ConsoleWindow.Print($"Log[{level}] {message}");
			}
		});
		_activityManager = _discord.GetActivityManager();
		_lobbyManager = _discord.GetLobbyManager();
		_overlayManager = _discord.GetOverlayManager();
		_activityManager.OnActivityJoinRequest += OnActivityJoinRequest;
		_activityManager.OnActivitySpectate += OnActivitySpectate;
		_activityManager.OnActivityJoin += OnActivityJoin;
		_lobbyManager.OnMemberConnect += LobbyOnMemberConnect;
		_lobbyManager.OnMemberDisconnect += LobbyOnMemberDisconnect;
		WaitForSteam().Forget();
		_currentActivity = new Activity
		{
			Name = _productName,
			Type = ActivityType.Playing,
			ApplicationId = _appId,
			Timestamps = 
			{
				Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			},
			Assets = 
			{
				LargeImage = _capsuleImageName,
				LargeText = _productName
			}
		};
		ConsoleWindow.PrintAction("Discord initialised");
		UpdateActivity("Playing a game", "In Start Menu");
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		try
		{
			_discord?.RunCallbacks();
		}
		catch (ResultException exception)
		{
			Debug.LogException(exception, this);
			_failureAttempts++;
			if (_failureAttempts > 3)
			{
				Dispose();
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_discord?.Dispose();
	}

	private void Dispose()
	{
		_discord?.Dispose();
		_discord = null;
		_activityManager = null;
		_lobbyManager = null;
		_overlayManager = null;
	}

	private async UniTaskVoid WaitForSteam()
	{
		if (!(await UniTask.WaitUntil(() => SteamClient.IsValid).TimeoutWithoutException(TimeSpan.FromSeconds(30.0))) && _activityManager != null)
		{
			_activityManager.RegisterSteam((uint)SteamClient.SteamId.Value);
		}
	}

	private void RefreshActivity()
	{
		_activityManager?.UpdateActivity(_currentActivity, delegate(Discord.Result result)
		{
			if (result == Discord.Result.Ok)
			{
				ConsoleWindow.Print("Discord activity updated");
			}
			else
			{
				ConsoleWindow.Print("Discord Activity failed to update");
			}
		});
	}

	public void UpdateActivityInGame()
	{
		UpdateActivity("Playing a Game", $"Day {WorldManager.DaysPast} on {WorldManager.CurrentWorldName}");
	}

	public void UpdateActivityState(string state)
	{
		_currentActivity.State = state;
		RefreshActivity();
	}

	private void UpdateActivity(string details, string state)
	{
		_currentActivity.Details = details;
		_currentActivity.State = state;
		RefreshActivity();
	}

	public void CreateLobby(uint maxPlayers, string serverIp, LobbyType lobbyType = LobbyType.Public)
	{
		if (!IsInitialised || _lobbyManager == null)
		{
			return;
		}
		try
		{
			LobbyTransaction lobbyCreateTransaction = _lobbyManager.GetLobbyCreateTransaction();
			lobbyCreateTransaction.SetCapacity(maxPlayers);
			lobbyCreateTransaction.SetType(lobbyType);
			lobbyCreateTransaction.SetMetadata("ServerIp", serverIp);
			_lobbyManager.CreateLobby(lobbyCreateTransaction, delegate(Discord.Result result, ref Lobby lobby)
			{
				if (result != Discord.Result.Ok)
				{
					throw new ResultException(result);
				}
				long lobbyId = lobby.Id;
				uint lobbyCapacity = lobby.Capacity;
				_joinActivitySecret = $"{lobby.Id}:{lobby.Secret}";
				LobbyTransaction lobbyUpdateTransaction = _lobbyManager.GetLobbyUpdateTransaction(lobbyId);
				_lobbyManager.UpdateLobby(lobbyId, lobbyUpdateTransaction, delegate
				{
					ConsoleWindow.Print($"lobby {lobbyId} updated");
					_currentActivity.Party = new ActivityParty
					{
						Id = lobbyId.ToString(),
						Size = 
						{
							CurrentSize = _lobbyManager.MemberCount(lobbyId),
							MaxSize = (int)lobbyCapacity
						}
					};
					_currentActivity.Secrets = new ActivitySecrets
					{
						Join = _joinActivitySecret
					};
					UpdateActivityInGame();
				});
			});
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("Failed To create Discord Lobby: " + ex.Message);
		}
	}

	private void OnActivityJoinRequest(ref User user)
	{
		ConsoleWindow.Print($"OnJoinRequest {user.Username} {user.Id}");
		long userId = user.Id;
		string username = user.Username;
		Singleton<ConfirmationPanel>.Instance.ShowRaw("User Requesting To Join", "Discord user <b>" + username + "</b> is requesting to join your game.", "Accept", delegate
		{
			_activityManager.SendRequestReply(userId, ActivityJoinRequestReply.Yes, delegate(Discord.Result res)
			{
				if (res == Discord.Result.Ok)
				{
					ConsoleWindow.Print("Responded successfully");
				}
			});
		}, "Decline", delegate
		{
			_activityManager.SendRequestReply(userId, ActivityJoinRequestReply.No, delegate(Discord.Result res)
			{
				if (res == Discord.Result.Ok)
				{
					ConsoleWindow.Print("Responded successfully");
				}
			});
		});
	}

	private void OnActivityJoin(string secret)
	{
		if (secret == _joinActivitySecret)
		{
			return;
		}
		_joinActivitySecret = secret;
		ConsoleWindow.Print("OnJoin " + secret);
		_lobbyManager.ConnectLobbyWithActivitySecret(secret, delegate(Discord.Result result, ref Lobby lobby)
		{
			if (result != Discord.Result.Ok)
			{
				ConsoleWindow.PrintError($"Failed to join lobby: {result}");
			}
			else
			{
				long id = lobby.Id;
				ConsoleWindow.Print($"Connected to lobby: {id}");
				_currentActivity.Party = new ActivityParty
				{
					Id = id.ToString(),
					Size = 
					{
						CurrentSize = _lobbyManager.MemberCount(lobby.Id),
						MaxSize = (int)lobby.Capacity
					}
				};
				_currentActivity.Secrets = new ActivitySecrets
				{
					Join = secret
				};
				UpdateActivityState("Joining a session");
				string lobbyMetadataValue = _lobbyManager.GetLobbyMetadataValue(id, "ServerIp");
				OnClientJoinedLobby?.Invoke(lobbyMetadataValue);
			}
		});
	}

	private void OnActivitySpectate(string secret)
	{
	}

	public void DisconnectLobby()
	{
		_lobbyManager?.DisconnectLobby(LobbyId, delegate(Discord.Result res)
		{
			if (res != Discord.Result.Ok)
			{
				ConsoleWindow.PrintError($"Failed to disconnect lobby: {res}");
			}
			else
			{
				ConsoleWindow.Print("Lobby disconnected");
			}
		});
	}

	private void LobbyOnMemberConnect(long lobbyId, long userId)
	{
		ConsoleWindow.Print($"Member connected to lobby. lobbyId: {lobbyId}, userId: {userId}");
		LobbyRefresh(lobbyId);
	}

	private void LobbyOnMemberDisconnect(long lobbyId, long userId)
	{
		ConsoleWindow.Print($"Member disconnected from lobby. lobbyId: {lobbyId}, userId: {userId}");
		LobbyRefresh(lobbyId);
	}

	private void LobbyRefresh(long lobbyId)
	{
		LobbyTransaction lobbyUpdateTransaction = _lobbyManager.GetLobbyUpdateTransaction(lobbyId);
		_lobbyManager.UpdateLobby(lobbyId, lobbyUpdateTransaction, delegate(Discord.Result res)
		{
			if (res != Discord.Result.Ok)
			{
				ConsoleWindow.PrintError("Error updating lobby");
			}
			else
			{
				_currentActivity.Party = new ActivityParty
				{
					Id = lobbyId.ToString(),
					Size = 
					{
						CurrentSize = _lobbyManager.MemberCount(lobbyId),
						MaxSize = (int)_lobbyManager.GetLobby(lobbyId).Capacity
					}
				};
				RefreshActivity();
			}
		});
	}
}
