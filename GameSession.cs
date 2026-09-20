using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;

public class GameSession
{
	public const int INVALID = -1;

	public int SessionId = -1;

	public string Name;

	public bool Password;

	public string Version;

	public string Address;

	public string Checksum;

	public string Port;

	public int Players;

	public int MaxPlayers;

	public int UpTime;

	public string MapName;

	public int Latency;

	public ulong SteamId;

	public ServerType Type;

	[JsonIgnore]
	public int StartTime;

	[JsonIgnore]
	public SteamId HostSteamId => new SteamId
	{
		Value = SteamId
	};

	public override string ToString()
	{
		return JObject.FromObject(this).ToString();
	}
}
