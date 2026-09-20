using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class Regexes
{
	private static readonly Dictionary<int, Regex> CachedType;

	private static readonly Dictionary<int, Regex> CachedTypeWithSpaces;

	private static readonly Dictionary<int, Regex> CachedVariable;

	private static readonly Dictionary<int, Regex> CachedCommandA2;

	private static readonly string _patternTypeWithSpaces;

	private static readonly string _patternType;

	private static readonly string _patternVariable;

	private static readonly string _patternCommandsA2;

	public static readonly Regex Comment;

	public static readonly Regex CommentLite;

	public static readonly Regex Numbers;

	public static readonly Regex PreprocessStrings;

	public static readonly Regex PreprocessHashes;

	public static readonly Regex PreprocessBinary;

	public static readonly Regex PreprocessHex;

	public static readonly Regex Constants;

	public static readonly Regex ScriptLine;

	public static readonly Regex Device;

	public static readonly Regex Network;

	public static readonly Regex Register;

	public static readonly Regex JumpReferences;

	public static readonly Regex LeadingWhitespace;

	private static Regex GetTypeFromCache(string type, string regexPattern, Dictionary<int, Regex> dictionary)
	{
		int key = Animator.StringToHash(type);
		if (dictionary.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new Regex(string.Format(regexPattern, type));
		dictionary.Add(key, value);
		return value;
	}

	public static Regex TypeWithSpaces(string type)
	{
		return GetTypeFromCache(type, _patternTypeWithSpaces, CachedTypeWithSpaces);
	}

	public static Regex Type(string type)
	{
		return GetTypeFromCache(type, _patternType, CachedType);
	}

	public static Regex Variable(string type)
	{
		return GetTypeFromCache(type, _patternVariable, CachedVariable);
	}

	public static Regex CommandA2(string command)
	{
		return GetTypeFromCache(command, _patternCommandsA2, CachedCommandA2);
	}

	public static string CleanInvalidXmlChars(string text)
	{
		string pattern = "[^\\x09\\x0A\\x0D\\x20-\\uD7FF\\uE000-\\uFFFD\\u10000-\\u10FFFF]";
		return Regex.Replace(text, pattern, "");
	}

	static Regexes()
	{
		CachedType = new Dictionary<int, Regex>();
		CachedTypeWithSpaces = new Dictionary<int, Regex>();
		CachedVariable = new Dictionary<int, Regex>();
		CachedCommandA2 = new Dictionary<int, Regex>();
		_patternTypeWithSpaces = "{{{0}:([\\s\\S]+?)}}";
		_patternType = "{{{0}:([\\S]+?)}}";
		_patternVariable = "#{0}#";
		_patternCommandsA2 = "{0} (-?[a-zA-Z0-9\\.]+)";
		Comment = new Regex("#([^\\n<>=]+|\\n)");
		CommentLite = new Regex("#([^\\n]*)");
		Numbers = new Regex("(?<!:)([-]?\\b(?:\\d*\\.)?\\d+)");
		PreprocessStrings = new Regex("STR\\(\"([^\"]+)\"\\)");
		PreprocessHashes = new Regex("HASH\\(\"([^\"]+)\"\\)");
		PreprocessBinary = new Regex("\\%([01_]+)");
		PreprocessHex = new Regex("\\$([0-9A-Fa-f_]+)+");
		ScriptLine = new Regex("^([^#]*)(#.*)?$");
		Device = new Regex("(\\bdr*\\d+|db)");
		Network = new Regex("(?:[dr]\\d+|db):(\\d+)");
		Register = new Regex("(\\br+\\d+)");
		JumpReferences = new Regex("^\\s*(-?-?[a-zA-Z0-9.]+):$");
		LeadingWhitespace = new Regex("^\\s*");
		string text = "\\b(?:";
		for (int i = 0; i < ProgrammableChip.AllConstants.Length; i++)
		{
			ProgrammableChip.Constant constant = ProgrammableChip.AllConstants[i];
			if (i != 0)
			{
				text += "|";
			}
			text += constant.Literal;
		}
		text += ")\\b";
		Constants = new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	}
}
