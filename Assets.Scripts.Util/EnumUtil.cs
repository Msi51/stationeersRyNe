using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class EnumUtil
{
	private static Dictionary<Type, Dictionary<int, int>> TypeToEnumHashDictionary = new Dictionary<Type, Dictionary<int, int>>();

	public static IEnumerable<T> GetValues<T>()
	{
		return Enum.GetValues(typeof(T)).Cast<T>();
	}

	public static List<T> RotateFrom<T>(this List<T> list, int startIndex)
	{
		if (list == null)
		{
			throw new ArgumentNullException("list");
		}
		if (startIndex < 0 || startIndex >= list.Count)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		List<T> range = list.GetRange(startIndex, list.Count - startIndex);
		List<T> range2 = list.GetRange(0, startIndex);
		range.AddRange(range2);
		return range;
	}

	private static Dictionary<int, int> GetHashes(Type enumType)
	{
		string[] names = Enum.GetNames(enumType);
		Array values = Enum.GetValues(enumType);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < names.Length; i++)
		{
			dictionary[(int)values.GetValue(i)] = Animator.StringToHash(names[i]);
		}
		return dictionary;
	}

	public static Dictionary<int, int> GetHashesCached(Type enumType)
	{
		TypeToEnumHashDictionary.TryGetValue(enumType, out var value);
		if (value == null)
		{
			value = GetHashes(enumType);
			TypeToEnumHashDictionary[enumType] = value;
		}
		return value;
	}
}
