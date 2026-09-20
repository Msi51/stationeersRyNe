using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Networking;

public static class NetworkManagerExtensions
{
	public static List<GameSession> SortGameSessionList(this IEnumerable<GameSession> gameSessions, SessionSortType SortType)
	{
		Func<GameSession, object> keySelector = SortType switch
		{
			SessionSortType.Name => (GameSession x) => x.Name, 
			SessionSortType.Version => (GameSession x) => x.Version, 
			SessionSortType.PlayerCount => (GameSession x) => x.Players, 
			_ => (GameSession x) => x.Players, 
		};
		return gameSessions.OrderByDescending((GameSession x) => !NetworkManager.GameSessionFavouriteList.Contains(x)).ThenBy(keySelector).ToList();
	}
}
