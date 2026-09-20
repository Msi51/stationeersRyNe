using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets.UI;

public class LocationInfoPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Location Info Panel")]
	[SerializeField]
	private ScanIcon _sitesIcon;

	[SerializeField]
	private ScanIcon _navPointsIcon;

	private LocationPanel _locationPanel;

	public void Initialize(LocationPanel locationPanel)
	{
		_locationPanel = locationPanel;
	}

	public void Refresh()
	{
		SpaceMapNode currentNode = _locationPanel.CurrentNode;
		if (!currentNode.IsCharted)
		{
			_sitesIcon.SetEnabled(enabled: false);
			_navPointsIcon.SetEnabled(enabled: false);
			return;
		}
		if (currentNode.DiscoverData != null)
		{
			_sitesIcon.SetEnabled(enabled: true);
			string text = StringManager.Get(currentNode.DynamicNodeCount());
			string text2 = StringManager.Get(currentNode.DynamicNodeCapacity);
			_sitesIcon.SetText(text + " / " + text2);
		}
		else
		{
			_sitesIcon.SetEnabled(enabled: false);
		}
		if (currentNode.ChartData != null)
		{
			_navPointsIcon.SetEnabled(enabled: true);
			(int charted, int total) chartedNavPointCount = currentNode.GetChartedNavPointCount();
			string text3 = StringManager.Get(chartedNavPointCount.charted);
			string text4 = StringManager.Get(chartedNavPointCount.total);
			_navPointsIcon.SetText(text3 + " / " + text4);
		}
		else
		{
			_navPointsIcon.SetEnabled(enabled: false);
		}
	}

	public void LocationChangedRefresh()
	{
	}

	public void Clear()
	{
	}
}
