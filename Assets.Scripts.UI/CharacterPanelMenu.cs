using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class CharacterPanelMenu : GameBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public static bool IsMouseInside;

	public void OnPointerEnter(PointerEventData eventData)
	{
		IsMouseInside = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		IsMouseInside = false;
	}
}
