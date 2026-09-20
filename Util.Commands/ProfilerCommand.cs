using Assets.Scripts;

namespace Util.Commands;

public class ProfilerCommand : CommandBase
{
	public override string HelpText => "Enables or disables the in-game thread profiler overlay.";

	public override string[] Arguments => new string[1] { "<enable | disable>" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		string text = args[0].ToLower();
		if (!(text == "enable"))
		{
			if (text == "disable")
			{
				ThreadProfiler.ShowInfo = false;
				return "Profiler disabled.";
			}
			ConsoleWindow.PrintError("Unknown option '" + args[0] + "', expected 'enable' or 'disable'.", suppressStacktrace: true);
			return null;
		}
		ThreadProfiler.ShowInfo = true;
		return "Profiler enabled.";
	}
}
