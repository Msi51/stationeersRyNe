using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class UserInterfaceBase : GameBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public delegate void Event();

	public RectTransform RectTransform;

	public UiComponentRenderer UiComponentRenderer;

	public override bool IsVisible
	{
		get
		{
			if (!(UiComponentRenderer != null))
			{
				return base.IsVisible;
			}
			return UiComponentRenderer.IsVisible;
		}
	}

	public event Event OnEnabled;

	public event Event OnDisabled;

	public override void SetVisible(bool isVisble)
	{
		if (UiComponentRenderer != null)
		{
			UiComponentRenderer.SetVisible(isVisble);
		}
		else
		{
			base.SetVisible(isVisble);
		}
	}

	public override void SetActive(bool active)
	{
		if (UiComponentRenderer != null)
		{
			UiComponentRenderer.SetVisible(active);
		}
		else
		{
			base.SetActive(active);
		}
	}

	public virtual void OnEnable()
	{
		if (this.OnEnabled != null)
		{
			this.OnEnabled();
		}
	}

	public virtual void OnDisable()
	{
		if (this.OnDisabled != null)
		{
			this.OnDisabled();
		}
	}

	public void ClampToScreen()
	{
		Vector3 localPosition = RectTransform.localPosition;
		Vector3 vector = localPosition;
		Rect rect = InventoryWindowManager.Instance.CanvasRectTransform.rect;
		Rect rect2 = RectTransform.rect;
		Vector3 vector2 = rect.min - rect2.min;
		Vector3 vector3 = rect.max - rect2.max;
		vector.x = Mathf.Clamp(localPosition.x, vector2.x, vector3.x);
		vector.y = Mathf.Clamp(localPosition.y, vector2.y, vector3.y);
		localPosition = vector;
		RectTransform.localPosition = localPosition;
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
	}
}
