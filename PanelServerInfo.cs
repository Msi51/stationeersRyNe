using System.Collections.Generic;
using Assets.Scripts.PlayerInfo;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelServerInfo : MonoBehaviour
{
	public GameObject PlayerInfoPrefab;

	public GameObject ServerInfo;

	public GameObject TitleInfo;

	public LayoutGroup PlayerWindow;

	public static PanelServerInfo Instance;

	private ulong SteamId;

	private const int PlayerDataQueryRetryTimes = 10;

	private void Start()
	{
		Instance = this;
	}

	public void OpenServerInfo(ulong steamid)
	{
		SteamId = steamid;
		TitleInfo.transform.Find("SteamId").GetComponent<Text>().text = SteamId.ToString();
		UpdateServerInfo();
		RefreshServerInfo();
	}

	public void RefreshServerButton()
	{
		OpenServerInfo(SteamId);
	}

	public void OnCloseServerInfo()
	{
	}

	public void OnQueryServerInfo()
	{
		RefreshServerInfo();
	}

	public void OnJoinServer()
	{
	}

	private void InitServerInfo()
	{
		TitleInfo.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = "Loading";
		TitleInfo.transform.Find("Avatar").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
		TitleInfo.transform.Find("Spinner").gameObject.SetActive(value: true);
		DeleteAllPlayerInfo();
	}

	private void DeleteAllPlayerInfo()
	{
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in PlayerWindow.transform)
		{
			list.Add(item.gameObject);
		}
		list.ForEach(delegate(GameObject child)
		{
			Object.Destroy(child);
		});
	}

	private void RefreshServerInfo()
	{
		InitServerInfo();
		UpdateServerQuery();
	}

	private void UpdateServerQuery()
	{
		CancelServerQuery();
	}

	private void CancelServerQuery()
	{
	}

	public void UpdateServerInfo()
	{
	}

	private void RefreshPlayerList(Transform listTransform)
	{
		foreach (KeyValuePair<ulong, PlayerDetail> item in PlayerInfoManager.PlayerDictionary)
		{
			GameObject obj = Object.Instantiate(PlayerInfoPrefab);
			obj.transform.SetParent(listTransform.transform, worldPositionStays: false);
			obj.transform.Find("Avatar").GetComponent<Image>().ApplyAvatarSprite(item.Value.SteamId)
				.Forget();
			obj.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = item.Value.PlayerName;
			obj.transform.Find("Score").GetComponent<TextMeshProUGUI>().text = item.Value.Score.ToString();
			Transform transform = obj.transform.Find("SteamId");
			if (transform != null)
			{
				transform.GetComponent<Text>().text = item.Value.SteamId.ToString();
			}
		}
	}
}
