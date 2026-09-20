using System;
using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

public class QuitCommand : CommandBase
{
	public override string HelpText => "Immediately quits the game without prompting or saving.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		ConsoleWindow.PrintAction("Quitting the game.");
		Application.Quit();
		return null;
	}
}
