using Assets.Scripts.Serialization;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SettingItem : UserInterfaceBase
{
	public SettingType SettingType;

	public Selectable Selectable;

	public UnityAction OnValueChanged;

	public SetSliderValueTMP SetSliderValue;

	public void Setup()
	{
		AddListener(Selectable as TMP_Dropdown);
		AddListener(Selectable as Toggle);
		AddListener(Selectable as Slider);
		AddListener(Selectable as TMP_InputField);
	}

	private void AddListener(TMP_InputField inputField)
	{
		if ((bool)inputField)
		{
			UnityAction<string> call = delegate
			{
				Settings.OnValueChanged(SettingType);
			};
			inputField.onEndEdit.AddListener(call);
		}
	}

	private void AddListener(Slider slider)
	{
		if (!slider)
		{
			return;
		}
		UnityAction<float> call = delegate
		{
			Settings.OnValueChanged(SettingType);
		};
		slider.onValueChanged.AddListener(call);
		if ((bool)SetSliderValue)
		{
			UnityAction<float> call2 = delegate
			{
				SetSliderValue.OnChangedValue();
			};
			slider.onValueChanged.AddListener(call2);
		}
	}

	private void AddListener(Toggle toggle)
	{
		if ((bool)toggle)
		{
			UnityAction<bool> call = delegate
			{
				Settings.OnValueChanged(SettingType);
			};
			toggle.onValueChanged.AddListener(call);
		}
	}

	private void AddListener(TMP_Dropdown dropDown)
	{
		if ((bool)dropDown)
		{
			UnityAction<int> call = delegate
			{
				Settings.OnValueChanged(SettingType);
			};
			dropDown.onValueChanged.AddListener(call);
		}
	}

	public void OnSettingChanged()
	{
		Settings.OnValueChanged(SettingType);
	}

	public void Initialize()
	{
		LocalizedText[] componentsInChildren = GetComponentsInChildren<LocalizedText>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Initialize();
		}
	}
}
