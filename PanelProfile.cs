using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.PlayerInfo;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelProfile : MonoBehaviour
{
	public GameObject ProfilePanel;

	public GameObject StatPrefab;

	public GameObject KickButton;

	public GameObject BanButton;

	private bool IsScoreBoard = true;

	public GameObject Avatar;

	private LayoutGroup StatWindow;

	private ulong SteamId;

	private string PlayerName;

	public static PanelProfile Instance;

	private void Start()
	{
		Instance = this;
		StatWindow = ProfilePanel.GetComponentInChildren<LayoutGroup>();
	}

	public void OpenScoreboardPlayerProfile(GameObject PlayerObject)
	{
		IsScoreBoard = true;
		OpenPlayerProfile(PlayerObject);
	}

	public void OpenLeaderboardPlayerProfile(GameObject PlayerObject)
	{
		IsScoreBoard = false;
		OpenPlayerProfile(PlayerObject);
	}

	private void OpenPlayerProfile(GameObject PlayerObject)
	{
		SteamId = ulong.Parse(PlayerObject.transform.Find("SteamId").GetComponent<Text>().text);
		PlayerName = PlayerObject.transform.Find("Username").GetComponent<TextMeshProUGUI>().text;
		Debug.Log(PlayerName + " : " + SteamId);
		if (!GameManager.RunSimulation || !IsScoreBoard || NetworkManager.LocalClientId == SteamId)
		{
			KickButton.SetActive(value: false);
			BanButton.SetActive(value: false);
		}
		else
		{
			KickButton.SetActive(value: true);
			BanButton.SetActive(value: true);
		}
		ProfilePanel.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = PlayerObject.transform.Find("Username").GetComponent<TextMeshProUGUI>().text;
		ProfilePanel.SetActive(value: true);
	}

	public void ClosePlayerProfile()
	{
		ProfilePanel.SetActive(value: false);
	}

	public void KickPlayer()
	{
		BlockedPlayerManager.Instance.ShowPopupForKickPlayer(PlayerName, SteamId);
	}

	public void BanPlayer()
	{
		BlockedPlayerManager.Instance.ShowPopupForBanPlayer(PlayerName, SteamId);
	}

	private void SetAvatar()
	{
		Avatar.GetComponent<Image>().ApplyAvatarSprite(SteamId).Forget();
	}

	private void RefreshFileList(Transform listTransform)
	{
		SetAvatar();
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in listTransform)
		{
			list.Add(item.gameObject);
		}
		list.ForEach(delegate(GameObject child)
		{
			Object.Destroy(child);
		});
		PlayerDetail playerDetail = PlayerInfoManager.PlayerDictionary[SteamId];
		foreach (string key in StatKey.Keys)
		{
			GameObject obj = Object.Instantiate(StatPrefab);
			obj.transform.SetParent(listTransform.transform, worldPositionStays: false);
			obj.transform.Find("StatName").GetComponent<TextMeshProUGUI>().text = StatKey.GetDescription(key);
			string text = ((key == "PlayTime_Base_Map") ? NetUtils.FormatSeconds(playerDetail.PlayTimeBaseMap) : playerDetail.GetStat(key).ToString());
			obj.transform.Find("StatValue").GetComponent<TextMeshProUGUI>().text = text;
		}
	}
}
