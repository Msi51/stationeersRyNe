using System;
using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenCondition : ScreenDropdownBase
{
	[NonSerialized]
	public LogicCondition Parent;

	public Dropdown Operator;

	public Image IsTrueImage;

	public bool IsTrue = true;

	public Sprite TrueSprite;

	public Sprite FalseSprite;

	public Sprite UnknownSprite;

	public List<ConditionOperation> DisplayedOperators = new List<ConditionOperation>();

	private bool _isSetDisconnect;

	private int _foundIndex;

	protected override void Awake()
	{
		base.Awake();
		Operator.ReplaceRaycasters();
	}

	public void Initialize(LogicCondition parent, int index)
	{
		Parent = parent;
		RefreshAll();
	}

	public void SetConditionState()
	{
		if (Parent.IsDisconnected && !_isSetDisconnect)
		{
			IsTrueImage.sprite = UnknownSprite;
			IsTrueImage.color = Color.yellow;
			_isSetDisconnect = true;
		}
		if (!Parent.IsDisconnected && (IsTrue != Parent.IsTrue || _isSetDisconnect))
		{
			_isSetDisconnect = false;
			IsTrueImage.sprite = (Parent.IsTrue ? TrueSprite : FalseSprite);
			IsTrueImage.color = (Parent.IsTrue ? Color.green : Color.red);
			IsTrue = Parent.IsTrue;
		}
	}

	public override void RefreshAll()
	{
		PopulateTypes();
		PopulateOperators();
		PopulateValue();
	}

	public override void OnDeviceChanged()
	{
		RefreshAll();
	}

	public override void OnTypeChanged()
	{
		PopulateOperators();
		PopulateValue();
	}

	public void OnOperatorChanged()
	{
		PopulateValue();
	}

	private void AddOperator(ConditionOperation operatorType, bool isText = false)
	{
		DisplayedOperators.Add(operatorType);
		ScreenDropdownBase.OptionData.Add(new Dropdown.OptionData
		{
			text = OperationToString(operatorType, isText)
		});
		if (Parent.Operation == operatorType)
		{
			_foundIndex = ScreenDropdownBase.OptionData.Count - 1;
		}
	}

	private void PopulateOperators()
	{
		Operator.ClearOptions();
		if (!Parent.Device || Parent.Type == LogicType.None)
		{
			Parent.Operation = ConditionOperation.Equals;
			Operator.interactable = false;
			return;
		}
		ScreenDropdownBase.OptionData.Clear();
		DisplayedOperators.Clear();
		_foundIndex = -1;
		switch (Parent.Type)
		{
		case LogicType.Quantity:
		case LogicType.Color:
			AddOperator(ConditionOperation.Equals);
			AddOperator(ConditionOperation.NotEquals);
			AddOperator(ConditionOperation.Greater);
			AddOperator(ConditionOperation.Less);
			break;
		case LogicType.Mode:
			if (Parent.Device.ModeStrings.Length > 2)
			{
				AddOperator(ConditionOperation.Equals);
				AddOperator(ConditionOperation.NotEquals);
				AddOperator(ConditionOperation.Greater);
				AddOperator(ConditionOperation.Less);
			}
			else
			{
				AddOperator(ConditionOperation.Equals, isText: true);
				AddOperator(ConditionOperation.NotEquals, isText: true);
			}
			break;
		case LogicType.Power:
		case LogicType.Open:
		case LogicType.Error:
		case LogicType.Activate:
		case LogicType.Lock:
		case LogicType.RecipeHash:
			AddOperator(ConditionOperation.Equals, isText: true);
			AddOperator(ConditionOperation.NotEquals, isText: true);
			break;
		default:
			AddOperator(ConditionOperation.Equals);
			AddOperator(ConditionOperation.NotEquals);
			AddOperator(ConditionOperation.Greater);
			AddOperator(ConditionOperation.Less);
			break;
		}
		Operator.AddOptions(ScreenDropdownBase.OptionData);
		if (ScreenDropdownBase.OptionData.Count == 0)
		{
			Operator.SetValueWithoutNotify(0);
			Operator.interactable = false;
			Parent.Operation = ConditionOperation.Equals;
			return;
		}
		if (_foundIndex < 0)
		{
			Debug.Log("resetting as not found operator");
			Operator.SetValueWithoutNotify(0);
		}
		else
		{
			Operator.SetValueWithoutNotify(_foundIndex);
		}
		Operator.interactable = true;
	}

	private void PopulateTypes()
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
			if (Parent.Device.CanLogicRead(logicType))
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
			Type.value = 0;
			Parent.Type = (LogicType)Enum.Parse(typeof(LogicType), Type.options[0].text);
		}
		else
		{
			Type.SetValueWithoutNotify(num);
		}
	}

	private void PopulateValue()
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

	private static string OperationToString(ConditionOperation operation, bool isText)
	{
		switch (operation)
		{
		case ConditionOperation.Equals:
			if (!isText)
			{
				return OperatorStrings.EqualsString;
			}
			return OperatorStrings.IsString;
		case ConditionOperation.NotEquals:
			if (!isText)
			{
				return OperatorStrings.NotEqualsString;
			}
			return OperatorStrings.NotString;
		case ConditionOperation.Greater:
			return OperatorStrings.GreaterString;
		case ConditionOperation.Less:
			return OperatorStrings.LessString;
		default:
			return OperatorStrings.UnknownString;
		}
	}
}
