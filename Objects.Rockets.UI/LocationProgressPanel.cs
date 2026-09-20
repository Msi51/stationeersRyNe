using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Assets.Scripts.Util;
using Objects.Rockets.Scanning;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class LocationProgressPanel : UserInterfaceBase, IScrollHandler, IEventSystemHandler
{
	[Space(15f)]
	[Header("Progress Panel")]
	[SerializeField]
	private Transform progressDisplaysParent;

	[SerializeField]
	private ActionProgressDisplay progressDisplayPrefab;

	[SerializeField]
	private TransferActionProgressDisplay transferActionProgressDisplayPrefab;

	[SerializeField]
	private TextMeshProUGUI _currentActionNameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _currentActionInfoTextMesh;

	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private VerticalLayoutGroup _scrollPanelVerticalLayout;

	private LocationPanel _locationPanel;

	public List<ActionProgressDisplay> instancedDisplays = new List<ActionProgressDisplay>();

	public List<TransferActionProgressDisplay> instancedTransferDisplays = new List<TransferActionProgressDisplay>();

	public const int DisplayInstanceCount = 12;

	public void Initialize(LocationPanel locationPanel)
	{
		_locationPanel = locationPanel;
		if (instancedDisplays.Count == 0)
		{
			for (int i = 0; i < 12; i++)
			{
				instancedDisplays.Add(Object.Instantiate(progressDisplayPrefab, progressDisplaysParent));
			}
		}
		if (instancedTransferDisplays.Count == 0)
		{
			for (int j = 0; j < 12; j++)
			{
				instancedTransferDisplays.Add(Object.Instantiate(transferActionProgressDisplayPrefab, progressDisplaysParent));
			}
		}
		Refresh();
	}

	public void Refresh()
	{
		_currentActionNameTextMesh.text = GameStrings.RocketLocationCurrentAction.AsString(GetModeString());
		RocketAvionicsDevice rocketAvionicsDevice = _locationPanel.Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice)
		{
			Clear();
			return;
		}
		if (_locationPanel.CurrentNode != rocketAvionicsDevice.GetCurrentNode())
		{
			Clear();
			return;
		}
		List<IRocketActionProgressable> list = rocketAvionicsDevice.Rocket.ActionProgressables();
		for (int i = 0; i < instancedDisplays.Count; i++)
		{
			bool flag = list.Count > i && list[i] != null && !(list[i] is IRocketTransferActionProgressable);
			instancedDisplays[i].SetVisible(flag);
			if (flag)
			{
				instancedDisplays[i].Apply(new ProgressDisplayData(list[i]));
			}
		}
		for (int j = 0; j < instancedTransferDisplays.Count; j++)
		{
			bool flag2 = list.Count > j && list[j] != null && list[j] is IRocketTransferActionProgressable;
			instancedTransferDisplays[j].SetVisible(flag2);
			if (flag2)
			{
				instancedTransferDisplays[j].Apply(new ProgressDisplayData(list[j] as IRocketTransferActionProgressable));
			}
		}
		_currentActionInfoTextMesh.text = ((list.Count > 0) ? GetModeInfoString() : GetModeInfoInvalidString());
		_scrollPanel.SetContentHeight(_scrollPanelVerticalLayout.preferredHeight);
	}

	private string GetModeString()
	{
		RocketAvionicsDevice rocketAvionicsDevice = _locationPanel.Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice || _locationPanel.CurrentNode != rocketAvionicsDevice.GetCurrentNode())
		{
			return GameStrings.None.DisplayString;
		}
		return rocketAvionicsDevice.Rocket.RocketMode switch
		{
			RocketMode.Mine => GameStrings.RocketActionMining.DisplayString, 
			RocketMode.Survey => GameStrings.RocketActionSurveying.DisplayString, 
			RocketMode.Discover => GameStrings.RocketActionDiscovering.DisplayString, 
			RocketMode.Chart => GameStrings.RocketActionCharting.DisplayString, 
			RocketMode.Deploy => GameStrings.RocketActionDeploying.DisplayString, 
			RocketMode.SurfaceScan => GameStrings.RocketActionSurfaceScan.DisplayString, 
			RocketMode.Transfer => GameStrings.RocketActionTransfer.DisplayString, 
			_ => GameStrings.None.DisplayString, 
		};
	}

	private string MiningModeInfoString()
	{
		return GameStrings.RocketActionMiningTotalOre.AsString(GetTotalOreAtLocation()) + "\n" + GameStrings.RocketActionMiningTotalOreMined.AsString(GetTotalOreMinedAtLocation());
	}

	private string GetModeInfoString()
	{
		RocketAvionicsDevice rocketAvionicsDevice = _locationPanel.Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice)
		{
			return string.Empty;
		}
		return rocketAvionicsDevice.Rocket.RocketMode switch
		{
			RocketMode.Mine => MiningModeInfoString(), 
			RocketMode.Survey => ProgressText(GameStrings.RocketActionSurveyProgress.DisplayString, _locationPanel.CurrentNode.SurveyPoints, _locationPanel.CurrentNode.SurveyData.Difficulty), 
			RocketMode.Discover => ProgressText(GameStrings.RocketActionDiscoverProgress.DisplayString, _locationPanel.CurrentNode.DiscoverPoints, _locationPanel.CurrentNode.DiscoverData.Difficulty), 
			RocketMode.Chart => ProgressText(GameStrings.RocketActionChartProgress.DisplayString, _locationPanel.CurrentNode.ChartPoints, _locationPanel.CurrentNode.GetHighestChartDifficulty()), 
			RocketMode.Deploy => string.Empty, 
			RocketMode.SurfaceScan => string.Empty, 
			_ => string.Empty, 
		};
	}

	private string GetTotalOreAtLocation()
	{
		return StringManager.Get((_locationPanel?.CurrentNode?.Deposit?.TotalOreAtLocation).GetValueOrDefault());
	}

	private string GetTotalOreMinedAtLocation()
	{
		return StringManager.Get((_locationPanel?.CurrentNode?.Deposit?.MinedQuantityTotal).GetValueOrDefault());
	}

	private string GetModeInfoInvalidString()
	{
		RocketAvionicsDevice rocketAvionicsDevice = _locationPanel.Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice)
		{
			return string.Empty;
		}
		return (rocketAvionicsDevice.Rocket.RocketMode switch
		{
			RocketMode.Mine => GameStrings.NoRocketMiner.DisplayString, 
			RocketMode.Survey => GameStrings.NoRocketScanner.DisplayString, 
			RocketMode.Discover => GameStrings.NoRocketScanner.DisplayString, 
			RocketMode.Chart => GameStrings.NoRocketScanner.DisplayString, 
			RocketMode.Deploy => GameStrings.UnableToDeploy.DisplayString, 
			RocketMode.SurfaceScan => GameStrings.NoRocketScanner.DisplayString, 
			_ => string.Empty, 
		}).AsColor("red");
	}

	private string ProgressText(string name, float current, float total)
	{
		string text = StringManager.Get(current);
		string text2 = StringManager.Get(total);
		return name + ": " + text + "/" + text2;
	}

	public void Clear()
	{
		for (int i = 0; i < instancedDisplays.Count; i++)
		{
			instancedDisplays[i].SetVisible(isVisble: false);
		}
		for (int j = 0; j < instancedTransferDisplays.Count; j++)
		{
			instancedTransferDisplays[j].SetVisible(isVisble: false);
		}
		_currentActionInfoTextMesh.text = string.Empty;
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
