using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class StateInstance
{
	public delegate void Event();

	public delegate Color ColorEvent();

	private int _currentValue;

	private string _currentValueString = "";

	public TextMeshProUGUI Text;

	public Unit Unit;

	public event Event OnChanged;

	public event ColorEvent OnChangedColor;

	public StateInstance(TextMeshProUGUI text, Unit unit = Unit.None)
	{
		Text = text;
		Unit = unit;
		Text.text = StringGenerator.GetString(_currentValue, Unit);
	}

	public bool UpdateText(int value)
	{
		if (value == _currentValue)
		{
			return false;
		}
		_currentValue = value;
		Text.text = StringGenerator.GetString(_currentValue, Unit);
		ChangeEvents();
		return true;
	}

	public void UpdateText(string value)
	{
		if (!(value == _currentValueString))
		{
			_currentValueString = value;
			Text.text = _currentValueString;
			ChangeEvents();
		}
	}

	private void SetColor(Color color)
	{
		Text.color = color;
	}

	public void UpdateText(float value)
	{
		UpdateText((int)value);
	}

	public void ChangeEvents()
	{
		if (this.OnChanged != null)
		{
			this.OnChanged();
		}
		if (this.OnChangedColor != null)
		{
			SetColor(this.OnChangedColor());
		}
	}
}
