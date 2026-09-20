using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class UserInterfaceClickEvent : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	public event Event OnClicked;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (this.OnClicked != null)
		{
			this.OnClicked();
		}
	}
}
