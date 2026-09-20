using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class DraggableWindow : Window, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
	public WindowTitleBar WindowTitleBar;

	public bool ChangeOrderOnFocus;

	private Vector3 _offset;

	public virtual bool DraggingEnabled => true;

	public new virtual void OnPointerEnter(PointerEventData eventData)
	{
	}

	public new virtual void OnPointerExit(PointerEventData eventData)
	{
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		if (DraggingEnabled)
		{
			_offset = Input.mousePosition - Transform.position;
		}
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		if (DraggingEnabled)
		{
			Vector3 worldPoint = Vector3.zero;
			if (RectTransformUtility.ScreenPointToWorldPointInRectangle(RectTransform, eventData.position, eventData.pressEventCamera, out worldPoint))
			{
				worldPoint -= _offset;
				RectTransform.position = worldPoint;
				ClampToScreen();
			}
		}
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (ChangeOrderOnFocus)
		{
			RectTransform.SetAsLastSibling();
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}

	public virtual void ToggleVisibility()
	{
		SetVisible(!IsVisible);
		if (IsVisible)
		{
			ClampToScreen();
		}
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		if (isVisble && ChangeOrderOnFocus)
		{
			RectTransform.SetAsLastSibling();
		}
	}
}
