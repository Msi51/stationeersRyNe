using System;

namespace Assets.Scripts.OpenNat;

internal static class EnumExtension
{
	public static bool HasFlag(this Enum value, Enum flag)
	{
		int num = (int)(object)value;
		int num2 = (int)(object)flag;
		return (num & num2) == num2;
	}
}
