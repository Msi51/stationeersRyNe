using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopModListItem : MonoBehaviour
{
	private const string DisabledColorString = "#808080";

	private const string ErrorColorString = "red";

	[SerializeField]
	private Image _icon;

	[SerializeField]
	private TextMeshProUGUI _title;

	[SerializeField]
	private Button _selectButton;

	[SerializeField]
	private Button _upButton;

	[SerializeField]
	private Button _downButton;

	[SerializeField]
	private Toggle _enableToggle;

	public ModData Data { get; private set; }

	public static event Action<WorkshopModListItem> Enabled;

	public static event Action<WorkshopModListItem> Selected;

	public static event Action<WorkshopModListItem> MoveUp;

	public static event Action<WorkshopModListItem> MoveDown;

	private void Start()
	{
		_selectButton.onClick.AddListener(delegate
		{
			WorkshopModListItem.Selected?.Invoke(this);
		});
		_upButton.onClick.AddListener(delegate
		{
			WorkshopModListItem.MoveUp?.Invoke(this);
		});
		_downButton.onClick.AddListener(delegate
		{
			WorkshopModListItem.MoveDown?.Invoke(this);
		});
		_enableToggle.onValueChanged.AddListener(OnEnabledToggleChanged);
	}

	private void OnEnabledToggleChanged(bool isOn)
	{
		if (Data is CoreModData)
		{
			_enableToggle.SetIsOnWithoutNotify(value: true);
			return;
		}
		Data.Enabled = isOn;
		SetInteractivity(isOn);
		SetFormatModName();
		WorkshopModListItem.Enabled?.Invoke(this);
	}

	public void SetData(ModData data)
	{
		Data = data;
		SetFormatModName();
		SetInteractivity(data.Enabled);
		_enableToggle.SetIsOnWithoutNotify(data.Enabled);
	}

	private void SetInteractivity(bool enabled)
	{
		_selectButton.interactable = enabled;
		_upButton.interactable = enabled;
		_downButton.interactable = enabled;
	}

	private void SetFormatModName()
	{
		ModAbout aboutData = Data.GetAboutData();
		bool isError = !aboutData.IsValid;
		Image icon = _icon;
		ModData data = Data;
		Sprite sprite = ((data is WorkshopModData) ? WorkshopMenu.Instance.SteamImage : ((data is LocalModData) ? WorkshopMenu.Instance.LocalImage : ((!(data is CoreModData)) ? null : WorkshopMenu.Instance.CoreImage)));
		icon.sprite = sprite;
		base.name = "~Mod_" + aboutData.Name;
		string modNameString = aboutData.Name;
		_title.text = ColorModName(modNameString, isError, Data.Enabled);
	}

	private static string ColorModName(string modNameString, bool isError, bool isEnabled)
	{
		if (!(!isError && isEnabled))
		{
			return "<color=" + (isError ? "red" : "#808080") + ">" + modNameString + "</color>";
		}
		return modNameString;
	}
}
