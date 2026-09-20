using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Genetics;

public class ValueDisplay : MonoBehaviour
{
	public enum Unit
	{
		None,
		Time,
		RatioToPercentage,
		PressureKpa,
		TemperatureKToC,
		Percentage,
		MolesPerHour,
		TemperatureC
	}

	[SerializeField]
	private RectTransform _bar;

	[SerializeField]
	private RectTransform _rootTransform;

	[SerializeField]
	private TextMeshProUGUI _leftText;

	[SerializeField]
	private TextMeshProUGUI _rightText;

	[SerializeField]
	private TextMeshProUGUI _centerText;

	public void SetValue(float baseValue, float value, float min, float max, Unit unit)
	{
		float num = _rootTransform.sizeDelta.x - 2f;
		_bar.offsetMin = new Vector2(num / 2f, 0f);
		_leftText.text = GetUnitValue(min, unit);
		_rightText.text = GetUnitValue(max, unit);
		_centerText.text = GetUnitValue(value, unit);
		float value2 = 0f;
		if (min != max)
		{
			value2 = ((!(value > baseValue)) ? ((0f - (baseValue - value)) / (baseValue - min)) : ((value - baseValue) / (max - baseValue)));
		}
		_bar.localScale = new Vector3(Mathf.Clamp(value2, -1f, 1f), 1f, 1f);
	}

	public static string GetUnitValue(float value, Unit unit)
	{
		switch (unit)
		{
		case Unit.Time:
			if (value < 60f)
			{
				return StringManager.Get(value) + "s";
			}
			if (value < 3600f)
			{
				return StringManager.Get(Mathf.Floor(value / 60f)) + "min " + StringManager.Get(Mathf.Floor(value % 60f)) + "s";
			}
			if (value >= 3600f)
			{
				float num = Mathf.Floor(value / 3600f);
				float value2 = Mathf.Floor((value - num * 3600f) / 60f);
				return StringManager.Get(num) + "h " + StringManager.Get(value2) + "min " + StringManager.Get(Mathf.Floor(value % 60f)) + "s";
			}
			break;
		case Unit.RatioToPercentage:
			return StringManager.Get(Mathf.RoundToInt(value * 100f)) + "%";
		case Unit.PressureKpa:
			return StringManager.Get(value.RoundToSignificantDigits(2)) + "kPa";
		case Unit.TemperatureKToC:
			return StringManager.Get(Mathf.RoundToInt(value - Chemistry.Temperature.ZeroDegrees.ToFloat())) + "°C";
		case Unit.TemperatureC:
			return StringManager.Get(value) + "°C";
		case Unit.Percentage:
			return StringManager.Get(Mathf.RoundToInt(value)) + "%";
		case Unit.MolesPerHour:
			return GameStrings.MolesPerHour.AsString(StringManager.Get(value * 120f * 60f));
		}
		return StringManager.Get(value);
	}
}
