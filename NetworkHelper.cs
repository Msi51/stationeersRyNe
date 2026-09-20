using UnityEngine;

public class NetworkHelper : MonoBehaviour
{
	public static class Fragments
	{
		public const int MAX_SIZE = 1400;

		public const int MIN_SIZE = 128;

		public const int DEFAULT_SIZE = 1024;

		public const int MTU_HEADER_MARGIN = 100;
	}

	public enum FileUnitType
	{
		EB,
		PB,
		TB,
		GB,
		MB,
		KB,
		B
	}

	public const int SYSTEM_MESSAGE_POOL_SIZE = 64;

	public static string GetBytesReadable(long i)
	{
		long num = ((i < 0) ? (-i) : i);
		FileUnitType fileUnitType;
		double num2;
		if (num >= 1152921504606846976L)
		{
			fileUnitType = FileUnitType.EB;
			num2 = i >> 50;
		}
		else if (num >= 1125899906842624L)
		{
			fileUnitType = FileUnitType.PB;
			num2 = i >> 40;
		}
		else if (num >= 1099511627776L)
		{
			fileUnitType = FileUnitType.TB;
			num2 = i >> 30;
		}
		else if (num >= 1073741824)
		{
			fileUnitType = FileUnitType.GB;
			num2 = i >> 20;
		}
		else if (num >= 1048576)
		{
			fileUnitType = FileUnitType.MB;
			num2 = i >> 10;
		}
		else if (num >= 1024)
		{
			fileUnitType = FileUnitType.KB;
			num2 = i;
		}
		else
		{
			fileUnitType = FileUnitType.B;
			num2 = i;
		}
		return $"{num2} {fileUnitType}";
	}
}
