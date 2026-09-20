using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using Trading;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace TraderUI;

public class TraderCanvas : InputWindowBase, IModal
{
	public static TraderCanvas Instance;

	[SerializeField]
	private GameObject _rootObject;

	[SerializeField]
	private BuyPanel _buyPanel;

	[SerializeField]
	private SellPanel _sellPanel;

	[SerializeField]
	private GameObject _processingOverlay;

	[Space(20f)]
	[SerializeField]
	private TabWell _tabWell;

	[SerializeField]
	private Button _closeButton;

	[SerializeField]
	private Button _departButton;

	[Space(20f)]
	[SerializeField]
	private TextMeshProUGUI _titleTextMesh;

	[SerializeField]
	private TextMeshProUGUI _playerNameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _playerCreditsTextMesh;

	private TradeData _data;

	private TradeDataHelper _helper;

	private bool _debugReadOnly;

	private bool _awaitingMessageFromServer;

	private static CancellationTokenWrapper _refreshUiCancellation = new CancellationTokenWrapper();

	public bool UnlockCursor => true;

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		_awaitingMessageFromServer = false;
		_refreshUiCancellation.Cancel();
		_rootObject.SetActive(value: true);
		SetVisible(isVisble: false);
	}

	public void Show(TradeData data, TradeDataHelper helper)
	{
		_debugReadOnly = false;
		ShowInternal(data, helper);
	}

	public void ShowDebug(TradeData data)
	{
		_debugReadOnly = true;
		ShowInternal(data, null);
	}

	private void ShowInternal(TradeData data, TradeDataHelper helper)
	{
		_helper = helper;
		_data = data;
		_awaitingMessageFromServer = false;
		_refreshUiCancellation.Cancel();
		SetVisible(isVisble: true);
		ShowProcessingOverlay(show: false);
		_buyPanel.Initialise(_data.Buying);
		_sellPanel.Initialise(_data.Selling);
		_titleTextMesh.text = GameStrings.TradingWith.DisplayString + " " + _data.TraderName;
		_playerNameTextMesh.text = _data.PlayerName;
		string text = _data.PlayerCredits.ToString("0.00");
		_playerCreditsTextMesh.text = "€" + text;
		_tabWell.Select(0);
		ShowBuyPanel();
		MouseModeController.AddModal(this);
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			Hide();
		}
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
		_awaitingMessageFromServer = false;
		_refreshUiCancellation.Cancel();
		_buyPanel.Clear();
		_sellPanel.Clear();
		MouseModeController.RemoveModal(this);
	}

	public void BuyItem(TradeItemData itemData, int amount, float cost)
	{
		if (_debugReadOnly || !(itemData.DataInstance is SellDataInstance sellDataInstance))
		{
			return;
		}
		if (GameManager.RunSimulation)
		{
			if (_helper.BuyItem(sellDataInstance, amount, cost, out var errorMessage))
			{
				Achievements.Achieve(Achievements.Kind.AchievementThatllBe350);
				ShowProcessingOverlay(show: true);
				_refreshUiCancellation.CancelAndInitialize();
				RefreshForTradeEvent();
			}
			else
			{
				ShowErrorPopup(errorMessage.DisplayString);
				_data = _helper.Convert();
				RefreshUI();
			}
		}
		else
		{
			ShowProcessingOverlay(show: true);
			_refreshUiCancellation.CancelAndInitialize();
			RefreshForTradeEvent();
			Achievements.Achieve(Achievements.Kind.AchievementThatllBe350);
			CreditCard creditCard = InventoryManager.ParentHuman?.GetCreditCard();
			RequestTrade requestTrade = new RequestTrade();
			requestTrade.TransactionDataInstanceId = sellDataInstance.ReferenceId;
			requestTrade.CreditCardId = creditCard?.ReferenceId ?? 0;
			requestTrade.TraderContactId = (_helper?.Contact?.ReferenceId).GetValueOrDefault();
			requestTrade.PlayerId = InventoryManager.Parent.OrganBrain.ClientId;
			requestTrade.Amount = amount;
			requestTrade.Cost = cost;
			requestTrade.Buying = true;
			requestTrade.SendToServer();
		}
	}

	public void SellItem(TradeItemData itemData, int amount, float cost)
	{
		if (_debugReadOnly || !(itemData.DataInstance is BuyDataInstance buyDataInstance))
		{
			return;
		}
		if (GameManager.RunSimulation)
		{
			if (_helper.SellItem(buyDataInstance, amount, cost, out var errorMessage))
			{
				ShowProcessingOverlay(show: true);
				_refreshUiCancellation.CancelAndInitialize();
				RefreshForTradeEvent();
				Achievements.Increment(Achievements.Stat.TotalCreditsFromSales, cost);
			}
			else
			{
				ShowErrorPopup(errorMessage.DisplayString);
				_data = _helper.Convert();
				RefreshUI();
			}
		}
		else
		{
			ShowProcessingOverlay(show: true);
			_refreshUiCancellation.CancelAndInitialize();
			RefreshForTradeEvent();
			CreditCard creditCard = InventoryManager.ParentHuman?.GetCreditCard();
			RequestTrade requestTrade = new RequestTrade();
			requestTrade.TransactionDataInstanceId = buyDataInstance.ReferenceId;
			requestTrade.CreditCardId = creditCard?.ReferenceId ?? 0;
			requestTrade.TraderContactId = (_helper?.Contact?.ReferenceId).GetValueOrDefault();
			requestTrade.PlayerId = InventoryManager.ParentHuman?.OrganBrain.ClientId ?? 0;
			requestTrade.Amount = amount;
			requestTrade.Cost = cost;
			requestTrade.Buying = false;
			requestTrade.SendToServer();
			Achievements.Increment(Achievements.Stat.TotalCreditsFromSales, cost);
		}
	}

	public float GetPlayerCredits()
	{
		return _data.PlayerCredits;
	}

	private void ShowErrorPopup(string errorMessage)
	{
		Singleton<ConfirmationPanel>.Instance.ShowWithRawMessage("TradeError", errorMessage, "ButtonOk");
	}

	private void ShowProcessingOverlay(bool show)
	{
		_processingOverlay.SetActive(show);
	}

	private void RefreshForTradeEvent()
	{
		_refreshUiCancellation.CancelAndInitialize();
		if (GameManager.RunSimulation)
		{
			RefreshUIAfterEventsProcessedServer(_refreshUiCancellation.Token).Forget();
		}
		else
		{
			RefreshUIAfterEventsProcessedClient(_refreshUiCancellation.Token).Forget();
		}
	}

	private async UniTaskVoid RefreshUIAfterEventsProcessedServer(CancellationToken token)
	{
		while (_helper.AwaitingAtmosEvent() && !token.IsCancellationRequested)
		{
			await UniTask.WaitForEndOfFrame(token);
		}
		uint tickStarted = GameManager.GameTickCount;
		while (GameManager.GameTickCount <= tickStarted && !token.IsCancellationRequested)
		{
			await UniTask.WaitForEndOfFrame(token);
		}
		_data = _helper.Convert();
		RefreshUI();
	}

	private async UniTaskVoid RefreshUIAfterEventsProcessedClient(CancellationToken token)
	{
		_awaitingMessageFromServer = true;
		while (_awaitingMessageFromServer && !token.IsCancellationRequested)
		{
			await UniTask.WaitForEndOfFrame(token);
		}
		_data = _helper.Convert();
		RefreshUI();
	}

	public void TradeResultMessage(bool success, int key)
	{
		if (!success && Assets.Scripts.Localization2.GameString.TryGet(key, out var gameString))
		{
			ShowErrorPopup(gameString.DisplayString);
		}
		_awaitingMessageFromServer = false;
	}

	private void RefreshUI()
	{
		ShowProcessingOverlay(show: false);
		_buyPanel.Refresh(_data.Buying);
		_sellPanel.Refresh(_data.Selling);
		string text = _data.PlayerCredits.ToString("0.00");
		_playerCreditsTextMesh.text = "€" + text;
	}

	private void CloseButtonClick()
	{
		Hide();
	}

	private void DepartButtonClick()
	{
		if (_debugReadOnly)
		{
			Hide();
		}
		else if (_helper.Depart())
		{
			Hide();
		}
	}

	private void TabSelected(int index)
	{
		switch (index)
		{
		case 0:
			ShowBuyPanel();
			break;
		case 1:
			ShowSellPanel();
			break;
		}
	}

	private void ShowSellPanel()
	{
		_sellPanel.Show();
		_buyPanel.Hide();
	}

	private void ShowBuyPanel()
	{
		_buyPanel.Show();
		_sellPanel.Hide();
	}

	private new void OnEnable()
	{
		_closeButton.onClick.AddListener(CloseButtonClick);
		_departButton.onClick.AddListener(DepartButtonClick);
		_tabWell.TabSelected.AddListener(TabSelected);
	}

	private new void OnDisable()
	{
		_closeButton.onClick.RemoveListener(CloseButtonClick);
		_departButton.onClick.RemoveListener(DepartButtonClick);
		_tabWell.TabSelected.RemoveListener(TabSelected);
	}

	public static async UniTaskVoid RefreshNextFrame(TraderContact contactParent)
	{
		if (!GameManager.IsBatchMode && (bool)Instance && GameManager.GameState == GameState.Running)
		{
			await UniTask.NextFrame();
			if ((bool)Instance && Instance.IsVisible && Instance._helper?.Contact == contactParent && Instance._helper != null)
			{
				Instance._data = Instance._helper.Convert();
				Instance.RefreshUI();
			}
		}
	}
}
