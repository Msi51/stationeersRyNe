using System.Collections.Generic;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class LocationSiteDiscoveryPanel : UserInterfaceBase, IScrollHandler, IEventSystemHandler
{
	[Space(15f)]
	[Header("Site Discovery Panel")]
	[SerializeField]
	private SiteInfoRow _siteInfoRowPrefab;

	[SerializeField]
	private Transform _siteInfoRowParent;

	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private VerticalLayoutGroup _verticalLayout;

	private LocationPanel _locationPanel;

	public void Show(LocationPanel locationPanel)
	{
		SetVisible(isVisble: true);
		_locationPanel = locationPanel;
		ShowSiteDiscoveryInfo();
		ResizeScrollPanel().Forget();
	}

	private async UniTaskVoid ResizeScrollPanel()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_verticalLayout.transform);
		await UniTask.WaitForEndOfFrame();
		_scrollPanel.SetScrollPosition(0f);
		_scrollPanel.SetContentHeight(_verticalLayout.preferredHeight, force: true);
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
		Clear();
	}

	private void ClearSiteInfoRows()
	{
		foreach (Transform item in _siteInfoRowParent)
		{
			Object.Destroy(item.gameObject);
		}
	}

	private void ShowSiteDiscoveryInfo()
	{
		ClearSiteInfoRows();
		List<SpaceMapNodeData> list = _locationPanel?.CurrentNode?.DiscoverData?.SiteGenerations;
		if (list == null || list.Count == 0)
		{
			return;
		}
		foreach (SpaceMapNodeData item in list)
		{
			Object.Instantiate(_siteInfoRowPrefab, _siteInfoRowParent).SetValues(item);
		}
	}

	public void Refresh()
	{
	}

	public void Clear()
	{
		ClearSiteInfoRows();
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
