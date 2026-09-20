using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(Selectable))]
public class UISelectableTransition : UIBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
	[SerializeField]
	private Selectable _selectable;

	[ReadOnly]
	public UiTransitionState CurrentState;

	[SerializeField]
	private List<UISelectableGraphicBehaviour> _graphicBehaviours;

	private bool _isPointerInside;

	private bool IsSelected => EventSystem.current.currentSelectedGameObject == base.gameObject;

	[ContextMenu("Interacable On")]
	private void _InteractableOn()
	{
		SetInteractivity(active: true);
	}

	[ContextMenu("Interacable Off")]
	private void _InteractableOff()
	{
		SetInteractivity(active: false);
	}

	public void SetInteractivity(bool active)
	{
		_selectable.interactable = active;
		if (active)
		{
			SetState(IsSelected ? UiTransitionState.Selected : UiTransitionState.Normal);
		}
		else
		{
			SetState(UiTransitionState.Disabled);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_isPointerInside = true;
		SetState(UiTransitionState.Highlighted);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_isPointerInside = false;
		SetState(IsSelected ? UiTransitionState.Selected : UiTransitionState.Normal);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		SetState(UiTransitionState.Pressed);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		SetState((_isPointerInside || IsSelected) ? UiTransitionState.Highlighted : UiTransitionState.Normal);
	}

	public void OnSelect(BaseEventData eventData)
	{
		SetState(UiTransitionState.Selected);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		SetState(UiTransitionState.Normal);
	}

	private void SetState(UiTransitionState state)
	{
		CurrentState = state;
		foreach (UISelectableGraphicBehaviour item in _graphicBehaviours.Where((UISelectableGraphicBehaviour x) => x))
		{
			item.SetState(state);
		}
	}
}
