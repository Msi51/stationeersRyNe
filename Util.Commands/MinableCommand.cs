using Assets.Scripts;
using Assets.Scripts.Localization2;
using TerrainSystem;

namespace Util.Commands;

public class MinableCommand : CommandBase
{
	public override string HelpText => "Toggles the ImGui minable vein debug visualiser, or sets its render range when invoked with 'range <distance>'.";

	public override string[] Arguments => new string[1] { "[range <distance>]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("minable"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			Vein.IsDrawDebug = !Vein.IsDrawDebug;
			Assets.Scripts.Localization2.GameString arg = (Vein.IsDrawDebug ? GameStrings.EnabledLower : GameStrings.DisabledLower);
			ConsoleWindow.PrintAction($"Minable visualiser {arg}.");
			return null;
		}
		if (args.Length == 2)
		{
			if (args[0] == "range")
			{
				if (!CommandBase.Get(args, 1, "distance", out int result))
				{
					return null;
				}
				Vein.DebugRenderRange = result;
				ConsoleWindow.PrintAction($"Debug minables render distance set to {result}.");
				return null;
			}
			return "Invalid syntax";
		}
		return "Invalid syntax";
	}
}
