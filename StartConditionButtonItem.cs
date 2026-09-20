using System;
using Assets.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartConditionButtonItem : ButtonItem, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
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

	public Action<StartConditionButtonItem> OnClick { get; set; }

	public StartConditionData StartCondition { get; private set; }

	public static implicit operator StartConditionData(StartConditionButtonItem item)
	{
		return item.StartCondition;
	}

	public void Initialize(StartConditionData startConditionData)
	{
		if (!startConditionData.IsValid())
		{
			startConditionData = DataCollection.Get<StartConditionData>(startConditionData.IdHash);
		}
		startConditionData.PreviewButton?.Load();
		StartCondition = startConditionData;
		ModImage.enabled = startConditionData.Mod != null;
		_mainTextMesh.text = startConditionData.Name;
		_subTextMesh.text = startConditionData.Description?.ToString();
		_buttonImage.texture = startConditionData.PreviewButton?.Texture;
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
