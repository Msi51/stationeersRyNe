using System;
using System.Globalization;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;

namespace Util.Commands;

internal class LocalizationCommand : CommandBase
{
	public enum Argument : byte
	{
		None,
		WordCount,
		Generate,
		Refresh,
		CheckKeys,
		CheckFonts
	}

	private static readonly EnumCollection<Argument, byte> ArgumentTypes = new EnumCollection<Argument, byte>(toProper: false);

	public override string HelpText => "Inspects and manages localization data. Subcommands run word counts, generate .resx files for a locale, refresh strings at runtime, and check keys or fonts.";

	public override string[] Arguments => ArgumentTypes.Names;

	public override bool IsLaunchCmd => false;

	public static string CommandText => "localization";

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		switch (ArgumentTypes.Get(args[0]))
		{
		case Argument.WordCount:
			ConsoleWindow.Print($"{GetWordCount()}");
			return null;
		case Argument.Generate:
			GenerateLanguage(args);
			return null;
		case Argument.Refresh:
			Localization.Refresh();
			return null;
		case Argument.CheckKeys:
			Localization.CheckKeys();
			return null;
		case Argument.CheckFonts:
			Localization.CheckFont();
			return null;
		default:
			ConsoleWindow.Print("Number of languages: " + GetNumberOfLanguages());
			return null;
		}
	}

	private static int GetNumberOfLanguages()
	{
		return Localization.Languages.Count;
	}

	private static int GetWordCount()
	{
		Localization.LanguageData.TryGetValue(LanguageCode.EN, out var value);
		if (value != null)
		{
			return value.GetCurrentLanguageWordCount();
		}
		throw new Exception("Could not get language count");
	}

	private static void GenerateLanguage(string[] args)
	{
		if (ConsoleWindow.IsInvalidSyntax(args, 2) || !CommandBase.Get(args, 1, "locale", out string result))
		{
			return;
		}
		ConsoleWindow.PrintAction("Generating '" + result + "' language file.");
		try
		{
			if (!Enum.TryParse<LanguageCode>(result.ToUpperInvariant(), out var result2))
			{
				ConsoleWindow.PrintError("Cannot parse locale '" + result + "'.", suppressStacktrace: true);
				return;
			}
			Localization.LanguageData.TryGetValue(result2, out var value);
			new ResxGenerator(value, result).Generate();
		}
		catch (Exception arg)
		{
			ConsoleWindow.PrintError($"Error creating language file for '{result}': {arg}");
		}
	}

	private static bool TryGetArg(string[] args, int index, out string result)
	{
		result = null;
		if (args.Length < index)
		{
			return false;
		}
		result = args[index];
		return true;
	}

	private static string FilepathFor(string locale)
	{
		string text = CultureInfo.GetCultureInfo(locale).Name + ".resx";
		return Path.Join(Defines.Paths.LocalData, text);
	}
}
