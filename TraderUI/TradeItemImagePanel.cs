using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TraderUI;

public class TradeItemImagePanel : GameBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	[SerializeField]
	private GameObject _stationpediaButton;

	[SerializeField]
	private Image _itemImage;

	private string _tooltipTitle;

	private string _tooltipDescription;

	private RectTransform _stationpediaButtonTransform;

	private TradeItem _tradeItem;

	public void Initialise(Sprite itemImage, string tooltipTitle, string tooltipDescription, TradeItem tradeItem)
	{
		_itemImage.sprite = itemImage;
		_tooltipTitle = tooltipTitle;
		_tooltipDescription = tooltipDescription;
		_tradeItem = tradeItem;
		_stationpediaButtonTransform = _stationpediaButton.transform as RectTransform;
		_stationpediaButton.SetActive(value: false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_stationpediaButton.SetActive(value: true);
		PanelToolTip.Instance.SetUpTooltip(_tooltipTitle, _tooltipDescription);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_stationpediaButton.SetActive(value: false);
		PanelToolTip.Instance.ClearToolTip();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(_stationpediaButtonTransform, eventData.position))
		{
			_tradeItem.StationpediaButtonClick();
		}
	}
}
