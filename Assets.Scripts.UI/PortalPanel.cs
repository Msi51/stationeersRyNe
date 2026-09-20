using System.Collections.Generic;
using Assets.Scripts.PlayerInfo;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PortalPanel : MonoBehaviour, IModal
{
	public GameObject PlayerInfoPrefab;

	public GameObject ServerInfo;

	public GameObject TitleInfo;

	public GameObject PortalWindow;

	public LayoutGroup PlayerWindow;

	public static PortalPanel Instance;

	public static InputPanelState InputState;

	private ulong SteamId;

	private const int PlayerDataQueryRetryTimes = 10;

	public bool UnlockCursor => true;

	private void Start()
	{
		Instance = this;
	}

	public void OpenServerInfo(GameObject PlayerObject)
	{
		OpenServerInfo(ulong.Parse(PlayerObject.transform.Find("SteamId").GetComponent<Text>().text));
	}

	public void OpenServerInfo(ulong steamid)
	{
		if (InputState == InputPanelState.None)
		{
			MouseModeController.AddModal(this);
			PortalWindow.SetActive(value: true);
			SteamId = steamid;
			TitleInfo.transform.Find("SteamId").GetComponent<Text>().text = SteamId.ToString();
			RefreshServerInfo();
		}
	}

	public void RefreshServerButton()
	{
		OpenServerInfo(SteamId);
	}

	public void OnCloseServerInfo()
	{
		InputState = InputPanelState.Cancelled;
		InputState = InputPanelState.None;
		PortalWindow.SetActive(value: false);
		MouseModeController.RemoveModal(this);
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

	private static void ServerRulesQuery()
	{
	}

	private void UpdatePlayerAvatar(string steamId, string Username)
	{
		foreach (Transform item in PlayerWindow.transform)
		{
			if (!item.Find("Username").GetComponent<TextMeshProUGUI>().text.Trim().Equals(Username))
			{
				continue;
			}
			Image component = item.Find("Avatar").GetComponent<Image>();
			if (null == component.sprite)
			{
				component.ApplyAvatarSprite(ulong.Parse(steamId)).Forget();
				component.color = new Color(1f, 1f, 1f, 1f);
				Transform transform2 = item.Find("SteamId");
				if (transform2 != null)
				{
					transform2.GetComponent<Text>().text = steamId;
				}
			}
		}
	}

	private void OnRulesRefreshComplete()
	{
	}

	private void OnAddPlayerToList(string pchName, int nScore, float flTimePlayed)
	{
		GameObject gameObject = Object.Instantiate(PlayerInfoPrefab);
		gameObject.transform.SetParent(PlayerWindow.transform, worldPositionStays: false);
		gameObject.transform.Find("Avatar").GetComponent<Image>().enabled = false;
		gameObject.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = pchName;
		gameObject.transform.Find("Score").GetComponent<TextMeshProUGUI>().text = nScore.ToString();
		gameObject.transform.Find("Playtime").GetComponent<TextMeshProUGUI>().text = FormatSeconds((int)flTimePlayed);
		AddEventTriggerListener(gameObject.GetComponent<EventTrigger>(), EventTriggerType.PointerDown, gameObject);
	}

	private void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, GameObject PlayerObject)
	{
		if (PanelProfile.Instance != null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = eventType;
			entry.callback = new EventTrigger.TriggerEvent();
			entry.callback.AddListener(delegate
			{
				PanelProfile.Instance.OpenLeaderboardPlayerProfile(PlayerObject);
			});
			trigger.triggers.Add(entry);
		}
	}

	private string FormatSeconds(int Elapsed)
	{
		int num = Elapsed % 60;
		int num2 = Elapsed / 60 % 60;
		int num3 = Elapsed / 60 / 60;
		return $"{num3:00}:{num2:00}:{num:00}";
	}

	private void OnPlayersFailedToRespond()
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
