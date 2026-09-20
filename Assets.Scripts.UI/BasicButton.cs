using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class BasicButton : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler
{
	public Action OnClick;

	[Header("Basic Button")]
	[SerializeField]
	private Image _image;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	[Header("Bg Colors")]
	[SerializeField]
	private Color _defaultBackgroundColor;

	[SerializeField]
	private Color _hoveredBackgroundColor;

	[SerializeField]
	private Color _disabledColor;

	[Header("Text Colors")]
	[SerializeField]
	private Color _defaultTextColor;

	[SerializeField]
	private Color _disabledTextColor;

	private bool _hovered;

	private bool _interactable = true;

	public bool Interactable => _interactable;

	public bool Hovered => _hovered;

	public void SetInteractable(bool interactable)
	{
		_interactable = interactable;
		SetColor();
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
		SetColor();
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
		SetColor();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (_interactable)
		{
			UIAudioManager.Play(UIAudioManager.ClickLightHash);
			OnClick?.Invoke();
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		SetColor();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		_hovered = false;
	}

	private void SetColor()
	{
		if (!_interactable)
		{
			SetBackgroundColor(_disabledColor);
			SetTextColor(_disabledTextColor);
		}
		else
		{
			SetBackgroundColor(_hovered ? _hoveredBackgroundColor : _defaultBackgroundColor);
			SetTextColor(_defaultTextColor);
		}
	}

	private void SetBackgroundColor(Color color)
	{
		if (_image != null)
		{
			_image.color = color;
		}
	}

	private void SetTextColor(Color color)
	{
		if (_textMesh != null)
		{
			_textMesh.color = color;
		}
	}
}
