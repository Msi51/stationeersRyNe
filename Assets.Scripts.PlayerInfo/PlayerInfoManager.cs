using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.PlayerInfo;

public class PlayerInfoManager : Singleton<PlayerInfoManager>
{
	public static readonly Dictionary<ulong, PlayerDetail> PlayerDictionary = new Dictionary<ulong, PlayerDetail>();

	public float SteamPlaytimeUpdateInterval = 10f;

	private float DeltaSteamPlaytimeUpdateInterval;

	public static string ServerCreateKey = "creator";

	public static string ServerPlayerKey = "player_";

	public static string ServerDataKey = "data_";

	private float PingInterval = 2f;

	private float DeltaInterval;

	private float UpdatePlayTimeInterval = 1f;

	private float DeltaPlayTimeInterval;

	private float LocalStartPlayTime = -1f;

	private float StartPlayTime = -1f;

	private ulong SteamId;

	private void ValidatePlayerDictionary()
	{
		if (!GameManager.IsBatchMode)
		{
			SteamId = NetworkManager.LocalClientId;
		}
	}

	public int GetPlayTime(float destStartPlayTime)
	{
		if (GameManager.IsBatchMode)
		{
			return (int)(Time.realtimeSinceStartup - destStartPlayTime);
		}
		float num = Time.realtimeSinceStartup - LocalStartPlayTime - (destStartPlayTime - StartPlayTime);
		if ((int)num >= 0)
		{
			return (int)num;
		}
		return 0;
	}

	private static void UpdateServerKeyValues()
	{
		if (!GameManager.IsBatchMode)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<ulong, PlayerDetail> item in PlayerDictionary)
		{
			_ = $"{ServerPlayerKey}{num++}";
			_ = $"{item.Value.SteamId}:{item.Value.PlayerName}";
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.IsBatchMode && GameManager.GameState != GameState.None)
		{
			DeltaSteamPlaytimeUpdateInterval += Time.deltaTime;
			if (DeltaSteamPlaytimeUpdateInterval >= SteamPlaytimeUpdateInterval)
			{
				DeltaSteamPlaytimeUpdateInterval = 0f;
				UpdateServerKeyValues();
			}
		}
	}
}
