using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.LoadGame;

public class StartLocationItem : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("Start Location Item")]
	[SerializeField]
	private Image _backgroundImage;

	[SerializeField]
	private TextMeshProUGUI _nameText;

	[SerializeField]
	private GameObject _border;

	private const float RowItemHoveredAlpha = 1f;

	private const float RowItemDefaultAlpha = 0.8f;

	public WorldPresetItem WorldPresetItem { get; private set; }

	public bool Selected { get; private set; }

	public StartLocationData Data { get; private set; }

	public void Initialise(WorldPresetItem worldPresetItem, StartLocationData data)
	{
		WorldPresetItem = worldPresetItem;
		Select(select: false, force: true);
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.8f);
		Data = data;
		TextMeshProUGUI nameText = _nameText;
		LocalizedStringReference localizedStringReference = Data.Name;
		nameText.text = ((localizedStringReference != null) ? ((string)localizedStringReference) : Data.Id);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		NewWorldMenu.Instance.StartLocationClicked(this);
	}

	public void Select(bool select, bool force = false)
	{
		if (force || Selected != select)
		{
			Selected = select;
			_border.SetActive(Selected);
			if (Selected)
			{
				LocalizedStringReference description = Data.Description;
				string value = ((description != null) ? ((string)description) : "Missing description");
				description = Data.Name;
				string value2 = ((description != null) ? ((string)description) : "Missing name");
				StringManager.ReusableStringBuilder.Clear();
				StringManager.ReusableStringBuilder.AppendLine(value2);
				StringManager.ReusableStringBuilder.AppendLine(value);
				string startLocationText = StringManager.ReusableStringBuilder.ToString();
				StringManager.ReusableStringBuilder.Clear();
				WorldPresetItem.SetStartLocationText(startLocationText);
			}
		}
	}

	public float CalculateSize()
	{
		float num = 65f;
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		return num;
	}

	public void OnWorldPresetItemExpanded()
	{
		TextMeshProUGUI nameText = _nameText;
		LocalizedStringReference localizedStringReference = Data.Name;
		nameText.text = ((localizedStringReference != null) ? ((string)localizedStringReference) : Data.Id);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(1f);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.8f);
	}
}
