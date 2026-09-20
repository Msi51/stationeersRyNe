using Assets.Scripts;
using Assets.Scripts.Serialization;
using Newtonsoft.Json.Linq;

namespace Networking.GameSessions;

public struct GameSessionConfig
{
	public string gameName;

	public string mapName;

	public ushort port;

	public bool password;

	public int maxPlayers;

	public string ipAddress;

	public ulong SteamId;

	public static GameSessionConfig Default => new GameSessionConfig
	{
		gameName = "Stationeers",
		password = false,
		maxPlayers = 10,
		port = 27016,
		mapName = "Moon",
		SteamId = (Settings.CurrentData.UseSteamP2P ? GameManager.GetSteamId() : 0)
	};

	public override string ToString()
	{
		return JObject.FromObject(this).ToString();
	}
}
