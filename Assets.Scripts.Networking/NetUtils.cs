using System;
using System.Net;
using System.Net.Http;
using Assets.Scripts.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Networking;

public static class NetUtils
{
	public static string IPToString(uint unIP)
	{
		return $"{(ulong)(unIP >> 24) & 0xFFuL}.{(ulong)(unIP >> 16) & 0xFFuL}.{(ulong)(unIP >> 8) & 0xFFuL}.{(ulong)unIP & 0xFFuL}";
	}

	public static string IPAndPortToString(uint unIP, ushort usPort)
	{
		return $"{IPToString(unIP)}:{usPort}";
	}

	public static uint IPToInt(string addr)
	{
		byte[] addressBytes = IPAddress.Parse(addr).GetAddressBytes();
		Array.Reverse(addressBytes);
		return BitConverter.ToUInt32(addressBytes, 0);
	}

	public static string FormatSeconds(int Elapsed)
	{
		int num = Elapsed % 60;
		int num2 = Elapsed / 60 % 60;
		int num3 = Elapsed / 60 / 60;
		return $"{num3:00}:{num2:00}:{num:00}";
	}

	public static async UniTask<string> GetPublicIp()
	{
		int retries = 0;
		WebException ex = null;
		while (retries++ < 3)
		{
			try
			{
				using HttpClient httpClient = new HttpClient();
				return await httpClient.GetStringAsync("https://api.ipify.org");
			}
			catch (WebException ex2)
			{
				Debug.LogWarning(ex2);
				ex = ex2;
			}
		}
		throw ex ?? new WebException();
	}

	public static bool IsCorrectVersion(this GameSession session)
	{
		return session.Version == GameManager.GetGameVersion();
	}

	public static void GenerateCheckSum(this GameSession gameSession)
	{
		int num = Prefab.AllPrefabs.Count.GetHashCode();
		foreach (Thing allPrefab in Prefab.AllPrefabs)
		{
			num ^= allPrefab.GetHashCode();
		}
		gameSession.Checksum = num.ToString("X");
	}

	public static bool ValidateCheckSum(this GameSession gameSession, GameSession other)
	{
		return gameSession.Checksum == other.Checksum;
	}
}
