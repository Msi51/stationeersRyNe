using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Util.Commands;

public class OrbitalCommand : CommandBase
{
	private const string ARG_DEBUG = "debug";

	private const string ARG_VIEW = "view";

	private const string ARG_SIMULATE = "simulate";

	private const string ARG_SET = "set";

	private const string ARG_CELESTIALS = "celestials";

	private const string ARG_TIMESCALE = "timescale";

	private const string ARG_MAKE_OFFSET = "makeoffset";

	public override string HelpText => "Controls orbital simulation. With no arguments prints the current state. Time arguments accept spans of " + EnumCollections.SimulationSpans.PrintAll("<", ">") + ". Mutating subcommands cannot be run as a client.";

	public override string[] Arguments => new string[7] { "debug", "view", "celestials", "simulate <amount> [span]", "set <amount> [span]", "timescale <scale>", "makeoffset" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("orbital"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			OrbitalSimulation.PrintDebug();
			return null;
		}
		return args[0] switch
		{
			"debug" => Debug(args), 
			"view" => View(args), 
			"simulate" => Simulate(args), 
			"set" => Set(args), 
			"celestials" => List(args), 
			"timescale" => TimeScale(args), 
			"makeoffset" => MakeOffset(args), 
			_ => "Invalid syntax", 
		};
	}

	private string MakeOffset(string[] args)
	{
		if (IsInvalidArguments(args, 2))
		{
			return null;
		}
		TimeLength timeLength = TimeLength.FromSeconds(OrbitalSimulation.MakeOffset());
		ConsoleWindow.PrintAction("Copied offset for " + timeLength.ToString() + " to clipboard.");
		TimeSpanReference timeSpanReference = new TimeSpanReference
		{
			Days = timeLength.Days,
			Hours = timeLength.Hours,
			Minutes = timeLength.Minutes,
			Seconds = timeLength.Seconds
		};
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<TimeOffset ");
		if (timeSpanReference.Days > 0.0)
		{
			stringBuilder.Append($"Days=\"{timeSpanReference.Days}\" ");
		}
		if (timeSpanReference.Hours > 0.0)
		{
			stringBuilder.Append($"Hours=\"{timeSpanReference.Hours}\" ");
		}
		if (timeSpanReference.Minutes > 0.0)
		{
			stringBuilder.Append($"Minutes=\"{timeSpanReference.Minutes}\" ");
		}
		if (timeSpanReference.Seconds > 0.0)
		{
			stringBuilder.Append($"Seconds=\"{timeSpanReference.Seconds}\" ");
		}
		stringBuilder.Append("/>");
		GUIUtility.systemCopyBuffer = stringBuilder.ToString();
		return null;
	}

	private string TimeScale(string[] args)
	{
		if (IsInvalidArguments(args, 3))
		{
			return null;
		}
		if (NetworkManager.IsActiveAsClient)
		{
			ConsoleWindow.PrintError("Cannot set timescale as client.", suppressStacktrace: true);
			return null;
		}
		if (!float.TryParse(args[1], out var result))
		{
			ConsoleWindow.PrintError("timescale must be a float.", suppressStacktrace: true);
			return null;
		}
		OrbitalSimulation.SetTimeScale(result);
		return null;
	}

	private string List(string[] args)
	{
		OrbitalSimulation.PrintList();
		return null;
	}

	private bool IsInvalidArguments(string[] args, int maxLength, int minLength = 0)
	{
		if (args.Length < minLength)
		{
			ConsoleWindow.PrintError("Too few arguments for '" + args[0] + "'.", suppressStacktrace: true);
			return true;
		}
		if (args.Length > maxLength)
		{
			ConsoleWindow.PrintError("Too many arguments for '" + args[0] + "'.", suppressStacktrace: true);
			return true;
		}
		return false;
	}

	private string Simulate(string[] args)
	{
		if (NetworkManager.IsActiveAsClient)
		{
			ConsoleWindow.PrintError("Cannot simulate time as client.", suppressStacktrace: true);
			return null;
		}
		if (IsInvalidArguments(args, 3, 2))
		{
			return null;
		}
		if (!double.TryParse(args[1], out var result))
		{
			ConsoleWindow.PrintError("Invalid argument '" + args[1] + "' for 'simulate'.", suppressStacktrace: true);
			return null;
		}
		SimulationSpan result2 = SimulationSpan.Seconds;
		if (args.Length == 3 && !Enum.TryParse<SimulationSpan>(args[2], ignoreCase: true, out result2))
		{
			ConsoleWindow.PrintError("Invalid simulation span '" + args[2] + "' for 'simulate'.", suppressStacktrace: true);
			return null;
		}
		ConsoleWindow.PrintAction($"Simulating {result} {EnumCollections.SimulationSpans.GetName(result2)} for celestials.");
		result = ChangeTimeValue(result2, result);
		OrbitalSimulation.SimulateTime(result);
		return null;
	}

	private string Set(string[] args)
	{
		if (NetworkManager.IsActiveAsClient)
		{
			ConsoleWindow.PrintError("Cannot set time as client.", suppressStacktrace: true);
			return null;
		}
		if (IsInvalidArguments(args, 3, 2))
		{
			return null;
		}
		if (!double.TryParse(args[1], out var result))
		{
			ConsoleWindow.PrintError("Invalid argument '" + args[1] + "' for 'set'.", suppressStacktrace: true);
			return null;
		}
		SimulationSpan result2 = SimulationSpan.Seconds;
		if (args.Length == 3 && !Enum.TryParse<SimulationSpan>(args[2], ignoreCase: true, out result2))
		{
			ConsoleWindow.PrintError("Invalid simulation span '" + args[2] + "' for 'set'.", suppressStacktrace: true);
			return null;
		}
		ConsoleWindow.PrintAction($"Set {result} {EnumCollections.SimulationSpans.GetName(result2)} for celestials.");
		result = ChangeTimeValue(result2, result);
		OrbitalSimulation.SetRealTime(result, publish: true);
		return null;
	}

	private double ChangeTimeValue(SimulationSpan span, double result)
	{
		switch (span)
		{
		case SimulationSpan.Minutes:
			result *= 60.0;
			break;
		case SimulationSpan.Hours:
			result *= 3600.0;
			break;
		case SimulationSpan.Days:
			result *= 86400.0;
			break;
		case SimulationSpan.Weeks:
			result *= 604800.0;
			break;
		case SimulationSpan.Months:
			result *= 2592000.0;
			break;
		case SimulationSpan.Years:
			result *= 31104000.0;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case SimulationSpan.Seconds:
			break;
		}
		return result;
	}

	private string View(string[] args)
	{
		if (IsInvalidArguments(args, 2))
		{
			return null;
		}
		SkyBoxController.OrbitDrawMode = ((SkyBoxController.OrbitDrawMode != OrbitDrawMode.InWorld) ? OrbitDrawMode.InWorld : OrbitDrawMode.None);
		return null;
	}

	private string Debug(string[] args)
	{
		if (IsInvalidArguments(args, 2))
		{
			return null;
		}
		OrbitalSimulation.OrbitalDebugger = !OrbitalSimulation.OrbitalDebugger;
		return null;
	}
}
