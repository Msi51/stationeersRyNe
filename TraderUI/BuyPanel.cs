using System.Collections.Generic;
using UnityEngine;

namespace TraderUI;

public class BuyPanel : GameBase
{
	[SerializeField]
	private Transform _content;

	[SerializeField]
	private TradeItem _tradeItemPrefab;

	private List<TradeItem> _tradeItems;

	public void Show()
	{
		GameObject.SetActive(value: true);
	}

	public void Hide()
	{
		GameObject.SetActive(value: false);
	}

	public void Initialise(List<TradeItemData> data)
	{
		Clear();
		foreach (TradeItemData datum in data)
		{
			TradeItem tradeItem = Object.Instantiate(_tradeItemPrefab, _content);
			tradeItem.Initialise(datum, buying: true);
			_tradeItems.Add(tradeItem);
		}
	}

	public void Refresh(List<TradeItemData> data)
	{
		if (data.Count != _tradeItems.Count)
		{
			Initialise(data);
			return;
		}
		for (int i = 0; i < data.Count; i++)
		{
			_tradeItems[i].Refresh(data[i]);
		}
	}

	public void Clear()
	{
		_tradeItems = new List<TradeItem>();
		foreach (Transform item in _content)
		{
			Object.Destroy(item.gameObject);
		}
	}
}
