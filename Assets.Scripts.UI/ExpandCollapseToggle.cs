using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ExpandCollapseToggle : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler
{
	public Action OnExpand;

	public Action OnCollapse;

	[SerializeField]
	private Sprite _expandedSprite;

	[SerializeField]
	private Sprite _collapsedSprite;

	[SerializeField]
	private Image _image;

	public bool Expanded { get; private set; }

	public void Set(bool on)
	{
		Expanded = on;
		_image.sprite = (Expanded ? _expandedSprite : _collapsedSprite);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		UIAudioManager.Play(UIAudioManager.ClickLightHash);
		Set(!Expanded);
		if (Expanded)
		{
			OnExpand?.Invoke();
		}
		else
		{
			OnCollapse?.Invoke();
		}
	}
}
