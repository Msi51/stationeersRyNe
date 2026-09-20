using System;
using Assets.Scripts.Util;

namespace Assets.Scripts.PlayerInfo;

public class PlayerDetail
{
	public ulong SteamId;

	public string PlayerName;

	public float StartPlayTime;

	public float LastStatUpdateTime;

	public int Score;

	public int PingMs;

	public int GamesPlayed;

	public int TotalScore;

	public int PlayTimeBaseMap;

	public int GetTotalPlayTime()
	{
		return Singleton<PlayerInfoManager>.Instance.GetPlayTime(StartPlayTime);
	}

	public int GetStat(string key)
	{
		return key switch
		{
			"Total_Score" => TotalScore, 
			"Games_Played" => GamesPlayed, 
			"PlayTime_Base_Map" => PlayTimeBaseMap, 
			_ => throw new ArgumentException(), 
		};
	}
}
