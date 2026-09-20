using System.Threading;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace UI.Tooltips;

public class UITooltipCanvas : UserInterfaceBase
{
	[SerializeField]
	private UITooltipPanel _tooltipPanel;

	[SerializeField]
	private int _tooltipDelayMs;

	private static UITooltipCanvas _instance;

	private readonly CancellationTokenWrapper _tooltipCancellation = new CancellationTokenWrapper();

	private UITooltip _currentTooltip;

	public static UITooltipCanvas Instance => _instance;

	private void ShowTooltipAfterDelay(UITooltip tooltip)
	{
		_tooltipCancellation.CancelAndInitialize();
		DelayShowTooltip(tooltip, _tooltipCancellation.Token).Forget();
	}

	private async UniTaskVoid DelayShowTooltip(UITooltip tooltip, CancellationToken cancellationToken)
	{
		await UniTask.Delay(_tooltipDelayMs, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		_tooltipPanel.Show(tooltip);
	}

	private void Awake()
	{
		_instance = this;
		_currentTooltip = null;
	}

	public void HideTooltip()
	{
		_tooltipCancellation.Cancel();
		_tooltipPanel.Hide();
	}

	public void DoUpdate()
	{
		Vector3 mousePosition = Input.mousePosition;
		string anonymousTooltip = UITooltipManager.GetAnonymousTooltip();
		UITooltip tooltip;
		if (!string.IsNullOrEmpty(anonymousTooltip))
		{
			_tooltipPanel.Show(anonymousTooltip);
			UITooltipManager.ClearTooltip();
		}
		else if (UITooltipManager.Current(mousePosition, out tooltip))
		{
			if (_currentTooltip != tooltip)
			{
				HideTooltip();
				_currentTooltip = tooltip;
				ShowTooltipAfterDelay(_currentTooltip);
			}
			else if (_tooltipPanel.GameObject.activeInHierarchy)
			{
				_tooltipPanel.Show(_currentTooltip);
			}
		}
		else if ((bool)_currentTooltip || _tooltipPanel.IsVisible)
		{
			_currentTooltip = null;
			HideTooltip();
		}
	}
}
