using System;
using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

public class WorldObjectiveCommand : CommandBase
{
	public override string HelpText => "Dev tool for testing world objectives. Sets the chosen state on either every active objective ('all') or one specific objective by id-hash string.";

	public override string[] Arguments => new string[1] { "<dismiss | complete | trigger> <all | objectiveId>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("helperhints"))
		{
			return null;
		}
		if (args.Length < 2)
		{
			return "Invalid syntax";
		}
		string text = args[0].ToLowerInvariant();
		if (text != "dismiss" && text != "complete" && text != "trigger")
		{
			ConsoleWindow.PrintError("unknown action '" + args[0] + "', expected dismiss | complete | trigger", suppressStacktrace: true);
			return null;
		}
		string text2 = args[1];
		bool flag = string.Equals(text2, "all", StringComparison.OrdinalIgnoreCase);
		int num = ((!flag) ? Animator.StringToHash(text2) : 0);
		int num2 = 0;
		foreach (WorldObjectiveState currentWorldObjective in WorldObjectiveState.CurrentWorldObjectives)
		{
			if (flag || currentWorldObjective.WorldObjective.IdHash == num)
			{
				Apply(currentWorldObjective, text);
				num2++;
			}
		}
		if (num2 == 0)
		{
			ConsoleWindow.PrintError("no objective matched '" + text2 + "'", suppressStacktrace: true);
			return null;
		}
		return $"{text} applied to {num2} objective(s).";
	}

	private static void Apply(WorldObjectiveState objective, string action)
	{
		switch (action)
		{
		case "dismiss":
			objective.SetDismissed(value: true);
			break;
		case "complete":
			objective.Completed = true;
			break;
		case "trigger":
			objective.Triggered = true;
			break;
		}
	}
}
