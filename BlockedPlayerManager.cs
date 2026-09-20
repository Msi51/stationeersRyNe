using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlockedPlayerManager : MonoBehaviour
{
	public static string TargetName;

	public static ulong TargetSteamId;

	public static BlockedPlayerManager Instance;

	private GameObject TargetPanel;

	private Dictionary<ulong, string> Blocked = new Dictionary<ulong, string>();

	private void Awake()
	{
		Instance = this;
	}

	public bool IsBlocked(ulong steamId, out string remaining)
	{
		remaining = "forever";
		if (Blocked.TryGetValue(steamId, out var value) && (DateTime.Now.Ticks < long.Parse(value) || value.Equals("0")))
		{
			if (!value.Equals("0"))
			{
				long value2 = long.Parse(value) - DateTime.Now.Ticks;
				remaining = DateTime.Now.AddTicks(value2).ToString();
			}
			return true;
		}
		return false;
	}

	public void HidePopup()
	{
		if ((bool)TargetPanel)
		{
			TargetPanel.SetActive(value: false);
		}
	}

	public void ShowPopupForKickPlayer(string target, ulong steamId)
	{
		TargetName = target;
		TargetSteamId = steamId;
		TargetPanel = base.transform.GetChild(0).gameObject;
		TargetPanel.transform.GetChild(0).Find("Content").GetComponent<TextMeshProUGUI>()
			.text = $"Kick '{target}'?";
		TargetPanel.SetActive(value: true);
	}

	public void ShowPopupForBanPlayer(string target, ulong steamId)
	{
		TargetName = target;
		TargetSteamId = steamId;
		TargetPanel = base.transform.GetChild(1).gameObject;
		TargetPanel.transform.GetChild(0).Find("Content").GetComponent<TextMeshProUGUI>()
			.text = $"Ban '{target}'?";
		TargetPanel.SetActive(value: true);
	}

	public bool RemoveBanPlayer(ulong steamId)
	{
		if (Blocked.TryGetValue(steamId, out var _))
		{
			Blocked.Remove(steamId);
			return true;
		}
		return false;
	}

	public void SetBanPlayer(ulong steamId, double hours, string reason = "")
	{
		string remaining = "forever";
		if (hours > 0.0)
		{
			remaining = DateTime.Now.AddHours(hours).ToString();
		}
		BanPlayer(steamId, remaining, reason);
		string text = DateTime.Now.AddHours(hours).Ticks.ToString();
		if (Blocked.ContainsKey(steamId))
		{
			Blocked[steamId] = ((hours <= 0.0) ? hours.ToString() : text);
		}
		else
		{
			Blocked.Add(steamId, (hours <= 0.0) ? hours.ToString() : text);
		}
	}

	private void BanPlayer(ulong steamId, string remaining, string reason)
	{
		Debug.LogError("Previously called: NetworkManagerHudOverride.Instance.BanPlayer(steamId, remaining, reason);");
		throw new NotImplementedException();
	}

	public void ClickedYes(bool kickPlayer)
	{
		if (kickPlayer)
		{
			KickPlayer(TargetSteamId, TargetPanel.transform.GetComponentInChildren<TMP_InputField>().text);
		}
		else
		{
			SetBanPlayer(TargetSteamId, double.Parse(TargetPanel.GetComponentsInChildren<TMP_InputField>()[1].text), TargetPanel.GetComponentsInChildren<TMP_InputField>()[0].text);
		}
		ResetClient(TargetSteamId);
	}

	private void ResetClient(ulong targetSteamId)
	{
		Debug.LogError("Previously called: NetworkManagerHudOverride.NetworkManager.ResetClient(TargetSteamId);");
		throw new NotImplementedException();
	}

	private void KickPlayer(ulong targetSteamId, string text)
	{
		Debug.LogError("Previously called: NetworkManagerHudOverride.Instance.KickPlayer(TargetSteamId, TargetPanel.transform.GetComponentInChildren<TMP_InputField>().text);");
		throw new NotImplementedException();
	}
}
