using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine.EventSystems;

namespace UI;

public class PlayerStatsPanel : UserInterfaceBase, IScreenSpaceTooltip
{
	public bool TooltipIsVisible => IsVisible;

	public static string StatDeltaString(float delta)
	{
		if (!(delta < 0f))
		{
			if (!(delta > 0f))
			{
				return GameStrings.PlayerStatsStable.DisplayString.AsColor("orange");
			}
			return GameStrings.PlayerStatsIncreasing.AsColor("green");
		}
		return GameStrings.PlayerStatsDecreasing.AsColor("red");
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (!InventoryManager.ParentHuman.IsArtificial)
		{
			string statsTooltip = InventoryManager.ParentHuman.GetStatsTooltip();
			PanelToolTip.Instance.SetUpTooltip(GameStrings.PlayerStatsTooltipTitle.DisplayString, statsTooltip, this);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		PanelToolTip.Instance.ClearToolTip();
	}

	public void DoUpdate()
	{
		string statsTooltip = InventoryManager.ParentHuman.GetStatsTooltip();
		PanelToolTip.Instance.SetInfoText(statsTooltip);
	}
}
