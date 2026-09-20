using Assets.Scripts;
using Weather;

namespace Util.Commands;

public class StormCommand : CommandBase
{
	public override string HelpText => "Starts or stops a weather event, or toggles the weather debug overlay. 'start' optionally takes a storm id to activate a specific event. Host or dedicated server only.";

	public override string[] Arguments => new string[1] { "<start [stormId] | stop | debug>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("storm"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		switch (args[0].ToLower())
		{
		case "start":
			if (args.Length == 2)
			{
				string stormId = args[1];
				StartStorm(stormId);
			}
			else
			{
				StartStorm();
			}
			break;
		case "stop":
			StopStorm();
			break;
		case "debug":
			WeatherManager.DrawStormDebug = !WeatherManager.DrawStormDebug;
			break;
		default:
			ConsoleWindow.PrintError("Unknown subcommand '" + args[0] + "'.", suppressStacktrace: true);
			return null;
		}
		return null;
	}

	private void StartStorm(string stormId)
	{
		ConsoleWindow.PrintAction("Started weather event.");
		WeatherManager.ImmediatelyActivateWeatherEvent(stormId);
	}

	private void StartStorm()
	{
		ConsoleWindow.PrintAction("Started weather event.");
		WeatherManager.ImmediatelyActivateWeatherEvent();
	}

	private void StopStorm()
	{
		ConsoleWindow.PrintAction("Stopped weather event.");
		WeatherManager.StopCurrentWeatherEvent();
	}
}
