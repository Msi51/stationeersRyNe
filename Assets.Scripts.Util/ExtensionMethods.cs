using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Util;

public static class ExtensionMethods
{
	private static string _g = "G";

	private static string _f0 = "F0";

	private static string _f1 = "F1";

	private static string _f2 = "F2";

	private static string _f3 = "F3";

	private static string _f4 = "F4";

	private static string _f5 = "F5";

	private static string _f6 = "F6";

	private static string _f7 = "F7";

	private static string _f8 = "F8";

	private static string _f9 = "F9";

	private static string _zero = "0";

	private static string _tera = "{0:F2} T{1}";

	private static string _giga = "{0:F2} G{1}";

	private static string _mega = "{0:F2} M{1}";

	private static string _kilo = "{0:F2} k{1}";

	private static string _mili = "{0} m{1}";

	private static string _centi = "{0} c{1}";

	private static string _micro = "{0} μ{1}";

	private static string _normal = "{0} {1}";

	private static string _teraColor = "<color={2}>{0:F2} T{1}</color>";

	private static string _gigaColor = "<color={2}>{0:F2} G{1}</color>";

	private static string _megaColor = "<color={2}>{0:F2} M{1}</color>";

	private static string _kiloColor = "<color={2}>{0:F2} k{1}</color>";

	private static string _miliColor = "<color={2}>{0} m{1}</color>";

	private static string _microColor = "<color={2}>{0} μ{1}</color>";

	private static string _centiColor = "<color={2}>{0} c{1}</color>";

	private static string _normalColor = "<color={2}>{0} {1}</color>";

	private static string _percentColor = "<color={1}>{0}%</color>";

	private static string _percent = "{0}%";

	private static string _stringColor = "<color={1}>{0}</color>";

	private static System.Random _random = new System.Random();

	private static double _radianConvert = 180.0 / Math.PI;

	private static readonly Dictionary<(string unit, string unitPlural, int tenths), string> timeUnitCache = new Dictionary<(string, string, int), string>();

	private static readonly StringBuilder timeUnitBuilder = new StringBuilder(32);

	private const int TimeUnitCacheLimit = 2048;

	private static Dictionary<Type, Array> enumValuesLookup = new Dictionary<Type, Array>();

	private static Dictionary<Type, int[]> enumValuesSortedRedirector = new Dictionary<Type, int[]>();

