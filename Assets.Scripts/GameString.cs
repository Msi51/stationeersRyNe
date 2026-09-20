using UnityEngine;

namespace Assets.Scripts;

public class GameString
{
	private int _key;

	private string _baseString;

	public static GameString Invalid = Create("InvalidString", "Invalid");

	public string DisplayString => _baseString;

	public static GameString Create(string keyString, string baseString)
	{
		return new GameString
		{
			_baseString = baseString,
			_key = Animator.StringToHash(keyString)
		};
	}

	public override string ToString()
	{
		return DisplayString;
	}
}
