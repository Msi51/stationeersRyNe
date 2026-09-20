using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.UI.HelperHints.Extensions;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class StringManager
{
	private static readonly Dictionary<int, string> _strings = new Dictionary<int, string>();

	private static readonly string[] IntegerStrings = new string[131070];

	private static string[] _floatStrings;

	private static string[] _negativeFloatStrings;

	public static readonly float LargestFloat = 1000f;

	public static bool IsInitialized;

	public static StringBuilder ReusableStringBuilder = new StringBuilder();

	private static string TrueString = "True";

	private static string FalseString = "False";

	private static Dictionary<int, string> _outOfRangeInt = new Dictionary<int, string>();

	public static string Indent = "  ";

	public static int GetSize => _strings.Count;

	public static void WrapLineLength(StringBuilder stringBuilder, string inputString, int maxLineLength, string color = "")
	{
		string[] array = inputString.Split(' ');
		StringBuilder stringBuilder2 = new StringBuilder();
		if (!string.IsNullOrEmpty(color))
		{
			stringBuilder2.Append("<color=").Append(color).Append(">");
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (stringBuilder2.Length + text.Length < maxLineLength)
			{
				stringBuilder2.Append(text).Append(" ");
				continue;
			}
			if (stringBuilder2.Length > 0)
			{
				stringBuilder.AppendLine(stringBuilder2.ToString().TrimEnd());
				stringBuilder2.Clear();
			}
			stringBuilder2.Append(text).Append(" ");
		}
		stringBuilder.AppendLine(stringBuilder2.ToString().TrimEnd());
		if (!string.IsNullOrEmpty(color))
		{
			stringBuilder2.Append("</color>");
		}
	}

	public static void Initialize()
	{
		if (IsInitialized)
		{
			return;
		}
		for (int i = 0; i < IntegerStrings.Length; i++)
		{
			IntegerStrings[i] = (i - 65535).ToString();
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		float num = 0f;
		while (num < LargestFloat)
		{
			num = num.RoundToSignificantDigits(4);
			list.Add(num.ToString(CultureInfo.CurrentCulture));
			list2.Add((0f - num).ToString(CultureInfo.CurrentCulture));
			if (num < 10f)
			{
				num += 0.001f;
				continue;
			}
			if (num < 100f)
			{
				num += 0.01f;
				continue;
			}
			if (!(num <= 1000f))
			{
				break;
			}
			num += 0.1f;
		}
		_floatStrings = list.ToArray();
		_negativeFloatStrings = list2.ToArray();
		IsInitialized = true;
	}

	public static string Get(ArmControl value)
	{
		return value switch
		{
			ArmControl.Idle => GameStrings.ArmControlIdle.AsString(), 
			ArmControl.Plant => GameStrings.ArmControlPlant.AsString(), 
			ArmControl.Harvest => GameStrings.ArmControlHarvest.AsString(), 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	private static string GetPlural(int value, Assets.Scripts.Localization2.GameString plural, Assets.Scripts.Localization2.GameString singular)
	{
		if (value == 1)
		{
			return singular.AsString(Get(value));
		}
		return plural.AsString(Get(value));
	}

	public static string PositiveStatEffector(string text)
	{
		return ("+ " + text).AsColor("green");
	}

	public static string NegativeStatEffector(string text)
	{
		return ("- " + text).AsColor("red");
	}

	public static string GetFormattedTime(int seconds)
	{
		if (seconds < 86400)
		{
			if (seconds >= 60)
			{
				if (seconds < 3600)
				{
					return GetPlural(seconds / 60, GameStrings.UnitMinutes, GameStrings.UnitMinute);
				}
				return GetPlural(seconds / 3600, GameStrings.UnitHours, GameStrings.UnitHour);
			}
			return GetPlural(seconds, GameStrings.UnitSeconds, GameStrings.UnitSecond);
		}
		if (seconds < 2592000)
		{
			if (seconds < 604800)
			{
				return GetPlural(seconds / 86400, GameStrings.UnitDays, GameStrings.UnitDay);
			}
			return GetPlural(seconds / 604800, GameStrings.UnitWeeks, GameStrings.UnitWeek);
		}
		if (seconds < 31104000)
		{
			return GetPlural(seconds / 2592000, GameStrings.UnitMonths, GameStrings.UnitMonth);
		}
		return GetPlural(seconds / 31104000, GameStrings.UnitYears, GameStrings.UnitYear);
	}

	public static string Get(int value)
	{
		if (value < -65535 || value >= 65535)
		{
			return OutOfRangeIntegerString(value);
		}
		value += 65535;
		return IntegerStrings[value];
	}

	public static string Get(long value)
	{
		return Get((int)value);
	}

	public static string Get(float value)
	{
		value = SanitiseFloat(value);
		if (value < 0f - LargestFloat + 1f || value > LargestFloat - 1f)
		{
			return Get(Mathf.RoundToInt(value));
		}
		value = value.RoundToSignificantDigits(3);
		int num = -1;
		float num2 = Mathf.Abs(value);
		int num3 = ((num2 < 100f) ? ((!(num2 < 10f)) ? (Mathf.RoundToInt((num2 - 10f) * 100f) + 10000) : Mathf.RoundToInt(num2 * 1000f)) : ((!(num2 < 1000f)) ? num : (Mathf.RoundToInt((num2 - 100f) * 10f) + 10000 + 9000)));
		num = num3;
		if (!(value >= 0f))
		{
			return _negativeFloatStrings[num];
		}
		return _floatStrings[num];
	}

	private static float SanitiseFloat(float value)
	{
		if (float.IsNaN(value))
		{
			return 0f;
		}
		if (float.IsInfinity(value))
		{
			return 0f;
		}
		if (float.IsNegativeInfinity(value))
		{
			return 0f;
		}
		if (float.IsPositiveInfinity(value))
		{
			return 0f;
		}
		return value;
	}

	public static string Get(double value)
	{
		return Get((float)value);
	}

	public static string Get(bool value)
	{
		if (!value)
		{
			return FalseString;
		}
		return TrueString;
	}

	public static string GetGridString(Grid3 grid3)
	{
		return "(" + Get(grid3.x) + ", " + Get(grid3.y) + ", " + Get(grid3.z) + ")";
	}

	public static string Get(Vector3 vector3)
	{
		return "(" + Get(vector3.x) + ", " + Get(vector3.y) + ", " + Get(vector3.z) + ")";
	}

	public static string GetString(object value)
	{
		int hashCode = value.GetHashCode();
		if (_strings.TryGetValue(hashCode, out var value2))
		{
			return value2;
		}
		value2 = value.ToString();
		_strings[hashCode] = value2;
		if (GetSize % 10000 == 0)
		{
			Debug.LogWarning($"StringManager string cache count: {GetSize}");
		}
		return value2;
	}

	private static string OutOfRangeIntegerString(int value)
	{
		if (_outOfRangeInt.TryGetValue(value, out var value2))
		{
			return value2;
		}
		value2 = value.ToString();
		_outOfRangeInt.Add(value, value2);
		return value2;
	}

	public static void AddKeyValueLine(StringBuilder sb, Assets.Scripts.Localization2.GameString key, string value, bool indent = false)
	{
		AddKeyValue(sb, key, value, indent);
		sb.AppendLine();
	}

	public static void AddKeyValueLine(StringBuilder sb, Assets.Scripts.Localization2.GameString key, string value, string color, bool indent = false)
	{
		AddKeyValue(sb, key, value, color, indent);
		sb.AppendLine();
	}

	public static void AddKeyValue(StringBuilder sb, Assets.Scripts.Localization2.GameString key, string value, bool indent = false)
	{
		if (indent)
		{
			sb.Append(Indent);
		}
		sb.Append(key);
		sb.Append(" ");
		sb.Append(value);
	}

	public static void AddKeyValue(StringBuilder sb, Assets.Scripts.Localization2.GameString key, string value, string color, bool indent = false)
	{
		if (indent)
		{
			sb.Append(Indent);
		}
		sb.AppendColorText(color, key);
		sb.Append(" ");
		sb.Append(value);
	}
}
