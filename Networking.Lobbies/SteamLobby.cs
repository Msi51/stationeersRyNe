using System.Threading.Tasks;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Steamworks;
using Steamworks.Data;

namespace Networking.Lobbies;

public class SteamLobby
{
	private const string HostAddressKey = "HostAddress";

	private Lobby activeLobby;

	public SteamLobby()
	{
		SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
		SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
		SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
		SteamFriends.OnGameRichPresenceJoinRequested += RichPresenceJoinRequested;
		SteamApps.OnNewLaunchParameters += OnNewLaunchParameters;
	}

	private void RichPresenceJoinRequested(Friend friend, string connectString)
	{
		ConsoleWindow.Print("Requesting rich presence join with friend " + friend.Name + " | Connect String: " + connectString);
		if (ulong.TryParse(connectString, out var result))
		{
			SteamId steamId = result;
			ConsoleWindow.Print($"RichPresenceJoinRequested: {steamId}");
		}
	}

	public async Task<bool> HostLobby(SteamId hostId, int maxConnections)
	{
		Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync();
		if (!lobby.HasValue)
		{
			ConsoleWindow.PrintError("Lobby created but not correctly instantiated");
			return false;
		}
		activeLobby = lobby.Value;
		activeLobby.SetPublic();
		activeLobby.SetData("HostAddress", hostId.ToString());
		MakeLobbyJoinable(activeLobby);
		activeLobby.SetGameServer(hostId);
		ConsoleWindow.Print("Joinable Steam Lobby created");
		return await activeLobby.Join() == RoomEnter.Success;
	}

	public static void MakeLobbyJoinable(Lobby lobby)
	{
		lobby.SetData("joinable", "1");
	}

	public static async void ClientJoinLobbyAndGame(Lobby lobby, SteamId steamId)
	{
		if (NetworkManager.NetworkRole != NetworkRole.None)
		{
			ConsoleWindow.PrintError("Client already connected to a lobby or game - cannot join another");
			return;
		}
		RoomEnter roomEnter = await lobby.Join();
		if (roomEnter == RoomEnter.Success)
		{
			SteamFriends.SetRichPresence("connect", steamId.ToString());
			MakeLobbyJoinable(lobby);
			ConsoleWindow.Print("Lobby joined succesfully - Starting client");
			NetworkManager.StartClient(steamId);
		}
		else
		{
			ConsoleWindow.PrintError("Client failed to join Lobby: " + roomEnter);
		}
	}

	public void LeaveLobby()
	{
		ConsoleWindow.Print("Leaving steam lobby");
		activeLobby.Leave();
	}

	public static void OnLobbyMemberJoined(Lobby lobby, Friend friend)
	{
		if (NetworkManager.NetworkRole == NetworkRole.Server)
		{
			ConsoleWindow.Print("Host: Friend joined with ID: " + friend.Id.ToString());
		}
		else
		{
			ConsoleWindow.Print("Lobby entered with host " + friend.Id.ToString());
		}
	}

	public static void OnLobbyEntered(Lobby lobby)
	{
		ConsoleWindow.Print($"Lobby entered with host {lobby.Id}");
	}

	public static void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
	{
		if (NetworkManager.NetworkRole == NetworkRole.Server)
		{
			ConsoleWindow.Print("Host: Friend requests lobby join " + steamId.ToString());
			return;
		}
		ConsoleWindow.Print("Client: Requesting lobby join " + steamId.ToString());
		ClientJoinLobbyAndGame(lobby, steamId);
	}

	public static void OnLobbyInvite(Friend friend, Lobby lobby)
	{
		if (NetworkManager.NetworkRole == NetworkRole.Server)
		{
			ConsoleWindow.Print("Host: Lobby invite");
		}
		else
		{
			ConsoleWindow.Print("Client: Lobby invite received");
		}
	}

	public static void OnNewLaunchParameters()
	{
		ConsoleWindow.Print("Steam launch parameters provided: " + SteamApps.CommandLine);
	}
}