	public static bool HasParameter(this Animator animator, int paramName)
	{
		if (animator == null)
		{
			return false;
		}
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].nameHash == paramName)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasParameter(this Animator animator, string paramName)
	{
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == paramName)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<string> SplitBy(this string str, int chunkLength)
	{
		if (string.IsNullOrEmpty(str))
		{
			throw new ArgumentException();
		}
		if (chunkLength < 1)
		{
			throw new ArgumentException();
		}
		for (int i = 0; i < str.Length; i += chunkLength)
		{
			if (chunkLength + i > str.Length)
			{
				chunkLength = str.Length - i;
			}
			yield return str.Substring(i, chunkLength);
		}
	}

	public static string ToStringRounded(this float value)
	{
		if (RocketMath.Approximately(value, 0f))
		{
			return _zero;
		}
		float num = Mathf.Abs(value);
		if (num < 0.01f)
		{
			return value.ToString(_f3, CultureInfo.CurrentCulture);
		}
		if (num < 0.1f)
		{
			return value.ToString(_f2, CultureInfo.CurrentCulture);
		}
		if (num < 1f)
		{
			return value.ToString(_f1, CultureInfo.CurrentCulture);
		}
		return value.ToString(_f0, CultureInfo.CurrentCulture);
	}

	public static string ToStringRounded(this float value, string color)
	{
		return string.Format("<color={1}>{0}</color>", value.ToStringRounded(), color);
	}

	public static string ToStringRounded(this int value, string color)
	{
		return string.Format("<color={1}>{0}</color>", ToStringRounded(value), color);
	}

	public static string ToStringDisplay(this float value, float maxValue, int eDigits = 2)
	{
		if (RocketMath.Approximately(value, 0f))
		{
			return _zero;
		}
		float num = Mathf.Abs(value);
		if (num < 0.01f || num > maxValue)
		{
			return value.ToString($"G{eDigits}");
		}
		if (num < 0.1f)
		{
			return value.ToString(_f2);
		}
		return value.ToString(_g);
	}

	public static string ToStringDisplay(this double value, double maxValue, int eDigits = 2)
	{
		if (RocketMath.Approximately(value, 0.0))
		{
			return _zero;
		}
		double num = Math.Abs(value);
		if (num < 0.009999999776482582 || num > maxValue)
		{
			return value.ToString($"G{eDigits}");
		}
		if (num < 0.10000000149011612)
		{
			return value.ToString(_f2);
		}
		string text = value.ToString(_g);
		int num2 = (int)(Math.Floor(Math.Log10(maxValue)) + 1.0);
		if (text.Length > num2)
		{
			text = text.Substring(0, num2);
		}
		return text;
	}

	public static Vector3d ToVector3d(this Vector3 vector3)
	{
		return new Vector3d(vector3.x, vector3.y, vector3.z);
	}

	public static Vector3 ToVector3(this Vector3d vector3)
	{
		return new Vector3((float)vector3.x, (float)vector3.y, (float)vector3.z);
	}

	public static string ToStringRounded(this double value)
	{
		if (Math.Abs(value) < 1.401298464324817E-45)
		{
			return _zero;
		}
		double num = Math.Abs(value) - (double)(int)Math.Abs(value);
		if (num < 1.401298464324817E-45)
		{
			return value.ToString(_f0, CultureInfo.CurrentCulture);
		}
		if (num < 0.009999999776482582)
		{
			return value.ToString(_f3, CultureInfo.CurrentCulture);
		}
		if (num < 0.10000000149011612)
		{
			return value.ToString(_f2, CultureInfo.CurrentCulture);
		}
		if (num < 1.0)
		{
			return value.ToString(_f1, CultureInfo.CurrentCulture);
		}
		return value.ToString(_f0, CultureInfo.CurrentCulture);
	}

	public static string ToStringPrefix(this int value, string unit = "")
	{
		int num = Mathf.Abs(value);
		if ((float)num >= 1E+09f)
		{
			return string.Format(_giga, StringManager.Get((float)value / 1E+09f), unit);
		}
		if ((float)num >= 1000000f)
		{
			return string.Format(_mega, StringManager.Get((float)value / 1000000f), unit);
		}
		if ((float)num >= 1000f)
		{
			return string.Format(_kilo, StringManager.Get((float)value / 1000f), unit);
		}
		return string.Format(_normal, StringManager.Get(value), unit);
	}

	public static string ToStringPrefix(this float value, string unit = "")
	{
		float num = Mathf.Abs(value);
		if (num >= 1E+12f)
		{
			return string.Format(_tera, StringManager.Get(value / 1E+12f), unit);
		}
		if (num >= 1E+09f)
		{
			return string.Format(_giga, StringManager.Get(value / 1E+09f), unit);
		}
		if (num >= 1000000f)
		{
			return string.Format(_mega, StringManager.Get(value / 1000000f), unit);
		}
		if (num >= 1000f)
		{
			return string.Format(_kilo, StringManager.Get(value / 1000f), unit);
		}
		if (RocketMath.Approximately(num, 0f))
		{
			return string.Format(_normal, _zero, unit);
		}
		if (num <= 0.001f)
		{
			return string.Format(_mili, StringManager.Get(value / 0.001f), unit);
		}
		if (num <= 1E-06f)
		{
			return string.Format(_micro, StringManager.Get(value / 1E-06f), unit);
		}
		return string.Format(_normal, StringManager.Get(value), unit);
	}

	public static bool Contains(this Span<ConnectionRef> span, Connection connection)
	{
		Span<ConnectionRef> span2 = span;
		for (int i = 0; i < span2.Length; i++)
		{
			ConnectionRef connectionRef = span2[i];
			if (connectionRef.Matches(connection))
			{
				return true;
			}
		}
		return false;
	}

	public static void FillCharArray(this double value, char[] buffer, double maxValue, int eDigits = 2)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		ReadOnlySpan<char> format = _f2.AsSpan();
		ReadOnlySpan<char> format2 = _g.AsSpan();
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		Span<char> destination = buffer;
		int charsWritten;
		if (RocketMath.Approximately(value, 0.0))
		{
			destination[0] = '0';
			charsWritten = 1;
		}
		else
		{
			double num = Math.Abs(value);
			if (num < 0.01 || num > maxValue)
			{
				Span<char> span = stackalloc char[2];
				span[0] = 'G';
				span[1] = (char)(48 + eDigits);
				value.TryFormat(destination, out charsWritten, span, CultureInfo.InvariantCulture);
			}
			else if (num < 0.1)
			{
				value.TryFormat(destination, out charsWritten, format, CultureInfo.InvariantCulture);
			}
			else
			{
				value.TryFormat(destination, out charsWritten, format2, CultureInfo.InvariantCulture);
				int num2 = (int)(Math.Floor(Math.Log10(maxValue)) + 1.0);
				if (charsWritten > num2)
				{
					charsWritten = num2;
				}
			}
		}
		for (int i = charsWritten; i < destination.Length; i++)
		{
			destination[i] = '\0';
		}
	}

	public static void FillCharArray(this double value, char[] charArray, string unit = "")
	{
		if (charArray == null)
		{
			throw new ArgumentNullException("charArray");
		}
		Span<char> destination = charArray;
		double num = Math.Abs(value);
		ReadOnlySpan<char> format = destination.Length switch
		{
			0 => ReadOnlySpan<char>.Empty, 
			1 => _f0.AsSpan(), 
			2 => _f1.AsSpan(), 
			3 => _f2.AsSpan(), 
			4 => _f3.AsSpan(), 
			5 => _f4.AsSpan(), 
			6 => _f5.AsSpan(), 
			7 => _f6.AsSpan(), 
			8 => _f7.AsSpan(), 
			_ => _f8.AsSpan(), 
		};
		double num2;
		char c;
		if (num >= 1000000.0)
		{
			if (!(num >= 999999995904.0))
			{
				if (num >= 1000000000.0)
				{
					num2 = value / 1000000000.0;
					c = 'G';
					format = _f2.AsSpan();
				}
				else
				{
					num2 = value / 1000000.0;
					c = 'M';
					format = _f2.AsSpan();
				}
			}
			else
			{
				num2 = value / 999999995904.0;
				c = 'T';
				format = _f2.AsSpan();
			}
		}
		else if (num >= 1000.0)
		{
			num2 = value / 1000.0;
			c = 'k';
			format = _f2.AsSpan();
		}
		else if (RocketMath.Approximately(num, 0.0))
		{
			num2 = 0.0;
			c = '\0';
		}
		else if (!(num >= 0.0010000000474974513))
		{
			if (num >= 9.999999974752427E-07)
			{
				num2 = value / 9.999999974752427E-07;
				c = 'µ';
				format = _f3.AsSpan();
			}
			else
			{
				num2 = value / 9.999999974752428E-10;
				c = 'n';
				format = _f3.AsSpan();
			}
		}
		else
		{
			num2 = value / 0.0010000000474974513;
			c = 'm';
			format = _f3.AsSpan();
		}
		if (!num2.TryFormat(destination, out var charsWritten, format, CultureInfo.InvariantCulture))
		{
			charsWritten = destination.Length - 1;
		}
		if (c != 0 || !string.IsNullOrEmpty(unit))
		{
			if (charsWritten < destination.Length)
			{
				destination[charsWritten++] = ' ';
			}
			destination[charsWritten++] = c;
			foreach (char c2 in unit)
			{
				if (charsWritten >= destination.Length)
				{
					break;
				}
				destination[charsWritten++] = c2;
			}
		}
		for (int j = charsWritten; j < destination.Length; j++)
		{
			destination[j] = '\0';
		}
	}

	public static string ToStringPrefix(this double value, string unit = "")
	{
		double num = Math.Abs(value);
		if (num >= 999999995904.0)
		{
			return string.Format(_tera, StringManager.Get(value / 999999995904.0), unit);
		}
		if (num >= 1000000000.0)
		{
			return string.Format(_giga, StringManager.Get(value / 1000000000.0), unit);
		}
		if (num >= 1000000.0)
		{
			return string.Format(_mega, StringManager.Get(value / 1000000.0), unit);
		}
		if (num >= 1000.0)
		{
			return string.Format(_kilo, StringManager.Get(value / 1000.0), unit);
		}
		if (RocketMath.Approximately(num, 0.0))
		{
			return string.Format(_normal, _zero, unit);
		}
		if (num <= 0.0010000000474974513)
		{
			return string.Format(_mili, StringManager.Get(value / 0.0010000000474974513), unit);
		}
		if (num <= 9.999999974752427E-07)
		{
			return string.Format(_micro, StringManager.Get(value / 9.999999974752427E-07), unit);
		}
		return string.Format(_normal, StringManager.Get(value), unit);
	}

	public static string ToStringSimple(this double value, string unit = "")
	{
		double num = Math.Abs(value);
		if (RocketMath.Approximately(num, 0.0))
		{
			return string.Format(_normal, _zero, unit);
		}
		if (num <= 0.0010000000474974513)
		{
			return string.Format(_mili, StringManager.Get(value / 0.0010000000474974513), unit);
		}
		if (num <= 9.999999974752427E-07)
		{
			return string.Format(_micro, StringManager.Get(value / 9.999999974752427E-07), unit);
		}
		return string.Format(_normal, StringManager.Get(value), unit);
	}

	public static string ToString(this string value, string color = "")
	{
		return string.Format("<color={1}>{0}</color>", value, color);
	}

	public static string ToString(this float value, string color = "")
	{
		return "<color=" + color + ">" + StringManager.Get(value) + "</color>";
	}

	public static string ToStringExact(this float value)
	{
		return value.ToString("0." + new string('#', 8), CultureInfo.CurrentCulture);
	}

	public static string ToStringExact(this double value)
	{
		return value.ToString("0." + new string('#', 339), CultureInfo.CurrentCulture);
	}

	public static string ToStringPrefix(this float value, string unit, string color, bool adaptive = true)
	{
		float num = Mathf.Abs(value);
		if (!adaptive)
		{
			if ((double)num <= 0.001)
			{
				return string.Format(_normalColor, _zero, unit, color);
			}
			return string.Format(_normalColor, value.ToStringRounded(), unit, color);
		}
		if (num >= 1E+12f)
		{
			return string.Format(_teraColor, StringManager.Get(value / 1E+12f), unit, color);
		}
		if (num >= 1E+09f)
		{
			return string.Format(_gigaColor, StringManager.Get(value / 1E+09f), unit, color);
		}
		if (num >= 1000000f)
		{
			return string.Format(_megaColor, StringManager.Get(value / 1000000f), unit, color);
		}
		if (num >= 1000f)
		{
			return string.Format(_kiloColor, StringManager.Get(value / 1000f), unit, color);
		}
		if (num < float.Epsilon)
		{
			return string.Format(_normalColor, _zero, unit, color);
		}
		if (num <= 0.001f)
		{
			return string.Format(_miliColor, (value / 0.001f).ToStringRounded(), unit, color);
		}
		if (num <= 1E-06f)
		{
			return string.Format(_microColor, (value / 1E-06f).ToStringRounded(), unit, color);
		}
		return string.Format(_normalColor, value.ToStringRounded(), unit, color);
	}

	public static void AppendPrefix(this float value, StringBuilder sb, string unit, string color, bool adaptive = true)
	{
		float num = Mathf.Abs(value);
		if (!adaptive)
		{
			sb.AppendFormat(_normalColor, ((double)num <= 0.001) ? _zero : StringManager.Get(value), unit, color);
		}
		else if (num >= 1E+12f)
		{
			sb.AppendFormat(_teraColor, StringManager.Get(value / 1E+12f), unit, color);
		}
		else if (num >= 1E+09f)
		{
			sb.AppendFormat(_gigaColor, StringManager.Get(value / 1E+09f), unit, color);
		}
		else if (num >= 1000000f)
		{
			sb.AppendFormat(_megaColor, StringManager.Get(value / 1000000f), unit, color);
		}
		else if (num >= 1000f)
		{
			sb.AppendFormat(_kiloColor, StringManager.Get(value / 1000f), unit, color);
		}
		else if (num < float.Epsilon)
		{
			sb.AppendFormat(_normalColor, _zero, unit, color);
		}
		else if (num <= 0.001f)
		{
			sb.AppendFormat(_miliColor, StringManager.Get(value / 0.001f), unit, color);
		}
		else if (num <= 1E-06f)
		{
			sb.AppendFormat(_microColor, StringManager.Get(value / 1E-06f), unit, color);
		}
		else
		{
			sb.AppendFormat(_normalColor, StringManager.Get(value), unit, color);
		}
	}

	public static string AsColor(this string value, string color)
	{
		return string.Format(_stringColor, value, color);
	}

	public static void AppendColor(this string value, StringBuilder sb, string color)
	{
		sb.AppendFormat(_stringColor, value, color);
	}

	public static string ToStringPercent(this float value)
	{
		return string.Format(_percent, value);
	}

	public static void AppendPercent(this float value, StringBuilder sb)
	{
		sb.AppendFormat(_percent, StringManager.Get(value));
	}

	public static string ToStringPercent(this float value, string color)
	{
		return string.Format(_percentColor, value.ToStringRounded(), color);
	}

	public static void AppendPercent(this float value, StringBuilder sb, string color)
	{
		sb.AppendFormat(_percentColor, StringManager.Get(value), color);
	}

	public static string ToStringPercent(this int value)
	{
		return string.Format(_percent, value);
	}

	public static void AppendPercent(this int value, StringBuilder sb)
	{
		sb.AppendFormat(_percent, StringManager.Get(value));
	}

	public static string ToStringPercent(this int value, string color)
	{
		return string.Format(_percentColor, value, color);
	}

	public static void AppendPercent(this int value, StringBuilder sb, string color)
	{
		sb.AppendFormat(_percentColor, StringManager.Get(value), color);
	}

	public static string ToStringPrefix(this int value, string unit, string color)
	{
		int num = Mathf.Abs(value);
		if ((float)num >= 1E+09f)
		{
			return string.Format(_gigaColor, StringManager.Get((float)value / 1E+09f), unit, color);
		}
		if ((float)num >= 1000000f)
		{
			return string.Format(_megaColor, StringManager.Get((float)value / 1000000f), unit, color);
		}
		if ((float)num >= 1000f)
		{
			return string.Format(_kiloColor, StringManager.Get((float)value / 1000f), unit, color);
		}
		return string.Format(_normalColor, StringManager.Get(value), unit, color);
	}

	public static void AppendPrefix(this int value, StringBuilder sb, string unit, string color)
	{
		int num = Mathf.Abs(value);
		if ((float)num >= 1E+09f)
		{
			sb.AppendFormat(_gigaColor, StringManager.Get((float)value / 1E+09f), unit, color);
		}
		else if ((float)num >= 1000000f)
		{
			sb.AppendFormat(_megaColor, StringManager.Get((float)value / 1000000f), unit, color);
		}
		else if ((float)num >= 1000f)
		{
			sb.AppendFormat(_kiloColor, StringManager.Get((float)value / 1000f), unit, color);
		}
		else
		{
			sb.AppendFormat(_normalColor, StringManager.Get(value), unit, color);
		}
	}

	public static string ToStringPrefix(this double value, string unit, string color, bool adaptive = true)
	{
		double num = Math.Abs(value);
		if (!adaptive)
		{
			if (num <= 0.001)
			{
				return string.Format(_normalColor, _zero, unit, color);
			}
			return string.Format(_normalColor, value.ToStringRounded(), unit, color);
		}
		if (num >= 999999995904.0)
		{
			return string.Format(_teraColor, StringManager.Get(value / 999999995904.0), unit, color);
		}
		if (num >= 1000000000.0)
		{
			return string.Format(_gigaColor, StringManager.Get(value / 1000000000.0), unit, color);
		}
		if (num >= 1000000.0)
		{
			return string.Format(_megaColor, StringManager.Get(value / 1000000.0), unit, color);
		}
		if (num >= 1000.0)
		{
			return string.Format(_kiloColor, StringManager.Get(value / 1000.0), unit, color);
		}
		if (num < 1.401298464324817E-45)
		{
			return string.Format(_normalColor, _zero, unit, color);
		}
		if (num <= 0.0010000000474974513)
		{
			return string.Format(_miliColor, (value / 0.0010000000474974513).ToStringRounded(), unit, color);
		}
		if (num <= 9.999999974752427E-07)
		{
			return string.Format(_microColor, (value / 9.999999974752427E-07).ToStringRounded(), unit, color);
		}
		return string.Format(_normalColor, value.ToStringRounded(), unit, color);
	}

	public static string ToProper(this string value, bool withSpaces = true)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (withSpaces)
		{
			return Regex.Replace(value, "(?!^)[A-Z]", " $0");
		}
		return Regex.Replace(value, "(?!^)[A-Z]", "$0");
	}

	public static string StripRtf(this string value)
	{
		return Regex.Replace(value, "\\<.*?\\>", string.Empty);
	}

	public static int RemoveAll<T>(this ICollection<T> collection, Predicate<T> match)
	{
		if (match == null)
		{
			return 0;
		}
		Collection<T> collection2 = new Collection<T>();
		foreach (T item in collection)
		{
			if (match(item))
			{
				collection2.Add(item);
			}
		}
		foreach (T item2 in collection2)
		{
			collection.Remove(item2);
		}
		return collection2.Count;
	}

	public static int AddRange<T>(this ICollection<T> collection, ICollection<T> toAdd)
	{
		if (toAdd == null)
		{
			return 0;
		}
		int num = 0;
		foreach (T item in toAdd)
		{
			collection.Add(item);
			num++;
		}
		return num;
	}

	public static string ToDelimitedString<T>(this IEnumerable<T> lst, string separator = ", ")
	{
		return lst.ToDelimitedString((T p) => p, separator);
	}

	public static string ToDelimitedString<S, T>(this IEnumerable<S> lst, Func<S, T> selector, string separator = ", ")
	{
		return string.Join(separator, Array.ConvertAll(lst.ToArray(), (S x) => x.ToString()));
	}

	public static Color SetAlpha(this Color color, float alpha)
	{
		Color result = color;
		result.a = alpha;
		return result;
	}

	public static bool CompareWith(this float[] array1, float[] array2)
	{
		if (array1.Length != array2.Length)
		{
			return false;
		}
		for (int i = 0; i < array1.Length; i++)
		{
			if (Math.Abs(array1[i] - array2[i]) > Mathf.Epsilon)
			{
				return false;
			}
		}
		return true;
	}

	public static float[] ToFloat(this Color color)
	{
		return new float[3] { color.r, color.g, color.b };
	}

	public static T First<T>(this IList<T> list)
	{
		if (list.Count != 0)
		{
			return list[0];
		}
		return default(T);
	}

	public static T Last<T>(this IList<T> list)
	{
		if (list.Count != 0)
		{
			return list[list.Count - 1];
		}
		return default(T);
	}

	public static bool ValidIndex<T>(this T[] array, int index)
	{
		if (index >= 0)
		{
			return index < array.Length;
		}
		return false;
	}

	[Obsolete("Please consider using a List instead")]
	public static T[] RemoveAt<T>(this T[] source, int index)
	{
		T[] array = new T[source.Length - 1];
		if (index > 0)
		{
			Array.Copy(source, 0, array, 0, index);
		}
		if (index < source.Length - 1)
		{
			Array.Copy(source, index + 1, array, index, source.Length - index - 1);
		}
		return array;
	}

	[Obsolete("Please consider using a List instead")]
	public static T First<T>(this HashSet<T> hashSet)
	{
		using (HashSet<T>.Enumerator enumerator = hashSet.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return default(T);
	}

	[Obsolete("Please consider using a List instead")]
	public static T Get<T>(this HashSet<T> hashSet, int index)
	{
		int num = 0;
		foreach (T item in hashSet)
		{
			if (index == num)
			{
				return item;
			}
			num++;
		}
		return default(T);
	}

	public static T[] ToArray<T>(this HashSet<T> set)
	{
		T[] array = new T[set.Count];
		set.CopyTo(array);
		return array;
	}

	[Obsolete("Please consider using a List instead")]
	public static T Find<T>(this HashSet<T> set, Predicate<T> match)
	{
		foreach (T item in set)
		{
			if (match(item))
			{
				return item;
			}
		}
		return default(T);
	}

	public static void SetMaterialColor(this GameObject obj, Color color)
	{
		Renderer component = obj.GetComponent<Renderer>();
		if ((bool)component)
		{
			Material[] materials = component.materials;
			for (int i = 0; i < materials.Length; i++)
			{
				materials[i].color = color;
			}
		}
	}

	public static void SetMaterial(this GameObject obj, Material material)
	{
		Renderer component = obj.GetComponent<Renderer>();
		if ((bool)component)
		{
			component.material = material;
		}
	}

	public static void SetMaterial(this GameObject obj, Material[] materials)
	{
		Renderer component = obj.GetComponent<Renderer>();
		if ((bool)component)
		{
			component.materials = materials;
		}
	}

	public static void SetMaterialColorRecursive(this GameObject obj, Color color)
	{
		Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>();
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		Renderer[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			Material[] materials = array[i].materials;
			for (int j = 0; j < materials.Length; j++)
			{
				materials[j].color = color;
			}
		}
	}

	public static int LineCount(this string str)
	{
		return str.Split('\n').Length;
	}

	public static string Last(this string source, int tail_length)
	{
		if (tail_length >= source.Length)
		{
			return source;
		}
		return source.Substring(source.Length - tail_length);
	}

	public static string SanitizeFilename(this string source)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			source = source.Replace(oldChar, '_');
		}
		return source;
	}

	public static string SanitizePath(this string str)
	{
		return str.Replace("\\", "/").Replace("//", "/").TrimEnd('/');
	}

	public static Vector3 GridPosition(this Vector3 worldPosition, float halfGridSize = 1f, float gridOffset = 0f)
	{
		worldPosition = new Vector3(Mathf.Round((worldPosition.x - gridOffset) / halfGridSize) * halfGridSize + gridOffset, Mathf.Round((worldPosition.y - gridOffset) / halfGridSize) * halfGridSize + gridOffset, Mathf.Round((worldPosition.z - gridOffset) / halfGridSize) * halfGridSize + gridOffset);
		return worldPosition;
	}

	public static Vector3 GridTerrainPosition(this Vector3 worldPosition)
	{
		return worldPosition.GridTerrainPosition(1f);
	}

	public static Vector3 GridTerrainPosition(this Vector3 worldPosition, float grid)
	{
		if (Terrain.activeTerrain == null)
		{
			return worldPosition.GridPosition(grid);
		}
		return new Vector3(Mathf.Round(worldPosition.x / grid) * grid, Terrain.activeTerrain.SampleHeight(worldPosition), Mathf.Round(worldPosition.z / grid) * grid);
	}

	public static Grid3 ToGrid(this Vector3 worldPosition, float gridSize = 2f, float gridOffset = 0f)
	{
		return new Grid3(worldPosition, gridSize, gridOffset);
	}

	public static Grid3 ToGridPosition(this Vector3 worldPosition)
	{
		return new Grid3(worldPosition);
	}

	public static Grid3 ToGridRaw(this Vector3 vector)
	{
		return new Grid3((int)vector.x, (int)vector.y, (int)vector.z);
	}

	public static Grid3 ToGridFace(this Vector3 vector, Vector3 direction)
	{
		return vector.ToGrid().ToGridFace(direction);
	}

	public static Color ToColor(this MinableType minableType)
	{
		return minableType switch
		{
			MinableType.Iron => new Color32(byte.MaxValue, 33, byte.MaxValue, byte.MaxValue), 
			MinableType.Ice => new Color32(85, 160, 219, byte.MaxValue), 
			MinableType.Gold => new Color32(254, 254, 38, byte.MaxValue), 
			MinableType.Coal => new Color32(96, 96, 97, byte.MaxValue), 
			MinableType.Copper => new Color32(254, 200, 155, byte.MaxValue), 
			MinableType.Uranium => new Color32(136, byte.MaxValue, 130, byte.MaxValue), 
			MinableType.Nickel => new Color32(204, 114, 87, byte.MaxValue), 
			MinableType.Lead => new Color32(187, 178, 129, byte.MaxValue), 
			MinableType.Silver => Color.grey, 
			MinableType.Silicon => Color.white, 
			MinableType.Oxite => new Color32(31, byte.MaxValue, byte.MaxValue, byte.MaxValue), 
			MinableType.Volatiles => Color.red, 
			MinableType.GeyserHydrogen => Color.green, 
			MinableType.Cobalt => new Color32(183, 114, byte.MaxValue, byte.MaxValue), 
			_ => Color.black, 
		};
	}

	public static Vector3 GridCenter(this Vector3 worldPosition, float gridSquareSize = 2f, float offset = 0f)
	{
		float num = gridSquareSize * 0.5f;
		float num2 = offset + num;
		worldPosition.x = Mathf.Round((worldPosition.x - num2) / gridSquareSize) * gridSquareSize + num2;
		worldPosition.y = Mathf.Round((worldPosition.y - num2) / gridSquareSize) * gridSquareSize + num2;
		worldPosition.z = Mathf.Round((worldPosition.z - num2) / gridSquareSize) * gridSquareSize + num2;
		return worldPosition;
	}

	public static Vector3 GridCenter(this Vector3 worldPosition, Vector3 gridSize3d, Vector3 gridOffset3d)
	{
		Vector3 vector = gridSize3d * 0.5f;
		Vector3 vector2 = gridOffset3d + vector;
		worldPosition.x = Mathf.Round((worldPosition.x - vector2.x) / gridSize3d.x) * gridSize3d.x + vector2.x;
		worldPosition.y = Mathf.Round((worldPosition.y - vector2.y) / gridSize3d.y) * gridSize3d.y + vector2.y;
		worldPosition.z = Mathf.Round((worldPosition.z - vector2.z) / gridSize3d.z) * gridSize3d.z + vector2.z;
		return worldPosition;
	}

	public static Vector3 GridTerrainCenter(this Vector3 worldPosition)
	{
		return worldPosition.GridTerrainCenter(2f);
	}

	public static Vector3 GridTerrainCenter(this Vector3 worldPosition, float grid)
	{
		worldPosition -= new Vector3(1f, 1f, 1f);
		worldPosition = new Vector3(Mathf.Round(worldPosition.x / grid) * grid, Terrain.activeTerrain.SampleHeight(worldPosition), Mathf.Round(worldPosition.z / grid) * grid);
		worldPosition += new Vector3(1f, 1f, 1f);
		return worldPosition;
	}

	public static Vector3 ZeroHeight(this Vector3 worldPosition)
	{
		return new Vector3(worldPosition.x, 0f, worldPosition.z);
	}

	public static Vector3 ToCardinalDir(this Vector3 direction)
	{
		Vector3 vector = direction.Abs();
		if (vector.x > vector.y && vector.x > vector.z)
		{
			if (!(direction.x > 0f))
			{
				return Vector3.left;
			}
			return Vector3.right;
		}
		if (vector.y > vector.z)
		{
			if (!(direction.y > 0f))
			{
				return Vector3.down;
			}
			return Vector3.up;
		}
		if (!(direction.z > 0f))
		{
			return Vector3.back;
		}
		return Vector3.forward;
	}

	public static Vector3 Abs(this Vector3 vector)
	{
		return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
	}

	public static float MinComponent(this Vector3 vector)
	{
		return Mathf.Min(vector.x, Mathf.Min(vector.y, vector.z));
	}

	public static float MaxComponent(this Vector3 vector)
	{
		return Mathf.Max(vector.x, Mathf.Max(vector.y, vector.z));
	}

	public static List<T> AsList<T>(this T t)
	{
		return new List<T> { t };
	}

	public static T Pick<T>(this List<T> list)
	{
		if (list.Count != 0)
		{
			return list[_random.Next(0, list.Count)];
		}
		return default(T);
	}

	public static T Pick<T>(this List<T> list, System.Random random)
	{
		if (list.Count != 0)
		{
			return list[random.Next(0, list.Count)];
		}
		return default(T);
	}

	public static T PickOnly<T>(this List<DynamicThing> list) where T : Thing
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[_random.Next(0, list.Count)] is T result)
			{
				return result;
			}
		}
		throw new NullReferenceException($"unable to pick {typeof(T)} from list");
	}

	public static T Pick<T>(this T[] array)
	{
		if (array.Length != 0)
		{
			return array[_random.Next(0, array.Length)];
		}
		return default(T);
	}

	public static T Pick<T>(this Span<T> span)
	{
		if (span.Length != 0)
		{
			return span[_random.Next(0, span.Length)];
		}
		return default(T);
	}

	public static int[] ToIntArray(this Vector3 position)
	{
		return new int[3]
		{
			(int)Math.Round(position.x),
			(int)Math.Round(position.y),
			(int)Math.Round(position.z)
		};
	}

	public static float[] ToFloatArray(this Vector3 worldPosition)
	{
		return new float[3] { worldPosition.x, worldPosition.y, worldPosition.z };
	}

	public static void SetImageSizePreserveAspect(this RawImage rawImage, RectTransform.Axis axis, float size)
	{
		Texture mainTexture = rawImage.mainTexture;
		if (mainTexture != null)
		{
			float num = (float)mainTexture.width / (float)mainTexture.height;
			switch (axis)
			{
			case RectTransform.Axis.Horizontal:
			{
				float y = size / num;
				rawImage.rectTransform.sizeDelta = new Vector2(size, y);
				break;
			}
			case RectTransform.Axis.Vertical:
			{
				float x = size * num;
				rawImage.rectTransform.sizeDelta = new Vector2(x, size);
				break;
			}
			}
		}
	}

	public static Vector3 Between(this Vector3 pos, Vector3 to)
	{
		return to - pos;
	}

	public static Vector3 Middle(this Vector3 pos1, Vector3 pos2, float lengthAlong = 0.5f)
	{
		return pos1 + lengthAlong * (pos2 - pos1);
	}

	public static Grid3 Middle(this Grid3 pos1, Grid3 pos2)
	{
		return (pos1 + pos2) * 0.5f;
	}

	public static Vector3 DirectionTo(this Vector3 pos, Vector3 to)
	{
		return pos.Between(to).normalized;
	}

	public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
	{
		T result = value;
		if (value.CompareTo(max) > 0)
		{
			result = max;
		}
		if (value.CompareTo(min) < 0)
		{
			result = min;
		}
		return result;
	}

	public static Color ToColor(this Vector3 vec, float alpha)
	{
		return new Color(vec.x, vec.y, vec.z, alpha);
	}

	public static float[] ToFloatArray(this Quaternion worldPosition)
	{
		return new float[4] { worldPosition.x, worldPosition.y, worldPosition.z, worldPosition.w };
	}

	public static Quaternion ToQuaternion(this float[] worldPosition)
	{
		return new Quaternion(worldPosition[0], worldPosition[1], worldPosition[2], worldPosition[3]);
	}

	public static Vector3 ToVector3(this float[] worldPosition)
	{
		return new Vector3(worldPosition[0], worldPosition[1], worldPosition[2]);
	}

	public static Vector3 ToVector3(this int[] position)
	{
		return new Vector3(position[0], position[1], position[2]);
	}

	public static Color Clone(this Color color)
	{
		return new Color(color.r, color.g, color.b, color.a);
	}

	public static int AxisMax(this Mesh mesh)
	{
		return Mathf.FloorToInt((mesh.bounds.size.x > mesh.bounds.size.z) ? mesh.bounds.size.x : mesh.bounds.size.z);
	}

	public static Vector3 left(this Transform transform)
	{
		return -transform.right;
	}

	public static T ReverseFind<T>(this IList<T> list, T item)
	{
		T result = default(T);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].Equals(item))
			{
				return list[num];
			}
		}
		return result;
	}

	public static T ReverseFind<T>(this IList<T> list, Func<T, bool> rule)
	{
		T result = default(T);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (rule(list[num]))
			{
				return list[num];
			}
		}
		return result;
	}

	public static double RadianToDegree(this double angle)
	{
		return angle * _radianConvert;
	}

	public static double DegreeToRadian(this double angle)
	{
		return Math.PI * angle / 180.0;
	}

	public static bool IsDenormal(this float input)
	{
		if (input < 1.1754944E-38f)
		{
			return input > 0f;
		}
		return false;
	}

	public static bool IsDenormal(this double input)
	{
		if (input < 1.1754943508222875E-38)
		{
			return input > 0.0;
		}
		return false;
	}

	public static bool IsDenormalToNegative(this float input)
	{
		return input < 1.1754944E-38f;
	}

	public static bool IsDenormalToNegative(this double input)
	{
		return input < 1.1754943508222875E-38;
	}

	public static bool IsDenormalOrNegative(this float input)
	{
		if (input != 0f)
		{
			return input < 1.1754944E-38f;
		}
		return false;
	}

	public static bool IsDenormalOrNegative(this double input)
	{
		if (input != 0.0)
		{
			return input < 1.1754943508222875E-38;
		}
		return false;
	}

	public static bool IsDenormalOrZero(this float input)
	{
		if (input < 1.1754944E-38f)
		{
			return input >= 0f;
		}
		return false;
	}

	public static bool IsDenormalOrZero(this double input)
	{
		if (input < 1.1754943508222875E-38)
		{
			return input >= 0.0;
		}
		return false;
	}

	public static float DistanceSquared(this Vector3 vector1, Vector3 vector2)
	{
		return RocketMath.DistanceSquared(vector1, vector2);
	}

	public static float SquareDistanceComparison(this Vector3 vector1, Vector3 vector2, float value)
	{
		return RocketMath.SquareDistanceComparison(vector1, vector2, value);
	}

	public static void ReplaceRaycasters(this Dropdown dropdown)
	{
		GameObject gameObject = dropdown.template.gameObject;
		UnityEngine.Object.Destroy(gameObject.GetComponent<GraphicRaycaster>());
		gameObject.AddComponent<MyGraphicsRaycaster>();
	}

	public static float RoundTo(this float f, float b)
	{
		f += b / 2f;
		f = Mathf.Floor(f / b) * b;
		return f;
	}

	public static Vector3 ToVector3X(this float f)
	{
		return new Vector3(f, 0f, 0f);
	}

	public static Vector3 ToVector3Y(this float f)
	{
		return new Vector3(0f, f, 0f);
	}

	public static Vector3 ToVector3Z(this float f)
	{
		return new Vector3(0f, 0f, f);
	}

	public static Vector3 FindClosestLocalAxis(this Vector3 vector, Transform transform)
	{
		return vector.FindClosestLocalAxis(transform.up, transform.right, transform.forward);
	}

	public static Vector3 FindClosestLocalAxis(this Vector3 vector)
	{
		return vector.FindClosestLocalAxis(Vector3.up, Vector3.right, Vector3.forward);
	}

	public static Vector3 FindClosestLocalAxis(this Vector3 vector, params Vector3[] axes)
	{
		if (axes.Length == 0)
		{
			return vector.FindClosestLocalAxis();
		}
		Vector3 lhs = Vector3.Project(vector, axes[0]);
		Vector3 result = Mathf.Sign(Vector3.Dot(lhs, axes[0])) * axes[0];
		float sqrMagnitude = lhs.sqrMagnitude;
		for (int i = 1; i < axes.Length; i++)
		{
			lhs = Vector3.Project(vector, axes[i]);
			if (lhs.sqrMagnitude > sqrMagnitude)
			{
				sqrMagnitude = lhs.sqrMagnitude;
				result = Mathf.Sign(Vector3.Dot(lhs, axes[i])) * axes[i];
			}
		}
		return result;
	}

	public static void RotateOnto(this Transform transform, Vector3 forward, Vector3 target, float tol = 0.1f)
	{
		transform.RotateOnto(forward, target, Vector3.Cross(forward, target));
	}

	public static void RotateOnto(this Transform transform, Vector3 forward, Vector3 target, Vector3 relativeTo, float tol = 0.1f)
	{
		if (RocketMath.Approximately(relativeTo, Vector3.zero))
		{
			if (Vector3.Dot(forward, target) < 0f)
			{
				Vector3 vector = Vector3.Cross(transform.up, forward);
				if (RocketMath.Approximately(vector, Vector3.zero))
				{
					vector = Vector3.Cross(transform.right, forward);
				}
				transform.Rotate(vector, 180f);
			}
		}
		else
		{
			float num = Vector3.Angle(forward, target);
			if (Vector3.Dot(Vector3.Cross(forward, target), relativeTo) < 0f)
			{
				num = 0f - num;
			}
			if (!(Mathf.Abs(num) < tol))
			{
				transform.Rotate(relativeTo, num, Space.World);
			}
		}
	}

	public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Quaternion angle)
	{
		Vector3 vector = point - pivot;
		vector = angle * vector;
		return pivot + vector;
	}

	public static string ToCompactString(this TimeSpan time)
	{
		if (time.TotalSeconds < 1.0)
		{
			return "<1s";
		}
		string text = ((time.Minutes < 10) ? ("0" + time.Minutes) : time.Minutes.ToString());
		string text2 = ((time.Seconds < 10) ? ("0" + time.Seconds) : time.Seconds.ToString());
		if (time.Hours < 1)
		{
			return text + ":" + text2;
		}
		return time.Hours + ":" + time.Minutes + ":" + time.Seconds;
	}

	private static string FormatTimeUnit(double value, string unit)
	{
		return FormatTimeUnit(value, unit, null);
	}

	private static string FormatTimeUnit(double value, string unit, string unitPlural)
	{
		int num = (int)Math.Round(value * 10.0, MidpointRounding.AwayFromZero);
		(string, string, int) key = (unit, unitPlural, num);
		if (timeUnitCache.TryGetValue(key, out var value2))
		{
			return value2;
		}
		int num2 = (int)Math.Round(value);
		StringBuilder stringBuilder = timeUnitBuilder.Clear();
		bool flag;
		if (Math.Abs(value - (double)num2) < 0.1)
		{
			stringBuilder.Append(num2);
			flag = num2 > 1;
		}
		else
		{
			stringBuilder.Append(num / 10);
			stringBuilder.Append(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
			stringBuilder.Append((char)(48 + num % 10));
			flag = value > 1.0;
		}
		stringBuilder.Append(' ');
		if (unitPlural != null)
		{
			stringBuilder.Append(flag ? unitPlural : unit);
		}
		else
		{
			stringBuilder.Append(unit);
			if (flag)
			{
				stringBuilder.Append('s');
			}
		}
		string text = stringBuilder.ToString();
		if (timeUnitCache.Count >= 2048)
		{
			timeUnitCache.Clear();
		}
		timeUnitCache[key] = text;
		return text;
	}

	public static string ToNearestString(this TimeLength time)
	{
		double num = time.ToTotalSeconds();
		double num2 = num / 60.0;
		double num3 = num2 / 60.0;
		if (num >= double.MaxValue)
		{
			return GameStrings.TimeLengthInfiniteSeconds;
		}
		if (num <= double.Epsilon)
		{
			return GameStrings.TimeLengthZeroSeconds;
		}
		if (num < 0.001)
		{
			return GameStrings.TimeLengthLessThan1Millisecond;
		}
		if (num < 1.0)
		{
			return FormatTimeUnit(num * 1000.0, GameStrings.TimeLengthMillisecond);
		}
		if (num < 60.0)
		{
			return FormatTimeUnit(num, GameStrings.TimeLengthSecond);
		}
		if (num2 < 60.0)
		{
			return FormatTimeUnit(num2, GameStrings.TimeLengthMinute);
		}
		if (num3 < 24.0)
		{
			return FormatTimeUnit(num3, GameStrings.TimeLengthHour);
		}
		double num4 = num3 / 24.0;
		if (num4 < 7.0)
		{
			return FormatTimeUnit(num4, GameStrings.TimeLengthDay);
		}
		double num5 = num4 / 7.0;
		if (num5 < 4.0)
		{
			return FormatTimeUnit(num5, GameStrings.TimeLengthWeek);
		}
		double num6 = num4 / 30.0;
		if (num6 < 12.0)
		{
			return FormatTimeUnit(num6, GameStrings.TimeLengthMonth);
		}
		double num7 = num4 / 365.0;
		if (num7 < 100.0)
		{
			return FormatTimeUnit(num7, GameStrings.TimeLengthYear);
		}
		double num8 = num7 / 1000.0;
		if (num8 < 1000.0)
		{
			return FormatTimeUnit(num8, GameStrings.TimeLengthCentury, GameStrings.TimeLengthCenturies);
		}
		double num9 = num8 / 1000.0;
		if (num9 < 1000.0)
		{
			return FormatTimeUnit(num9, GameStrings.TimeLengthMillennium, GameStrings.TimeLengthMillennia);
		}
		return FormatTimeUnit(num9 / 1000.0, GameStrings.TimeLengthEon, GameStrings.TimeLengthAeons);
	}

	public static T GetNext<T>(this T structMember, bool isForward = true, bool isSorted = false) where T : struct
	{
		if (!typeof(T).IsEnum)
		{
			throw new ArgumentException("T must be an enumerated type");
		}
		enumValuesLookup.TryGetValue(typeof(T), out var value);
		if (value == null)
		{
			value = Enum.GetValues(typeof(T));
			enumValuesLookup.Add(typeof(T), value);
		}
		T[] array = (T[])value;
		int[] value2 = null;
		if (isSorted)
		{
			enumValuesSortedRedirector.TryGetValue(typeof(T), out value2);
			if (value2 == null)
			{
				string[] enumKeys = Enum.GetNames(typeof(T));
				value2 = new int[enumKeys.Length];
				for (int i = 0; i < value2.Length; i++)
				{
					value2[i] = i;
				}
				Array.Sort(value2, (int a, int b) => enumKeys[a].CompareTo(enumKeys[b]));
			}
		}
		int num = Array.IndexOf(array, structMember);
		if (isSorted)
		{
			num = Array.IndexOf(value2, num);
		}
		num += (isForward ? 1 : (-1));
		num %= array.Length;
		if (num < 0)
		{
			num += array.Length;
		}
		return array[(value2 == null) ? num : value2[num]];
	}

	public static T Pop<T>(this List<T> list)
	{
		T result = list.Last();
		list.RemoveAt(list.Count - 1);
		return result;
	}

	public static void StartCoroutineOnMainThread(this MonoBehaviour mono, IEnumerator routine)
	{
		if (!GameManager.IsMainThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(routine);
		}
		else
		{
			mono.StartCoroutine(routine);
		}
	}

	public static bool IsValidIndex<T>(this T[] t, int index)
	{
		if (index >= 0)
		{
			return index < t.Length;
		}
		return false;
	}

	public static bool IsValidIndex<T>(this T[,] t, int index, int dimension)
	{
		if (index >= 0)
		{
			return index < t.GetLength(dimension);
		}
		return false;
	}

	public static bool IsValidIndex<T>(this T[,,] t, int index, int dimension)
	{
		if (index >= 0)
		{
			return index < t.GetLength(dimension);
		}
		return false;
	}

	public static bool AreAllIndicesValid<T>(this T[,] t, int index0, int index1)
	{
		if (t.IsValidIndex(index0, 0))
		{
			return t.IsValidIndex(index1, 1);
		}
		return false;
	}

	public static bool AreAllIndicesValid<T>(this T[,,] t, int index0, int index1, int index2)
	{
		if (t.IsValidIndex(index0, 0) && t.IsValidIndex(index1, 1))
		{
			return t.IsValidIndex(index2, 2);
		}
		return false;
	}

	public static AtmosphereHelper.MatterState AsMatterState(this Pipe.ContentType type)
	{
		return type switch
		{
			Pipe.ContentType.Liquid => AtmosphereHelper.MatterState.Liquid, 
			Pipe.ContentType.Gas => AtmosphereHelper.MatterState.Gas, 
			Pipe.ContentType.Unknown => AtmosphereHelper.MatterState.All, 
			Pipe.ContentType.All => AtmosphereHelper.MatterState.All, 
			_ => AtmosphereHelper.MatterState.All, 
		};
	}

	public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return default(TValue);
		}
		return value;
	}

	public static string ToArrayString<T>(this IEnumerable<T> collection)
	{
		string text = string.Join(",\n", collection);
		if (!string.IsNullOrEmpty(text))
		{
			return "[\n" + text + "\n]";
		}
		return "[]";
	}

	public static T GetElement<T>(this IList<T> collection, int index, T fallback = default(T))
	{
		if (index >= 0 && index < collection.Count)
		{
			return collection[index];
		}
		return fallback;
	}

	public static void SetLayerRecursive(this GameObject gameObject, int layer)
	{
		if ((bool)gameObject)
		{
			gameObject.layer = layer;
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				gameObject.transform.GetChild(i).gameObject.SetLayerRecursive(layer);
			}
		}
	}

	public static async UniTaskVoid ApplyAvatarSprite(this Image image, ulong clientId, AvatarSize size = AvatarSize.Small)
	{
		Sprite sprite = await NetworkManager.GetAvatarSprite(clientId, size);
		image.enabled = sprite;
		image.sprite = sprite;
	}

	public static string GetXmlEnumAttributeValueFromEnum<TEnum>(this TEnum value) where TEnum : struct, IConvertible
	{
		Type typeFromHandle = typeof(TEnum);
		string text = value.ToString();
		MemberInfo memberInfo = typeFromHandle.GetMember(text).FirstOrDefault();
		if (memberInfo == null)
		{
			throw new NullReferenceException();
		}
		XmlEnumAttribute xmlEnumAttribute = memberInfo.GetCustomAttributes(inherit: false).OfType<XmlEnumAttribute>().FirstOrDefault();
		if (xmlEnumAttribute != null)
		{
			return xmlEnumAttribute.Name;
		}
		return text;
	}

	public static Vector3 Round(this Vector3 vector, int decimals = 0)
	{
		return new Vector3((float)Math.Round(vector.x, decimals), (float)Math.Round(vector.y, decimals), (float)Math.Round(vector.z, decimals));
	}

	public static Vector3Int FloorToInt(this Vector3 vector)
	{
		return new Vector3Int(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y), Mathf.FloorToInt(vector.z));
	}

	public static Vector3Int RoundToInt(this Vector3 vector)
	{
		return new Vector3Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
	}

	public static Vector3 RandomInUnitSphere(this System.Random rand)
	{
		float num;
		float num2;
		float num3;
		do
		{
			num = (float)(rand.NextDouble() * 2.0 - 1.0);
			num2 = (float)(rand.NextDouble() * 2.0 - 1.0);
			num3 = (float)(rand.NextDouble() * 2.0 - 1.0);
		}
		while (!(num * num + num2 * num2 + num3 * num3 <= 1f));
		return new Vector3(num, num2, num3);
	}

	public static bool TryRandomInUnitSphere(this System.Random rand, out Vector3 position)
	{
		float num = (float)(rand.NextDouble() * 2.0 - 1.0);
		float num2 = (float)(rand.NextDouble() * 2.0 - 1.0);
		float num3 = (float)(rand.NextDouble() * 2.0 - 1.0);
		if (num * num + num2 * num2 + num3 * num3 <= 1f)
		{
			position = new Vector3(num, num2, num3);
			return true;
		}
		position = Vector3.zero;
		return false;
	}

	public static Vector3 RandomInBox(this System.Random rand, Vector3 min, Vector3 max)
	{
		double num = rand.NextDouble() * (double)(max.x - min.x) + (double)min.x;
		double num2 = rand.NextDouble() * (double)(max.y - min.y) + (double)min.y;
		double num3 = rand.NextDouble() * (double)(max.z - min.z) + (double)min.z;
		return new Vector3((float)num, (float)num2, (float)num3);
	}

	public static Vector3Int RandomInBox(this System.Random rand, Vector3Int min, Vector3Int max)
	{
		double num = rand.NextDouble() * (double)(max.x - min.x) + (double)min.x;
		double num2 = rand.NextDouble() * (double)(max.y - min.y) + (double)min.y;
		double num3 = rand.NextDouble() * (double)(max.z - min.z) + (double)min.z;
		return new Vector3((float)num, (float)num2, (float)num3).FloorToInt();
	}

	public static bool ContainsXZ(this BoundsInt boundsInt, Vector3 position)
	{
		if (position.x >= (float)boundsInt.xMin && position.z >= (float)boundsInt.zMin && position.x < (float)boundsInt.xMax)
		{
			return position.z < (float)boundsInt.zMax;
		}
		return false;
	}

	public static int[] GetOpenEndLocationPermutation(this ISmartRotatable rotatable, Quaternion offset)
	{
		return rotatable.GetOpenEndLocationPermutation(offset, rotatable.Transform.rotation);
	}

	public static int[] GetOpenEndLocationPermutation(this ISmartRotatable rotatable, Quaternion offset, Quaternion rotation)
	{
		int[] openEndsPermutation = rotatable.GetOpenEndsPermutation();
		switch (rotatable.GetPlacementType())
		{
		case PlacementSnap.Face:
			switch (rotatable.GetRotationAxis())
			{
			case RotationAxis.All:
				if (rotatable.GetAllowedRotations() == AllowedRotations.All)
				{
					rotation = Quaternion.Inverse(offset) * rotation;
					Vector3 eulerAngles2 = rotation.eulerAngles;
					for (float num7 = eulerAngles2.z.RoundTo(90f); num7 > 0f; num7 -= 90f)
					{
						SmartRotate.RotZ.Permutation.Permute(openEndsPermutation);
					}
					for (float num8 = eulerAngles2.x.RoundTo(90f); num8 > 0f; num8 -= 90f)
					{
						SmartRotate.RotX.Permutation.Permute(openEndsPermutation);
					}
					for (float num9 = eulerAngles2.y.RoundTo(90f); num9 > 0f; num9 -= 90f)
					{
						SmartRotate.RotY.Permutation.Permute(openEndsPermutation);
					}
				}
				break;
			case RotationAxis.Y:
			{
				AllowedRotations allowedRotations = rotatable.GetAllowedRotations();
				if (allowedRotations == AllowedRotations.Wall || allowedRotations == AllowedRotations.All)
				{
					rotation = Quaternion.Inverse(offset) * rotation;
					for (float num6 = rotation.eulerAngles.y.RoundTo(90f); num6 > 0f; num6 -= 90f)
					{
						SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
					}
				}
				break;
			}
			case RotationAxis.ZY:
				switch (rotatable.GetAllowedRotations())
				{
				case AllowedRotations.Wall:
				case AllowedRotations.All:
				{
					rotation = Quaternion.Inverse(offset) * rotation;
					Vector3 eulerAngles = rotation.eulerAngles;
					for (float num3 = eulerAngles.z.RoundTo(90f); num3 > 0f; num3 -= 90f)
					{
						SmartRotate.RotZ.Permutation.Permute(openEndsPermutation);
					}
					for (float num4 = eulerAngles.x.RoundTo(90f); num4 > 0f; num4 -= 90f)
					{
						SmartRotate.RotX.Permutation.Permute(openEndsPermutation);
					}
					for (float num5 = eulerAngles.y.RoundTo(90f); num5 > 0f; num5 -= 90f)
					{
						SmartRotate.RotY.Permutation.Permute(openEndsPermutation);
					}
					break;
				}
				case AllowedRotations.Floor:
				{
					rotation = Quaternion.Inverse(offset) * rotation;
					rotation = Quaternion.Euler(90f, 0f, 0f) * rotation;
					for (float num2 = rotation.eulerAngles.z.RoundTo(90f); num2 > 0f; num2 -= 90f)
					{
						SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
					}
					break;
				}
				}
				break;
			}
			break;
		case PlacementSnap.Grid:
			switch (rotatable.GetRotationAxis())
			{
			case RotationAxis.XY:
			case RotationAxis.ZX:
			case RotationAxis.ZY:
			case RotationAxis.All:
			{
				rotation = Quaternion.Inverse(offset) * rotation;
				Vector3 eulerAngles3 = rotation.eulerAngles;
				for (float num13 = eulerAngles3.z.RoundTo(90f); num13 > 0f; num13 -= 90f)
				{
					SmartRotate.RotZ.Permutation.Permute(openEndsPermutation);
				}
				for (float num14 = eulerAngles3.x.RoundTo(90f); num14 > 0f; num14 -= 90f)
				{
					SmartRotate.RotX.Permutation.Permute(openEndsPermutation);
				}
				for (float num15 = eulerAngles3.y.RoundTo(90f); num15 > 0f; num15 -= 90f)
				{
					SmartRotate.RotY.Permutation.Permute(openEndsPermutation);
				}
				break;
			}
			case RotationAxis.X:
			{
				rotation = Quaternion.Inverse(offset) * rotation;
				for (float num11 = rotation.eulerAngles.x.RoundTo(90f); num11 > 0f; num11 -= 90f)
				{
					SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
				}
				break;
			}
			case RotationAxis.Y:
			{
				rotation = Quaternion.Inverse(offset) * rotation;
				for (float num12 = rotation.eulerAngles.y.RoundTo(90f); num12 > 0f; num12 -= 90f)
				{
					SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
				}
				break;
			}
			case RotationAxis.Z:
			{
				rotation = Quaternion.Inverse(offset) * rotation;
				for (float num10 = rotation.eulerAngles.z.RoundTo(90f); num10 > 0f; num10 -= 90f)
				{
					SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
				}
				break;
			}
			}
			break;
		case PlacementSnap.FaceMount:
		{
			for (float num = rotatable.Transform.localEulerAngles.z.RoundTo(90f); num > 0f; num -= 90f)
			{
				SmartRotate.Rot2D.Permutation.Permute(openEndsPermutation);
			}
			break;
		}
		default:
			Debug.LogError(rotatable.GetPlacementType().ToString() + " not supported");
			break;
		}
		return openEndsPermutation;
	}
}
