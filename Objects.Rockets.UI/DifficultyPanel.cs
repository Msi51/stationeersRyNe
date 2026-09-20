using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets.UI;

public class DifficultyPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Difficulty Panel")]
	[SerializeField]
	private ScanIcon _survey;

	[SerializeField]
	private ScanIcon _discover;

	[SerializeField]
	private ScanIcon _chart;

	private LocationPanel _locationPanel;

	private Color _iconColor;

	public void Initialize(LocationPanel locationPanel)
	{
		_locationPanel = locationPanel;
	}

	public void Refresh()
	{
		SpaceMapNode currentNode = _locationPanel.CurrentNode;
		if (currentNode.DiscoverData != null)
		{
			_discover.SetEnabled(enabled: true);
			_discover.SetText(StringManager.Get(currentNode.DiscoverData.Difficulty));
		}
		else
		{
			_discover.SetEnabled(enabled: false);
		}
		if (currentNode.SurveyData != null)
		{
			_survey.SetEnabled(enabled: true);
			_survey.SetText(StringManager.Get(currentNode.SurveyData.Difficulty));
		}
		else
		{
			_survey.SetEnabled(enabled: false);
		}
		if (currentNode.ChartData != null)
		{
			_chart.SetText(StringManager.Get(currentNode.GetHighestChartDifficulty()));
			_chart.SetEnabled(enabled: true);
		}
		else
		{
			_chart.SetEnabled(enabled: false);
		}
	}

	public void Clear()
	{
	}
}
