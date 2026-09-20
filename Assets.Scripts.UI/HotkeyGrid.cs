using UnityEngine;

namespace Assets.Scripts.UI;

public class HotkeyGrid : UserInterfaceBase
{
	public SlotHotkeyIndicator Hotkey = new SlotHotkeyIndicator();

	public SlotHotkeyIndicator Interaction = new SlotHotkeyIndicator();

	public SlotHotkeyIndicator Inventory = new SlotHotkeyIndicator();

	public GameObject ParentBackground;

	public UiComponentRenderer ParentBackgroundRenderer;

	public bool HasParentBackground
	{
		get
		{
			if (!ParentBackground)
			{
				return ParentBackgroundRenderer;
			}
			return true;
		}
	}

	public void HideAll()
	{
		Interaction.SetVisible(isVisible: false);
		Inventory.SetVisible(isVisible: false);
		SetParentBackGroundVisible(isVisible: false);
	}

	public void SetParentBackGroundVisible(bool isVisible)
	{
		if ((bool)ParentBackgroundRenderer)
		{
			ParentBackgroundRenderer.SetVisible(isVisible);
		}
		else if ((bool)ParentBackground)
		{
			ParentBackground.SetActive(isVisible);
		}
	}
}
