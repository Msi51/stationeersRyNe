using System.Collections.Generic;
using TMPro;
using UI.LoadGame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class WorldPresetItem : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("World Item")]
	[SerializeField]
	private TextMeshProUGUI _titleText;

	[SerializeField]
	private TextMeshProUGUI _startLocationText;

	[SerializeField]
	private TextMeshProUGUI _worldShortDescriptionText;

	[SerializeField]
	private GameObject _worldShortDescriptionGameObject;

	[ReadOnly]
	public WorldSetting WorldSetting;

	public Image ModImage;

	[SerializeField]
	private RawImage _backgroundImage;

	[SerializeField]
	private StartLocationItem startLocationItemPrefab;

	[SerializeField]
	private Transform startLocationItemParent;

	[SerializeField]
	private GameObject _bottomPanelGameObject;

	[SerializeField]
	public RectTransform _bottomPanelRectTransform;

	[SerializeField]
	private VerticalLayoutGroup startLocationsVerticalLayoutGroup;

	private NewWorldMenu _parentMenu;

	public PlanetScene PlanetScene { get; set; }

	public bool Expanded { get; private set; }

	public List<StartLocationItem> StartLocationItems { get; } = new List<StartLocationItem>();

	public void SetStartLocationText(string text)
	{
		_startLocationText.text = text;
	}

	public void Assign(WorldSetting worldSetting, NewWorldMenu parent)
	{
		_parentMenu = parent;
		WorldSetting = worldSetting;
		_titleText.text = worldSetting.Name.ToString();
		TextMeshProUGUI worldShortDescriptionText = _worldShortDescriptionText;
		LocalizedStringReference shortDescription = worldSetting.Data.ShortDescription;
		worldShortDescriptionText.text = ((shortDescription != null) ? ((string)shortDescription) : "Missing description");
		if ((bool)ModImage)
		{
			ModImage.enabled = worldSetting.Data.Mod != null;
		}
		_backgroundImage.texture = worldSetting.PreviewButton;
		_backgroundImage.SetNativeSize();
		Vector2 size = _backgroundImage.rectTransform.rect.size;
		if (size.y < RectTransform.rect.size.y)
		{
			float num = RectTransform.rect.size.y / size.y;
			_backgroundImage.rectTransform.sizeDelta = size * num;
		}
		foreach (StartLocationData startLocationData in worldSetting.Data.StartLocationDatas)
		{
			StartLocationItem startLocationItem = Object.Instantiate(startLocationItemPrefab, startLocationItemParent);
			startLocationItem.Initialise(this, DataCollection.Get<StartLocationData>(startLocationData.Id));
			StartLocationItems.Add(startLocationItem);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		UIAudioManager.Play(UIAudioManager.ClickLargeHash);
		if (this != NewWorldMenu.SelectedWorld)
		{
			NewWorldMenu.SelectedStartLocation = null;
		}
		_parentMenu.SelectNewWorld(this);
	}

	public bool HasStorms()
	{
		return WorldSetting.WeatherEvents.Count > 0;
	}

	private void SetBackgroundImageSize(float panelHeight)
	{
		int num = _backgroundImage.texture.width / _backgroundImage.texture.height;
		_backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
		_backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelHeight * (float)num);
	}

	public float CalculateSize()
	{
		float num = 200f;
		float num2 = 0f;
		if (Expanded)
		{
			foreach (StartLocationItem startLocationItem in StartLocationItems)
			{
				num2 += startLocationItem.CalculateSize();
			}
			num2 += NewWorldMenu.CalculateVerticalExtraSize(startLocationsVerticalLayoutGroup, StartLocationItems.Count);
		}
		num2 = Mathf.Max(num2, 0f);
		float num3 = num2 + num;
		_bottomPanelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3);
		SetBackgroundImageSize(num3);
		LayoutRebuilder.MarkLayoutForRebuild(_bottomPanelRectTransform);
		return num3;
	}

	public void Expand(bool expand, bool force = false)
	{
		if (!force && Expanded == expand)
		{
			return;
		}
		Expanded = expand;
		_bottomPanelGameObject.SetActive(expand);
		_worldShortDescriptionGameObject.SetActive(expand);
		if (expand)
		{
			foreach (StartLocationItem startLocationItem in StartLocationItems)
			{
				startLocationItem.OnWorldPresetItemExpanded();
			}
			return;
		}
		foreach (StartLocationItem startLocationItem2 in StartLocationItems)
		{
			startLocationItem2.Select(select: false);
		}
	}
}
