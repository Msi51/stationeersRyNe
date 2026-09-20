using System;
using Assets.Scripts;
using Assets.Scripts.Util;

namespace Util.Commands;

public class AchievementsCommand : CommandBase
{
	public enum Argument : byte
	{
		None,
		List,
		Award,
		Clear
	}

	private static readonly EnumCollection<Argument, byte> ArgumentTypes = new EnumCollection<Argument, byte>(toProper: false);

	public override string HelpText => "Lists Steam achievements and their unlock state for debugging. Use 'award <name>' or 'clear <name>' to override an achievement's state.";

	public override bool IsLaunchCmd => false;

	public override string[] Arguments => ArgumentTypes.Names;

	public override string Execute(string[] args)
	{
		if (args.Length == 0)
		{
			return List();
		}
		switch (ArgumentTypes.Get(args[0]))
		{
		case Argument.List:
			return List();
		case Argument.Award:
			if (args.Length < 2)
			{
				return "Invalid syntax";
			}
			return Award(args[1]);
		case Argument.Clear:
			if (args.Length < 2)
			{
				return "Invalid syntax";
			}
			return Clear(args[1]);
		default:
			return "Invalid syntax";
		}
	}

	private string Award(string name)
	{
		if (!Enum.TryParse<Achievements.Kind>(name, out var result))
		{
			ConsoleWindow.PrintError("Achievement '" + name + "' not found", suppressStacktrace: true);
			return null;
		}
		Achievements.Achieve(result);
		ConsoleWindow.PrintAction("Awarded achievement " + name + ".");
		return "Awarded achievement " + name + ".";
	}

	private string Clear(string name)
	{
		if (!Enum.TryParse<Achievements.Kind>(name, out var result))
		{
			ConsoleWindow.PrintError("Achievement '" + name + "' not found", suppressStacktrace: true);
			return null;
		}
		Achievements.Clear(result);
		ConsoleWindow.PrintAction("Cleared achievement " + name + ".");
		return "Cleared achievement " + name + ".";
	}

	private string List()
	{
		if (Achievements.Steam.Instance == null)
		{
			ConsoleWindow.PrintError("Steam achievements not initialized.", suppressStacktrace: true);
			return null;
		}
		ConsoleWindow.PrintAction("Listing Steam achievements.");
		Achievements.Kind[] values = EnumCollections.Achievements.Values;
		foreach (Achievements.Kind achievement in values)
		{
			bool flag = Achievements.Check(achievement);
			string key = Achievements.Steam.GetKey(achievement);
			ConsoleWindow.Print($"{key} = {flag}");
		}
		Achievements.Stat[] values2 = EnumCollections.AchievementStats.Values;
		foreach (Achievements.Stat achievement2 in values2)
		{
			bool flag2 = Achievements.Check(achievement2);
			string statKey = Achievements.Steam.GetStatKey(achievement2);
			ConsoleWindow.Print($"{statKey} = {flag2}");
		}
		return $"Listed {EnumCollections.Achievements.Values.Length + EnumCollections.AchievementStats.Values.Length} Steam achievements.";
	}
}
