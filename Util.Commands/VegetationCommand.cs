using Assets.Scripts;
using Assets.Scripts.Atmospherics;

namespace Util.Commands;

public class VegetationCommand : CommandBase
{
	public override string HelpText => "Adjusts vegetation per chunk. 'set <plantPrefab> <count>' overrides the global plant count for the named plant; 'debug' toggles the terraforming debug overlay. Host or dedicated server only.";

	public override string[] Arguments => new string[2] { "set <plantPrefab> <count>", "debug" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("vegetation"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		string text = args[0].ToLower();
		if (!(text == "debug"))
		{
			if (text == "set")
			{
				if (args.Length < 3)
				{
					return "Invalid syntax";
				}
				GlobalPlant globalPlant = null;
				foreach (GlobalPlant globalPlant2 in TerraForming.GlobalPlants)
				{
					if (globalPlant2.PlantPrefab.PrefabName == args[1])
					{
						globalPlant = globalPlant2;
					}
				}
				if (globalPlant == null)
				{
					ConsoleWindow.PrintError("Invalid clutter id '" + args[1] + "'.", suppressStacktrace: true);
					return null;
				}
				if (!CommandBase.Get(args, 2, "count", out int result))
				{
					return null;
				}
				globalPlant.Count = result;
				return $"Set '{args[1]}' count to {result}.";
			}
			ConsoleWindow.PrintError("Unknown vegetation subcommand '" + args[0] + "'.", suppressStacktrace: true);
			return null;
		}
		TerraForming.DrawDebug = !TerraForming.DrawDebug;
		return $"Vegetation debug: {TerraForming.DrawDebug}.";
	}
}
