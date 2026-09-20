using System;
using System.Globalization;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenAction : ScreenDropdownBase
{
	[NonSerialized]
	public LogicAction Parent;

	public void Initialize(LogicAction parent, int index)
	{
		Parent = parent;
		RefreshAll();
	}

	public override void RefreshAll()
	{
		PopulateTypes();
		PopulateValue();
	}

	public override void OnDeviceChanged()
	{
		PopulateTypes();
		PopulateValue();
	}

	public override void OnTypeChanged()
	{
		PopulateValue();
	}

	public void PopulateTypes()
	{
		Type.ClearOptions();
		if (!Parent.Device)
		{
			Parent.Type = LogicType.None;
			Type.interactable = false;
			return;
		}
		ScreenDropdownBase.OptionData.Clear();
		int num = -1;
		int num2 = 0;
		LogicType[] logicTypes = ScreenDropdownBase.LogicTypes;
		for (int i = 0; i < logicTypes.Length; i++)
		{
			LogicType logicType = logicTypes[i];
			if (Parent.Device.CanLogicWrite(logicType))
			{
				if (logicType == Parent.Type)
				{
					num = num2;
				}
				ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
				{
					text = logicType.ToString()
				});
				num2++;
			}
		}
		if (ScreenDropdownBase.OptionData.Count == 0)
		{
			Type.SetValueWithoutNotify(0);
			Type.interactable = false;
			Parent.Type = LogicType.None;
			return;
		}
		Type.interactable = true;
		Type.AddOptions(ScreenDropdownBase.OptionData);
		if (num < 0)
		{
			Type.SetValueWithoutNotify(0);
			Parent.Type = (LogicType)Enum.Parse(typeof(LogicType), Type.options[0].text);
		}
		else
		{
			Type.SetValueWithoutNotify(num);
		}
	}

	public void PopulateValue()
	{
		Value.ClearOptions();
		if (!Parent.Device)
		{
			Value.interactable = false;
			return;
		}
		ScreenDropdownBase.OptionData.Clear();
		switch (Parent.Type)
		{
		case LogicType.None:
			Value.interactable = false;
			break;
		case LogicType.Power:
		case LogicType.Activate:
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = ActionStrings.Off
			});
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = ActionStrings.On
			});
			Value.AddOptions(ScreenDropdownBase.OptionData);
			Value.SetValueWithoutNotify((int)Parent.Value);
			break;
		case LogicType.Open:
		case LogicType.Error:
		case LogicType.Lock:
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = ActionStrings.False
			});
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = ActionStrings.True
			});
			Value.AddOptions(ScreenDropdownBase.OptionData);
			Value.SetValueWithoutNotify((int)Parent.Value);
			break;
		case LogicType.Mode:
		{
			string[] colorStrings = Parent.Device.ModeStrings;
			foreach (string text2 in colorStrings)
			{
				ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
				{
					text = text2
				});
			}
			Value.AddOptions(ScreenDropdownBase.OptionData);
			Value.SetValueWithoutNotify((int)Parent.Value);
			break;
		}
		case LogicType.Color:
		{
			string[] colorStrings = GameManager.ColorStrings;
			foreach (string text in colorStrings)
			{
				ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
				{
					text = text
				});
			}
			Value.AddOptions(ScreenDropdownBase.OptionData);
			Value.SetValueWithoutNotify(GameManager.LogicDropdownFromColorIndex((int)Parent.Value));
			break;
		}
		default:
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = Parent.Value.ToString(CultureInfo.InvariantCulture)
			});
			ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
			{
				text = ScreenDropdownBase.EnterNewValue
			});
			Value.AddOptions(ScreenDropdownBase.OptionData);
			Value.SetValueWithoutNotify(0);
			break;
		}
		if (ScreenDropdownBase.OptionData.Count == 0)
		{
			Value.SetValueWithoutNotify(0);
			Value.interactable = false;
			Parent.Value = 0.0;
		}
		else
		{
			Value.interactable = true;
		}
	}
}
