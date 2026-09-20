using System;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class PinButton : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler
{
	public Action OnClick;

	[SerializeField]
	private Image _image;

	[SerializeField]
	private Color _pinnedColor;

	[SerializeField]
	private Color _unPinnedColor;

	public void SetColor(bool pinned)
	{
		_image.color = (pinned ? _pinnedColor : _unPinnedColor);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		OnClick?.Invoke();
	}
}
