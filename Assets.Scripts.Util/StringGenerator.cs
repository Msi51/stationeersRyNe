using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class StringGenerator
{
	public static Dictionary<StringReferenceInt, string> _stringReferenceInt = new Dictionary<StringReferenceInt, string>();

	private static readonly int _falseHash = Animator.StringToHash("False");

	private static readonly int _trueHash = Animator.StringToHash("True");

	public static string GetString(int value, Unit unit = Unit.None)
	{
		StringReferenceInt key = new StringReferenceInt(value, unit);
		_stringReferenceInt.TryGetValue(key, out var value2);
		if (!_stringReferenceInt.ContainsKey(key))
		{
			value2 = key.MakeString();
			_stringReferenceInt.Add(key, value2);
		}
		return value2;
	}

	public static string GetString(bool value)
	{
		return Localization.GetAction(value ? _trueHash : _falseHash);
	}
}
