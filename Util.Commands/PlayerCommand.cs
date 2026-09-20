using Assets.Scripts;
using Assets.Scripts.Objects.Entities;

namespace Util.Commands;

public class PlayerCommand : CommandBase
{
	public override string HelpText => "Inspects or modifies player character state. 'list' prints all human display names; the 'set...' subcommands take a player name and a numeric value. Server only.";

	public override string[] Arguments => new string[5] { "list", "setmood <playerName> <value>", "sethygiene <playerName> <value>", "setnutrition <playerName> <value>", "sethydration <playerName> <value>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("player"))
		{
			return null;
		}
		if (!GameManager.RunSimulation)
		{
			return "Can only be run on the server";
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		switch (args[0].ToLower())
		{
		case "list":
			foreach (Human allHuman in Human.AllHumans)
			{
				ConsoleWindow.Print(allHuman.DisplayName.ToLower());
			}
			return null;
		case "setmood":
		{
			if (args.Length < 3)
			{
				return "Invalid syntax";
			}
			if (!CommandBase.Get(args, 2, "value", out float result4))
			{
				return null;
			}
			foreach (Human allHuman2 in Human.AllHumans)
			{
				if (string.Equals(allHuman2.DisplayName.ToLower(), args[1].ToLower()))
				{
					allHuman2.Mood = result4;
					return $"Setting mood of {allHuman2.DisplayName} to {result4}.";
				}
			}
			ConsoleWindow.PrintError("Player " + args[1] + " not found.", suppressStacktrace: true);
			return null;
		}
		case "sethygiene":
		{
			if (args.Length < 3)
			{
				return "Invalid syntax";
			}
			if (!CommandBase.Get(args, 2, "value", out float result2))
			{
				return null;
			}
			foreach (Human allHuman3 in Human.AllHumans)
			{
				if (string.Equals(allHuman3.DisplayName.ToLower(), args[1].ToLower()))
				{
					allHuman3.Hygiene = result2;
					return $"Setting hygiene of {allHuman3.DisplayName} to {result2}.";
				}
			}
			ConsoleWindow.PrintError("Player " + args[1] + " not found.", suppressStacktrace: true);
			return null;
		}
		case "setnutrition":
		{
			if (args.Length < 3)
			{
				return "Invalid syntax";
			}
			if (!CommandBase.Get(args, 2, "value", out float result3))
			{
				return null;
			}
			foreach (Human allHuman4 in Human.AllHumans)
			{
				if (string.Equals(allHuman4.DisplayName.ToLower(), args[1].ToLower()))
				{
					allHuman4.Nutrition = result3;
					return $"Setting nutrition of {allHuman4.DisplayName} to {result3}.";
				}
			}
			ConsoleWindow.PrintError("Player " + args[1] + " not found.", suppressStacktrace: true);
			return null;
		}
		case "sethydration":
		{
			if (args.Length < 3)
			{
				return "Invalid syntax";
			}
			if (!CommandBase.Get(args, 2, "value", out float result))
			{
				return null;
			}
			foreach (Human allHuman5 in Human.AllHumans)
			{
				if (string.Equals(allHuman5.DisplayName.ToLower(), args[1].ToLower()))
				{
					allHuman5.Hydration = result;
					return $"Setting hydration of {allHuman5.DisplayName} to {result}.";
				}
			}
			ConsoleWindow.PrintError("Player " + args[1] + " not found.", suppressStacktrace: true);
			return null;
		}
		default:
			return "Invalid syntax";
		}
	}
}
