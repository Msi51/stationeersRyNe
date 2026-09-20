using System.Collections.Generic;

namespace Assets.Scripts.Networking;

public static class StatKey
{
	public const string GAMES_PLAYED = "Games_Played";

	public const string TOTAL_SCORE = "Total_Score";

	public const string PLAYTIME_BASE_MAP = "PlayTime_Base_Map";

	private static readonly Dictionary<string, string> _statMap = new Dictionary<string, string>
	{
		["Games_Played"] = "Games Played",
		["Total_Score"] = "Total Score",
		["PlayTime_Base_Map"] = "Total Play Time"
	};

	public static IEnumerable<string> Keys => _statMap.Keys;

	public static string GetDescription(string key)
	{
		if (!_statMap.TryGetValue(key, out var value))
		{
			return string.Empty;
		}
		return value;
	}
}
