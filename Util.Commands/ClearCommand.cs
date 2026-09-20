using System;
using Assets.Scripts;

namespace Util.Commands;

public class ClearCommand : CommandBase
{
	public override string HelpText => "Clears all text from the console buffer.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		ConsoleWindow.ClearConsole();
		return null;
	}
}
