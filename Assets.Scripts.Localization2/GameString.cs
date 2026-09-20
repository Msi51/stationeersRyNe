using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Localization2;

public class GameString
{
	private static readonly Dictionary<int, GameString> _gameStringLookup = new Dictionary<int, GameString>();

	private readonly string[] _args;

	private readonly string _keyString;

	private readonly string _baseString;

	private readonly int _key;

	private string _overrideString;

	public string DisplayString
	{
		get
		{
			if (string.IsNullOrEmpty(_overrideString))
			{
				return _baseString;
			}
			return _overrideString;
		}
	}

	public int Key => _key;

	public string KeyString { get; }

	public string RawValue { get; }

	public static implicit operator string(GameString gameString)
	{
		return gameString.DisplayString;
	}

	private GameString(params string[] strings)
	{
		_keyString = strings[0];
		_baseString = strings[1];
		_key = Animator.StringToHash(_keyString);
		KeyString = strings[0];
		RawValue = strings[1];
		if (strings.Length > 2)
		{
			int num = strings.Length - 2;
			_args = new string[num];
			Array.Copy(strings, 2, _args, 0, num);
		}
		else
		{
			_args = Array.Empty<string>();
		}
		_baseString = Prepare(_keyString, _baseString, _args);
		_gameStringLookup.Add(_key, this);
	}

	public static GameString Create(params string[] strings)
	{
		if (strings == null || strings.Length < 2)
		{
			throw new NullReferenceException("Game string is invalid");
		}
		string text = strings[0];
		if (string.IsNullOrEmpty(text))
		{
			throw new NullReferenceException("Key string is invalid");
		}
		if (TryGet(Animator.StringToHash(text), out var gameString))
		{
			Debug.LogError("Localization error: key '" + text + "' already exists with '" + gameString.DisplayString + "'");
			return gameString;
		}
		return new GameString(strings);
	}

	public static void UpdateLanguage(List<Localization.Record> records)
	{
		HashSet<int> hashSet = new HashSet<int>();
		LocalizedEnumCollections.CreateIfNeeded();
		foreach (Localization.Record record in records)
		{
			int num = Animator.StringToHash(record.Key);
			hashSet.Add(num);
			if (TryGet(num, out var gameString))
			{
				gameString.UpdateOverrideString(record);
			}
		}
		foreach (KeyValuePair<int, GameString> item in _gameStringLookup)
		{
			if (!hashSet.Contains(item.Key))
			{
				item.Value.ClearOverrideString();
			}
		}
		LocalizedEnumCollections.OnLanguageChanged();
	}

	public override string ToString()
	{
		return AsString();
	}

	public string AsString()
	{
		return DisplayString;
	}

	public string AsString(string arg0)
	{
		return string.Format(DisplayString, arg0);
	}

	public string AsString(string arg0, string arg1)
	{
		return string.Format(DisplayString, arg0, arg1);
	}

	public string AsString(string arg0, string arg1, string arg2)
	{
		return string.Format(DisplayString, arg0, arg1, arg2);
	}

	public string AsString(string arg0, string arg1, string arg2, string arg3)
	{
		return string.Format(DisplayString, arg0, arg1, arg2, arg3);
	}

	public void AppendFormat(StringBuilder sb)
	{
		sb.AppendFormat(DisplayString);
	}

	public void AppendFormat(StringBuilder sb, string arg0)
	{
		sb.AppendFormat(DisplayString, arg0);
	}

	public void AppendFormat(StringBuilder sb, string arg0, string arg1)
	{
		sb.AppendFormat(DisplayString, arg0, arg1);
	}

	public void AppendFormat(StringBuilder sb, string arg0, string arg1, string arg2)
	{
		sb.AppendFormat(DisplayString, arg0, arg1, arg2);
	}

	public static bool TryGet(int key, out GameString gameString)
	{
		return _gameStringLookup.TryGetValue(key, out gameString);
	}

	private static string ExtractValue(string input)
	{
		int num = input.IndexOf(':') + 1;
		int num2 = input.IndexOf('}', num);
		if (num < 1 || num2 < 0 || num2 <= num)
		{
			return "Invalid format";
		}
		return input.Substring(num, num2 - num);
	}

	private static string Prepare(string key, string original, string[] arguments)
	{
		string text = original;
		if (string.IsNullOrEmpty(original) || arguments.Length == 0)
		{
			return text;
		}
		List<Match> list = Regex.Matches(original, "([{](?:LOCAL:|KEY:)\\d*\\p{Lu}[\\p{Lu}\\p{Ll}_\\d]*[}])(?<!\\1[\\s\\S]\\1)", RegexOptions.IgnoreCase).ToList();
		List<Match> list2 = Regex.Matches(original, "([{]([\\p{Lu}]+):\\d*[\\p{Lu}][\\p{Lu}\\p{Ll}_\\d]*[}])(?<!\\1[\\s\\S]\\1)", RegexOptions.IgnoreCase).ToList();
		if (list.Count != list2.Count)
		{
			Debug.LogError("Error string <b>" + key + "</b> has " + StringManager.Get(list2.Count) + " formatted arguments when " + StringManager.Get(list.Count) + " expected");
			return original;
		}
		if (list.Count != arguments.Length)
		{
			Debug.LogError("Error string <b>" + key + "</b> has " + StringManager.Get(list.Count) + " arguments when " + StringManager.Get(arguments.Length) + " expected");
			return original;
		}
		foreach (Match item in list)
		{
			string text2 = ExtractValue(item.Value);
			if (!arguments.Contains(text2))
			{
				Debug.LogError("Error string <b>" + key + "</b> has argument '" + text2 + "' that is not in the arguments list");
				text = text.Replace(text2, "??" + text2 + "??");
			}
		}
		for (int i = 0; i < arguments.Length; i++)
		{
			string text3 = arguments[i];
			if (string.IsNullOrEmpty(text3))
			{
				Debug.LogError(key + " contains null argument");
				return original;
			}
			string text4 = $"{i}";
			text = Regex.Replace(text, "{LOCAL:" + text3 + "}", "{" + text4 + "}", RegexOptions.IgnoreCase);
			text = Regex.Replace(text, "{KEY:" + text3 + "}", "<color=#FBB03B>{" + text4 + "}</color>", RegexOptions.IgnoreCase);
		}
		return text;
	}

	private void UpdateOverrideString(Localization.Record record)
	{
		_overrideString = Prepare(record.Key, record.Value, _args);
	}

	private void ClearOverrideString()
	{
		_overrideString = string.Empty;
	}

	public string AsColor(string color)
	{
		return DisplayString.AsColor(color);
	}

	public static GameString GetOrCreate(string keyString, string baseString)
	{
		if (!_gameStringLookup.TryGetValue(Animator.StringToHash(keyString), out var value))
		{
			return new GameString(keyString, baseString);
		}
		return value;
	}
}
