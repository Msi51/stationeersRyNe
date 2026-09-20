using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ServerListManager : MonoBehaviour
{
	[SerializeField]
	private JoinWorldButton _serverListItemPrefab;

	[SerializeField]
	private TMP_InputField _addressInputField;

	[SerializeField]
	private Button _joinButton;

	[SerializeField]
	private Button _directConnectButton;

	[SerializeField]
	private Transform Content;

	[SerializeField]
	private GameObject Spinner;

	[SerializeField]
	private List<JoinWorldButton> _cachedServerItems = new List<JoinWorldButton>();

	[HideInInspector]
	public SessionSortType SortType;

	public GameObject ServerInfoMenu;

	public GameObject ServerDetailPanel;

	public Toggle FilterVersion;

	public Toggle FilterPassword;

	public Toggle FilterEmpty;

	public GameObject Refresh;

	public GameObject StopRefresh;

	private GameSession TargetServerItem;

	private static readonly int WrongVersion = Animator.StringToHash("WrongVersion");

	private NetworkClient _networkClient;

	private GameSession _selectedSession;

	private static readonly Regex ValidIpPattern = new Regex("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}:[0-9]{1,5}\\s*$");

	private static ServerListManager Instance { get; set; }

	public GameSession SelectedSession
	{
		get
		{
			return _selectedSession;
		}
		private set
		{
			_selectedSession = value;
			_joinButton.interactable = _selectedSession != null;
		}
	}

	private void Awake()
	{
		Instance = this;
		_networkClient = UnityEngine.Object.FindObjectOfType<NetworkClient>();
		FilterVersion.isOn = PlayerPrefs.GetInt("FILTER_VERSION", 1) == 1;
		FilterPassword.isOn = PlayerPrefs.GetInt("FILTER_PASSWORD", 0) == 1;
		FilterEmpty.isOn = PlayerPrefs.GetInt("FILTER_EMPTY", 0) == 1;
		Refresh.GetComponent<Button>().onClick.AddListener(UniTask.UnityAction(RefreshButtonClicked));
		StopRefresh.GetComponent<Button>().onClick.AddListener(StopRefreshButtonClicked);
		foreach (JoinWorldButton cachedServerItem in _cachedServerItems)
		{
			cachedServerItem.gameObject.SetActive(value: false);
		}
		_joinButton.onClick.AddListener(OnItemCommitted);
		_directConnectButton.onClick.AddListener(OnDirectConnect);
		_addressInputField.onValueChanged.AddListener(OnInputValueChanged);
	}

	private void OnInputValueChanged(string arg0)
	{
		_directConnectButton.interactable = IsValidIpAddressAndPort(_addressInputField.text);
	}

	public bool IsValidIpAddressAndPort(string address)
	{
		return ValidIpPattern.Match(address).Success;
	}

	public void OnChangedFilter(Toggle toggle)
	{
		if (FilterVersion == toggle)
		{
			PlayerPrefs.SetInt("FILTER_VERSION", toggle.isOn ? 1 : 0);
		}
		if (FilterPassword == toggle)
		{
			PlayerPrefs.SetInt("FILTER_PASSWORD", toggle.isOn ? 1 : 0);
		}
		if (FilterEmpty == toggle)
		{
			PlayerPrefs.SetInt("FILTER_EMPTY", toggle.isOn ? 1 : 0);
		}
		RefreshListBySort();
	}

	private void Start()
	{
		int value = PlayerPrefs.GetInt("JoinSortBy", 0);
		SortType = (SessionSortType)Mathf.Clamp(value, 0, Enum.GetNames(typeof(SessionSortType)).Length);
	}

	private void OnEnable()
	{
		RefreshButtonClicked().Forget();
		RefreshVersion();
	}

	private void OnDisable()
	{
		ServerInfoMenu.SetActive(value: false);
		ServerDetailPanel.SetActive(value: false);
		NetworkManager.CurrentTransport.CancelServerListRequest();
	}

	public void OnChangedSortOrder(TMP_Dropdown dropdown)
	{
		SortType = (SessionSortType)dropdown.value;
		PlayerPrefs.SetInt("JoinSortBy", (int)SortType);
		RefreshListBySort();
	}

	public void HideServerMenu()
	{
		if (TargetServerItem != null)
		{
			ServerInfoMenu.SetActive(value: false);
			TargetServerItem = null;
		}
	}

	public void ShowServerMenu(Vector3 pos, int index)
	{
		HideServerMenu();
		TargetServerItem = NetworkManager.GameSessionList[index];
		ServerInfoMenu.SetActive(value: true);
		RectTransform component = ServerInfoMenu.transform.Find("Outline").GetComponent<RectTransform>();
		component.SetPositionAndRotation(pos, Quaternion.identity);
		if (NetworkManager.GameSessionFavouriteList.Contains(TargetServerItem))
		{
			component.transform.GetChild(2).gameObject.SetActive(value: true);
			component.transform.GetChild(3).gameObject.SetActive(value: false);
		}
		else
		{
			component.transform.GetChild(2).gameObject.SetActive(value: false);
			component.transform.GetChild(3).gameObject.SetActive(value: true);
		}
	}

	public void ViewServerInfo()
	{
		ServerDetailPanel.SetActive(value: true);
		ServerDetailPanel.GetComponentInChildren<PanelServerInfo>().OpenServerInfo((uint)TargetServerItem.SessionId);
		HideServerMenu();
	}

	public void AddServerToFavorites()
	{
		if (TargetServerItem != null)
		{
			RefreshListBySort();
			HideServerMenu();
		}
	}

	public void RemoveServerToFavorites()
	{
		if (TargetServerItem != null)
		{
			RefreshListBySort();
			HideServerMenu();
		}
	}

	public async UniTaskVoid RefreshButtonClicked()
	{
		for (int num = _cachedServerItems.Count - 1; num >= 0; num--)
		{
			_cachedServerItems[num].gameObject.SetActive(value: false);
		}
		NetworkManager.ClearServerList();
		Spinner.SetActive(value: true);
		Refresh.SetActive(value: false);
		StopRefresh.SetActive(value: true);
		try
		{
			await NetworkManager.GetGameSessionList();
		}
		catch (UnityWebRequestException ex)
		{
			ConsoleWindow.PrintError(ex.Message);
			Singleton<ConfirmationPanel>.Instance.Show("ServerListCannotBeRetrieved", "CannotConnectToMetaServer", "ButtonOk");
		}
		RefreshListBySort();
		Spinner.SetActive(value: false);
		Refresh.SetActive(value: true);
		StopRefresh.SetActive(value: false);
	}

	public void StopRefreshButtonClicked()
	{
		NetworkManager.CurrentTransport.CancelServerListRequest();
		Refresh.SetActive(value: true);
		StopRefresh.SetActive(value: false);
		Spinner.SetActive(value: false);
	}

	private IEnumerator Debounce(float waitTime, TMP_InputField input)
	{
		yield return new WaitForSeconds(waitTime);
		SearchServerName(input);
	}

	public void SearchServerNameDebounced(TMP_InputField input)
	{
		StopAllCoroutines();
		StartCoroutine(Debounce(0.25f, input));
	}

	public void SearchServerName(TMP_InputField input)
	{
		foreach (JoinWorldButton cachedServerItem in _cachedServerItems)
		{
			cachedServerItem.gameObject.SetActive(cachedServerItem.ServerName.ToLower().Contains(input.text.ToLower()));
		}
	}

	private void RefreshVersion()
	{
		for (int i = 0; i < _cachedServerItems.Count; i++)
		{
			JoinWorldButton joinWorldButton = _cachedServerItems[i];
			UIAudioComponent component = joinWorldButton.GetComponent<UIAudioComponent>();
			List<GameSession> gameSessionList = NetworkManager.GameSessionList;
			if (i < gameSessionList.Count)
			{
				bool flag = gameSessionList[i].IsCorrectVersion();
				joinWorldButton.GetComponent<Button>().interactable = flag;
				component.buttonValid = flag;
			}
		}
	}

	private void RefreshListBySort()
	{
		for (int num = _cachedServerItems.Count - 1; num >= 0; num--)
		{
			_cachedServerItems[num].gameObject.SetActive(value: false);
		}
		List<GameSession> list = NetworkManager.GameSessionList.Where(CanAddToServerList).SortGameSessionList(SortType);
		for (int i = 0; i < list.Count; i++)
		{
			GameSession gameSession = list[i];
			JoinWorldButton joinWorldButton2;
			if (i >= _cachedServerItems.Count)
			{
				Spinner.SetActive(value: false);
				JoinWorldButton joinWorldButton = UnityEngine.Object.Instantiate(_serverListItemPrefab, Content);
				joinWorldButton.transform.localScale = Vector3.one;
				_cachedServerItems.Add(joinWorldButton);
				joinWorldButton2 = joinWorldButton;
			}
			else
			{
				joinWorldButton2 = _cachedServerItems[i];
				joinWorldButton2.gameObject.SetActive(value: true);
			}
			joinWorldButton2.SetData(gameSession);
			joinWorldButton2.OnClickSelected = OnItemSelected;
			DoubleClick component = joinWorldButton2.GetComponent<DoubleClick>();
			component.OnDoubleClick.RemoveListener(OnItemCommitted);
			component.OnDoubleClick.AddListener(OnItemCommitted);
			Animator component2 = joinWorldButton2.GetComponent<Animator>();
			if ((bool)component2 && !component2.transform.root.gameObject.activeSelf)
			{
				break;
			}
			bool flag = gameSession.IsCorrectVersion();
			if ((bool)component2)
			{
				component2.SetBool(WrongVersion, !flag);
			}
			joinWorldButton2.GetComponent<UIAudioComponent>().buttonValid = flag;
			joinWorldButton2.GetComponent<Button>().interactable = flag;
		}
	}

	private void OnItemSelected(GameSession session)
	{
		SelectedSession = session;
	}

	private void OnDirectConnect()
	{
		string text = null;
		if (!string.IsNullOrEmpty(_addressInputField.text))
		{
			text = _addressInputField.text.Trim();
		}
		if (!string.IsNullOrEmpty(text))
		{
			_networkClient.JoinClientFromMenu(text);
		}
	}

	private void OnItemCommitted()
	{
		string text = null;
		if (SelectedSession != null)
		{
			text = SelectedSession.Address + ":" + SelectedSession.Port;
		}
		bool flag = false;
		if (Settings.CurrentData.UseSteamP2P)
		{
			GameSession selectedSession = SelectedSession;
			if (selectedSession != null && selectedSession.HostSteamId.IsValid)
			{
				flag = _networkClient.JoinWithSteamP2P(SelectedSession.HostSteamId);
			}
		}
		if (!flag && !string.IsNullOrEmpty(text))
		{
			_networkClient.JoinClientFromMenu(text);
		}
	}

	private bool CanAddToServerList(GameSession session)
	{
		if (FilterVersion.isOn && !session.IsCorrectVersion())
		{
			return false;
		}
		if (FilterPassword.isOn && session.Password)
		{
			return false;
		}
		if (FilterEmpty.isOn && session.Players == 0)
		{
			return false;
		}
		return true;
	}
}
