using System;
using System.Text;
using Assets.Scripts.Util;
using Steamworks;

namespace Util.Commands;

public class SteamCommand : CommandBase
{
	private enum SteamCommandType
	{
		Refresh,
		Store,
		Achieve,
		Clear,
		ClearAll,
		IncreaseAchievementValue,
		SetAchievementValue,
		Invalid
	}

	public override string HelpText => "Tests the Steamworks integration. With no arguments, prints initialisation status, DLC, and achievement state. Subcommands manage achievements/stats. Subcommands taking arguments are editor-only.";

	public override string[] Arguments { get; } = Enum.GetNames(typeof(SteamCommandType));

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			args[i] = args[i].ToLower();
		}
		return "";
	}

	private static string ExecuteSingleArgumentCommand(string arg)
	{
		switch (ParseCommand(arg))
		{
		case SteamCommandType.Refresh:
			SteamUserStats.RequestCurrentStats();
			return "Stats refreshed.";
		case SteamCommandType.Store:
			Achievements.Steam.TryForceAchievementUpdate().Forget();
			return "Stats stored.";
		case SteamCommandType.ClearAll:
			SteamUserStats.ResetAll(includeAchievements: true);
			Achievements.ClearAll();
			return "Cleared all achievements.";
		default:
			return "Invalid arguments.";
		}
	}

	private string ExecuteMultiArgumentCommand(string[] args)
	{
		switch (ParseCommand(args[0]))
		{
		case SteamCommandType.Achieve:
			return ExecuteAchieveCommand(args[1]);
		case SteamCommandType.Clear:
			return ExecuteClearCommand(args[1]);
		case SteamCommandType.IncreaseAchievementValue:
			if (args.Length == 3)
			{
				return ExecuteIncreaseCommand(args[1], args[2]);
			}
			return "Invalid arguments.";
		case SteamCommandType.SetAchievementValue:
			if (args.Length == 3)
			{
				return ExecuteSetCommand(args[1], args[2]);
			}
			return "Invalid arguments.";
		default:
			return "Invalid arguments.";
		}
	}

	private string ExecuteIncreaseCommand(string achievement, string value)
	{
		if (!Enum.TryParse<Achievements.Stat>(achievement, ignoreCase: true, out var result))
		{
			return "Invalid achievement.";
		}
		if (int.TryParse(value, out var result2) && result != Achievements.Stat.TotalCreditsFromSales)
		{
			Achievements.Increment(result, result2);
			return "Achievement value increased.";
		}
		if (float.TryParse(value, out var result3))
		{
			Achievements.Increment(result, result3);
			return "Achievement value increased.";
		}
		return "Invalid argument: " + value + ". Argument must be a number.";
	}

	private string ExecuteSetCommand(string achievement, string value)
	{
		if (!Enum.TryParse<Achievements.Stat>(achievement, ignoreCase: true, out var result))
		{
			return "Invalid achievement.";
		}
		if (int.TryParse(value, out var result2))
		{
			Achievements.Steam.Instance.SetAchievementValue(result, result2);
			return "Achievement value set.";
		}
		if (float.TryParse(value, out var result3))
		{
			Achievements.Steam.Instance.SetAchievementValue(result, result3);
			return "Achievement value set.";
		}
		return "Invalid argument: " + value + ". Argument must be a number.";
	}

	private string ExecuteAchieveCommand(string achievement)
	{
		if (!Enum.TryParse<Achievements.Kind>(achievement, ignoreCase: true, out var result))
		{
			return "Invalid achievement.";
		}
		Achievements.Steam.Instance.Achieve(result);
		return "Achievement unlocked.";
	}

	private string ExecuteClearCommand(string achievement)
	{
		if (Enum.TryParse<Achievements.Kind>(achievement, ignoreCase: true, out var result))
		{
			Achievements.Steam.Instance.Clear(result);
			return "Achievement cleared.";
		}
		if (Enum.TryParse<Achievements.Stat>(achievement, ignoreCase: true, out var result2))
		{
			Achievements.Steam.Instance.SetAchievementValue(result2, 0);
			Achievements.Steam.Instance.SetAchievementValue(result2, 0f);
			Achievements.Steam.Instance.Clear(result2);
			return "Achievement cleared.";
		}
		return "Invalid achievement.";
	}

	private static SteamCommandType ParseCommand(string command)
	{
		return command.ToUpper() switch
		{
			"REFRESH" => SteamCommandType.Refresh, 
			"STORE" => SteamCommandType.Store, 
			"ACHIEVE" => SteamCommandType.Achieve, 
			"CLEAR" => SteamCommandType.Clear, 
			"CLEARALL" => SteamCommandType.ClearAll, 
			"INCREASEACHIEVEMENTVALUE" => SteamCommandType.IncreaseAchievementValue, 
			"SETACHIEVEMENTVALUE" => SteamCommandType.SetAchievementValue, 
			_ => SteamCommandType.Invalid, 
		};
	}

	private string CheckSteamInitialization()
	{
		if (!SteamClient.IsValid)
		{
			return "IsInitialised: false";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("IsInitialised: true");
		stringBuilder.AppendLine($"SteamId: {SteamClient.SteamId}");
		stringBuilder.AppendLine("DLC:\n");
		stringBuilder.AppendLine($"\tDLC ZrilianSpeciesPack: {SteamApps.IsDlcInstalled(1038400u)}");
		stringBuilder.AppendLine($"\tDLC RobotSpeciesPack: {SteamApps.IsDlcInstalled(1038500u)}");
		stringBuilder.AppendLine("Achievements:\n");
		foreach (Achievements.Kind value in Enum.GetValues(typeof(Achievements.Kind)))
		{
			stringBuilder.AppendLine($"\tAchievement {value}: {Achievements.Check(value)}");
		}
		return stringBuilder.ToString();
	}
}
