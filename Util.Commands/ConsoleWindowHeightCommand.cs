using Assets.Scripts;

namespace Util.Commands;

public class ConsoleWindowHeightCommand : CommandBase
{
	public override string HelpText => "Sets the console window height to a fixed number of lines, or resets it to default behaviour.";

	public override string[] Arguments => new string[1] { "<height | reset | r>" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length != 1)
		{
			return "Invalid syntax";
		}
		if (args[0] == "reset" || args[0] == "r")
		{
			ConsoleWindow.UseCustomWindowHeight(useCustom: false);
			return null;
		}
		if (int.TryParse(args[0], out var result))
		{
			ConsoleWindow.UseCustomWindowHeight(useCustom: true, result);
			return null;
		}
		ConsoleWindow.PrintError("Invalid argument; must be 'reset', 'r', or an integer.", suppressStacktrace: true);
		return null;
	}
}
