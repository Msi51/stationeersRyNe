using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ToggleButton : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler
{
	public Action OnClick;

	[SerializeField]
	private Color _onColor;

	[SerializeField]
	private Color _offColor;

	[SerializeField]
	private Image _image;

	public bool On { get; private set; }

	public void Set(bool on)
	{
		On = on;
		RefreshColor();
	}

	private void RefreshColor()
	{
		_image.color = (On ? _onColor : _offColor);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		UIAudioManager.Play(UIAudioManager.ClickLightHash);
		Set(!On);
		OnClick?.Invoke();
	}
}
