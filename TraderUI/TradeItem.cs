using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TraderUI;

public class TradeItem : GameBase
{
	[SerializeField]
	private TradeItemImagePanel _tradeItemImagePanel;

	[SerializeField]
	private TradeItemDisabledOverlay _disabledOverlay;

	[Space(20f)]
	[SerializeField]
	private TextMeshProUGUI _itemNameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _priceTextMesh;

	[SerializeField]
	private TextMeshProUGUI _leftInfoTextMesh;

	[SerializeField]
	private TextMeshProUGUI _rightInfoTextMesh;

	[SerializeField]
	private TextMeshProUGUI _confirmButtonTextMesh;

	[SerializeField]
	private TMP_InputField _amountInputField;

	[Space(20f)]
	[SerializeField]
	private Button _incrementButton;

	[SerializeField]
	private Button _decrementButton;

	[SerializeField]
	private Button _confirmButton;

	[Space(20f)]
	[SerializeField]
	private Color _insufficientCreditsColor;

	private int _tradeAmount;

	private int _maxTradeAmount;

	private float _totalCost;

	private string _confirmButtonText;

	private bool _buying;

	private TradeItemData _data;

	public void Initialise(TradeItemData data, bool buying)
	{
		_tradeAmount = 0;
		_data = data;
		_buying = buying;
		_confirmButtonText = (buying ? GameStrings.Buy.DisplayString : GameStrings.Sell.DisplayString);
		_itemNameTextMesh.text = data.ItemName;
		_tradeItemImagePanel.Initialise(data.ItemImage, data.ItemName, data.TooltipText, this);
		float num = 1f;
		if (InventoryManager.ParentHuman.ExperiencingRespawnStress)
		{
			FloatReference respawnStressTradePenalty = DifficultySetting.Current.RespawnStressTradePenalty;
			num = (_buying ? (1f / (float)respawnStressTradePenalty) : ((float)respawnStressTradePenalty));
		}
		string text = (data.Cost * num).ToString("0.00");
		_priceTextMesh.text = "€" + text;
		SetValues();
		SetTradeAmount(0);
		SetDisabledOverlay();
	}

	public void Refresh(TradeItemData data)
	{
		_data = data;
		SetValues();
		SetTradeAmount(_tradeAmount);
		SetDisabledOverlay();
	}

	public void StationpediaButtonClick()
	{
		Thing thing = _data.DataInstance?.GetItemPrefab();
		if (thing == null)
		{
			Stationpedia.OpenAt("GasPage");
		}
		else
		{
			Stationpedia.OpenAt(thing);
		}
	}

	private void SetValues()
	{
		if (_buying)
		{
			_leftInfoTextMesh.text = GameStrings.Available.DisplayString + ": " + StringManager.Get(_data.NumberAvailable);
			_rightInfoTextMesh.text = string.Empty;
		}
		else
		{
			_leftInfoTextMesh.text = GameStrings.Wants.DisplayString + ": " + StringManager.Get(_data.NumberWanted);
			_rightInfoTextMesh.text = GameStrings.Available.DisplayString + ": " + StringManager.Get(_data.NumberAvailable);
		}
		_maxTradeAmount = Mathf.Min(_data.NumberWanted, _data.NumberAvailable);
	}

	private void SetTradeAmount(int value)
	{
		_tradeAmount = Mathf.Clamp(value, 0, _maxTradeAmount);
		_amountInputField.text = StringManager.Get(_tradeAmount);
		FloatReference respawnStressTradePenalty = DifficultySetting.Current.RespawnStressTradePenalty;
		float num = ((!InventoryManager.ParentHuman.ExperiencingRespawnStress) ? 1f : (_buying ? (1f / (float)respawnStressTradePenalty) : ((float)respawnStressTradePenalty)));
		float num2 = _data.Cost * num;
		_totalCost = (float)_tradeAmount * num2;
		string text = _totalCost.ToString("0.00");
		_confirmButtonTextMesh.text = "€" + text + "\n" + _confirmButtonText;
		if (_buying && _totalCost > TraderCanvas.Instance.GetPlayerCredits())
		{
			_confirmButton.interactable = false;
			_confirmButtonTextMesh.color = _insufficientCreditsColor;
		}
		else
		{
			_confirmButton.interactable = true;
			_confirmButtonTextMesh.color = Color.white;
		}
	}

	private void SetDisabledOverlay()
	{
		if (_buying)
		{
			if (_data.NumberAvailable == 0)
			{
				_disabledOverlay.Show(GameStrings.SoldOut.DisplayString);
				return;
			}
		}
		else if (_data.NumberWanted == 0)
		{
			_disabledOverlay.Show(GameStrings.StockFull.DisplayString);
			return;
		}
		_disabledOverlay.Hide();
	}

	private void IncrementButtonClick()
	{
		SetTradeAmount(_tradeAmount + 1);
	}

	private void DecrementButtonClick()
	{
		SetTradeAmount(_tradeAmount - 1);
	}

	private void ConfirmButtonClick()
	{
		if (_tradeAmount != 0)
		{
			if (_buying)
			{
				TraderCanvas.Instance.BuyItem(_data, _tradeAmount, _totalCost);
			}
			else
			{
				TraderCanvas.Instance.SellItem(_data, _tradeAmount, _totalCost);
			}
			SetTradeAmount(0);
		}
	}

	private void AmountInputChanged(string value)
	{
		if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
		{
			SetTradeAmount(result);
		}
	}

	private void OnEnable()
	{
		_incrementButton.onClick.AddListener(IncrementButtonClick);
		_decrementButton.onClick.AddListener(DecrementButtonClick);
		_confirmButton.onClick.AddListener(ConfirmButtonClick);
		_amountInputField.onValueChanged.AddListener(AmountInputChanged);
	}

	private void OnDisable()
	{
		_incrementButton.onClick.RemoveListener(IncrementButtonClick);
		_decrementButton.onClick.RemoveListener(DecrementButtonClick);
		_confirmButton.onClick.RemoveListener(ConfirmButtonClick);
		_amountInputField.onValueChanged.RemoveListener(AmountInputChanged);
	}
}
