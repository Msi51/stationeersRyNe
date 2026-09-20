using Assets.Scripts.Util;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class StateInstanceOld
{
	private int _currentValue;

	public Text Text;

	public Unit Unit;

	public StateInstanceOld(Text text, Unit unit = Unit.None)
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
		return true;
	}

	public bool UpdateText(float value)
	{
		return UpdateText((int)value);
	}
}
