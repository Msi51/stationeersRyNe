using Assets.Scripts.GridSystem;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class ToggleVisibleOnHover : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public UserInterfaceBase TargetUI;

	private bool _wasHidden;

	public new void OnPointerEnter(PointerEventData eventData)
	{
		if (GameManager.GameState == GameState.Running && (bool)TargetUI && !TargetUI.IsVisible)
		{
			_wasHidden = true;
			TargetUI.SetVisible(isVisble: true);
		}
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		if (GameManager.GameState == GameState.Running && (bool)TargetUI && TargetUI.IsVisible)
		{
			_wasHidden = false;
			TargetUI.SetVisible(isVisble: false);
		}
	}
}
