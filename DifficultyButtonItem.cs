using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyButtonItem : ButtonItem, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	[SerializeField]
	private RawImage _buttonImage;

	[SerializeField]
	private Image _highlightImage;

	[SerializeField]
	private TextMeshProUGUI _mainTextMesh;

	[SerializeField]
	private TextMeshProUGUI _subTextMesh;

	public Image ModImage;

	public Action<DifficultyButtonItem> OnClick { get; set; }

	public DifficultySetting DifficultySetting { get; private set; }

	public static implicit operator DifficultySetting(DifficultyButtonItem item)
	{
		return item.DifficultySetting;
	}

	public void Initialize(DifficultySetting difficultySetting)
	{
		base.name = "DifficultyButton_" + difficultySetting.Id;
		DifficultySetting = difficultySetting;
		_mainTextMesh.text = ((difficultySetting.Name != null) ? difficultySetting.Name.ToString() : difficultySetting.Id);
		_subTextMesh.text = difficultySetting.Description?.ToString();
		ModImage.enabled = difficultySetting.Mod != null;
		Texture2D texture = difficultySetting.PreviewButton?.Texture;
		_buttonImage.texture = texture;
		_buttonImage.SetNativeSize();
		Vector2 size = _buttonImage.rectTransform.rect.size;
		if (size.y < RectTransform.rect.size.y)
		{
			float num = RectTransform.rect.size.y / size.y;
			_buttonImage.rectTransform.sizeDelta = size * num;
		}
	}

	public void ShowHighlight(bool show)
	{
		if ((bool)_highlightImage)
		{
			_highlightImage.enabled = show;
		}
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		UIAudioManager.Play(UIAudioManager.HoverLargeHash);
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		UIAudioManager.Play(UIAudioManager.ClickLargeHash);
		OnClick?.Invoke(this);
	}
}
