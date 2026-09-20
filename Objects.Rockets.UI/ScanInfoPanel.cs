using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets.UI;

public class ScanInfoPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Scan Info Panel")]
	[SerializeField]
	private ScanIcon _surveyIcon;

	[SerializeField]
	private ScanIcon _dicoveryIcon;

	[SerializeField]
	private ScanIcon _chartIcon;

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
			_surveyIcon.SetEnabled(enabled: false);
			_dicoveryIcon.SetEnabled(enabled: false);
			_chartIcon.SetEnabled(enabled: false);
			return;
		}
		if (currentNode.DiscoverData != null)
		{
			_dicoveryIcon.SetEnabled(enabled: true);
			_dicoveryIcon.SetText(StringManager.Get(currentNode.DiscoverPoints));
		}
		else
		{
			_dicoveryIcon.SetEnabled(enabled: false);
		}
		if (currentNode.ChartData != null)
		{
			_chartIcon.SetEnabled(enabled: true);
			_chartIcon.SetText(StringManager.Get(currentNode.ChartPoints));
		}
		else
		{
			_chartIcon.SetEnabled(enabled: false);
		}
		if (currentNode.SurveyData != null)
		{
			_surveyIcon.SetEnabled(enabled: true);
			string text = StringManager.Get(Mathf.RoundToInt(currentNode.SurveyPercent));
			_surveyIcon.SetText(text + "%");
		}
		else
		{
			_surveyIcon.SetEnabled(enabled: false);
		}
	}

	public void LocationChangedRefresh()
	{
	}

	public void Clear()
	{
	}
}
