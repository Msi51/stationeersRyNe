using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class TerrainCommands : CommandBase
{
	public override string HelpText => "Toggles terrain debug overlays. 'debug' toggles the global terrain debug view; 'cursor' toggles the cursor-position terrain readout.";

	public override string[] Arguments => new string[1] { "<debug | cursor>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("terrain"))
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
		result = result.ToLower();
		if (!(result == "cursor"))
		{
			if (result == "debug")
			{
				ImguiTerrainDebug.DebugTerrain = !ImguiTerrainDebug.DebugTerrain;
				return $"Terrain debug: {ImguiTerrainDebug.DebugTerrain}.";
			}
			ConsoleWindow.PrintError("Unknown terrain debug type '" + result + "'.", suppressStacktrace: true);
			return null;
		}
		ImguiTerrainDebug.DebugCursorTerrain = !ImguiTerrainDebug.DebugCursorTerrain;
		return $"Cursor terrain debug: {ImguiTerrainDebug.DebugCursorTerrain}.";
	}
}
