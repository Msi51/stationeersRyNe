using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Objects.Rockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class RocketMotherboardPanel : UserInterfaceBase
{
	[SerializeField]
	private TextMeshProUGUI _rocketNameText;

	[SerializeField]
	private TextMeshProUGUI _locationText;

	[SerializeField]
	private GameObject _locationTextGameObject;

	[SerializeField]
	private Image _rocketIcon;

	[SerializeField]
	private GameObject _rocketIconGameObject;

	[SerializeField]
	private Button _openUIButton;

	private IRocketPanelHolder _panelHolder;

	public void Initialize(IRocketPanelHolder panelHolder)
	{
		_panelHolder = panelHolder;
	}

	public void SetConnectedRocketInfo(RocketAvionicsDevice avionics)
	{
		if ((object)avionics == null)
		{
			_rocketNameText.text = GameStrings.NoRocketsConnected;
			_locationTextGameObject.SetActive(value: false);
			_rocketIconGameObject.SetActive(value: false);
		}
		else
		{
			_rocketNameText.text = avionics.Rocket.DisplayName;
			_locationText.text = avionics.GetCurrentNode()?.DisplayName ?? ((string)GameStrings.UnchartedLocation);
			_rocketIcon.sprite = avionics.Rocket.MapIcon;
			_locationTextGameObject.SetActive(value: true);
			_rocketIconGameObject.SetActive(value: true);
		}
	}

	private void Awake()
	{
		_openUIButton.onClick.AddListener(ToggleUI);
	}

	private void OnDestroy()
	{
		_openUIButton.onClick.RemoveListener(ToggleUI);
	}

	private void ToggleUI()
	{
		_panelHolder.ToggleUI();
	}
}
