using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class WorldPresetItemTutorial : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("World Item")]
	[SerializeField]
	private TextMeshProUGUI _titleText;

	[SerializeField]
	private TextMeshProUGUI _descriptionText;

	[ReadOnly]
	public WorldSetting WorldSetting;

	public Image ModImage;

	[SerializeField]
	private RawImage _backgroundImage;

	private TutorialScenariosMenu _parentMenu;

	public PlanetScene PlanetScene { get; set; }

	public void Assign(WorldSetting worldSetting, TutorialScenariosMenu parent)
	{
		_parentMenu = parent;
		WorldSetting = worldSetting;
		_titleText.text = worldSetting.Name.ToString();
		_descriptionText.text = worldSetting.SummaryText;
		if ((bool)ModImage)
		{
			ModImage.enabled = worldSetting.Data.Mod != null;
		}
		_backgroundImage.texture = worldSetting.PreviewButton;
		_backgroundImage.SetImageSizePreserveAspect(RectTransform.Axis.Vertical, 200f);
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
}
