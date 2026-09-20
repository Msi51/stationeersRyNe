using System.Text;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class DebugThreadsCommand : CommandBase
{
	public override string HelpText => "Toggles the worker-thread profiling overlay (game tick or terrain). Dev tool.";

	public override string[] Arguments => new string[1] { "<GameTick | Terrain>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("debugthreads"))
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (args.Length == 0)
		{
			HelpCommand.PrintAll();
		}
		else
		{
			switch (args[0])
			{
			case "GameTick":
			case "gametick":
				ImGuiProfiler.Enabled = !ImGuiProfiler.Enabled;
				if (!ImGuiProfiler.Enabled)
				{
					ImGuiProfiler.Clear();
				}
				break;
			case "Terrain":
			case "terrain":
				TerrainDebugHelper.DebugTerrainThreads = !TerrainDebugHelper.DebugTerrainThreads;
				break;
			}
		}
		return stringBuilder.ToString();
	}
}
