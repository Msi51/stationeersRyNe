using System;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Util;

public class EnumStrings<T> where T : Enum
{
	public string[] Names { get; }

	public Assets.Scripts.Localization2.GameString[] GameStrings { get; }

	public EnumStrings()
	{
		Names = Enum.GetNames(typeof(T));
	}
}
