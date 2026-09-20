using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class LiquidCommands : CommandBase
{
	public override string HelpText => "Toggles debug functions for the liquid solver. 'show' toggles the overlay, 'renderer' toggles liquid rendering, 'solver' toggles the solver itself, and 'WorldVolume' prints world-volume multipliers per liquid gas type.";

	public override string[] Arguments => new string[1] { "<show | renderer | solver | WorldVolume>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("liquid"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "type", out string result))
		{
			return null;
		}
		switch (result.ToLower())
		{
		case "show":
		{
			ImGuiAtmosphericDebug.DebugLiquidAtmosphereOverlay = !ImGuiAtmosphericDebug.DebugLiquidAtmosphereOverlay;
			string text3 = (ImGuiAtmosphericDebug.DebugLiquidAtmosphereOverlay ? "enabled" : "disabled");
			return "Liquid debug visualiser is " + text3 + ".";
		}
		case "renderer":
		{
			LiquidSolver.RenderingEnabled = !LiquidSolver.RenderingEnabled;
			string text2 = (LiquidSolver.RenderingEnabled ? "enabled" : "disabled");
			return "Liquid renderer is " + text2 + ".";
		}
		case "solver":
		{
			LiquidSolver.SolverEnabled = !LiquidSolver.SolverEnabled;
			string text = (LiquidSolver.SolverEnabled ? "enabled" : "disabled");
			return "Liquid solver is " + text + ".";
		}
		case "worldvolume":
			PrintWorldVolumeMultiplier();
			return null;
		default:
			ConsoleWindow.PrintError("Unknown option '" + args[0] + "'.", suppressStacktrace: true);
			return null;
		}
	}

	public void PrintWorldVolumeMultiplier()
	{
		ConsoleWindow.Print("GasType World Volume Multipliers");
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Liquid)
			{
				double num = MoleHelper.CalculateWorldVolumeMultiplier(gasType);
				ConsoleWindow.Print($"{gasType}: {num}");
			}
		}
	}
}
