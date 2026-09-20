using System;
using System.Globalization;
using System.Text;
using Assets.Scripts.Util;

namespace Assets.Scripts;

public class EnumCollection<T1, T2> : IEnumCollection where T1 : Enum, IConvertible, new() where T2 : IConvertible, IEquatable<T2>
{
	public T1[] Values;

	public T2[] ValuesAsInts;

	public readonly string[] Names;

	public readonly string[] PaddedNames;

	public readonly string LongestName;

	public T1 this[int index] => Values[index];

	public int Length { get; }

	public static implicit operator string[](EnumCollection<T1, T2> collection)
	{
		return collection.Names;
	}

	public static implicit operator T2[](EnumCollection<T1, T2> collection)
	{
		return collection.ValuesAsInts;
	}

	public EnumCollection(bool toProper = true)
	{
		Values = (T1[])Enum.GetValues(typeof(T1));
		Length = Values.Length;
		Names = Enum.GetNames(typeof(T1));
		PaddedNames = Enum.GetNames(typeof(T1));
		LongestName = string.Empty;
		ValuesAsInts = (T2[])Enum.GetValues(typeof(T1));
		for (int i = 0; i < Values.Length; i++)
		{
			string value = Names[i];
			if (toProper)
			{
				Names[i] = value.ToProper();
			}
			if (Names[i].Length > LongestName.Length)
			{
				LongestName = Names[i];
			}
		}
		for (int j = 0; j < Values.Length; j++)
		{
			PaddedNames[j] = Names[j].PadRight(LongestName.Length, ' ');
		}
	}

	public virtual string GetNameFromIndex(int index, bool padded = false)
	{
		if (padded)
		{
			return PaddedNames[index];
		}
		return Names[index];
	}

	public T1[] GetValues()
	{
		return Values;
	}

	public string GetName(T1 value, bool padded = false)
	{
		int num = value.ToInt32(CultureInfo.InvariantCulture);
		for (int i = 0; i < Length; i++)
		{
			ref readonly T2 reference = ref ValuesAsInts[i];
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			if (reference.ToInt32(invariantCulture) == num)
			{
				return GetNameFromIndex(i, padded);
			}
		}
		return string.Empty;
	}

	public string GetEnumTypeName()
	{
		return typeof(T1).Name;
	}

	public string GetNameFromValue(int value, bool padded = false)
	{
		for (int i = 0; i < Length; i++)
		{
			ref readonly T2 reference = ref ValuesAsInts[i];
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			if (reference.ToInt32(invariantCulture) == value)
			{
				return GetNameFromIndex(i, padded);
			}
		}
		return string.Empty;
	}

	public int GetIndexFromValue(T1 value)
	{
		for (int i = 0; i < Values.Length; i++)
		{
			T2 other = (T2)(object)value;
			if (ValuesAsInts[i].Equals(other))
			{
				return i;
			}
		}
		throw new IndexOutOfRangeException();
	}

	public int GetIntFromIndex(int i)
	{
		ref readonly T2 reference = ref ValuesAsInts[i];
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		return reference.ToInt32(invariantCulture);
	}

	public string PrintAll(string begin = "", string end = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < Length; i++)
		{
			if (!string.IsNullOrEmpty(begin))
			{
				stringBuilder.Append(begin);
			}
			stringBuilder.Append(GetNameFromIndex(i));
			if (!string.IsNullOrEmpty(end))
			{
				stringBuilder.Append(end);
			}
			if (i < Length - 1)
			{
				stringBuilder.Append(", ");
			}
		}
		return stringBuilder.ToString();
	}

	public T1 Get(string name)
	{
		for (int i = 0; i < Length; i++)
		{
			if (Names[i].Equals(name, StringComparison.InvariantCultureIgnoreCase))
			{
				return Values[i];
			}
		}
		return default(T1);
	}
}
