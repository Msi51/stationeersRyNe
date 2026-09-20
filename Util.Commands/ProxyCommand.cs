using Assets.Scripts;

namespace Util.Commands;

internal class ProxyCommand : CommandBase
{
	private const string ARG_ENABLE = "enable";

	private const string ARG_DISABLE = "disable";

	private const string ARG_DRAW = "draw";

	public override string HelpText => "Distant-structure proxy debug. No args: print stats. 'enable'/'disable': toggle the system. 'draw': toggle the wireframe overlay (Scene view). Not supported in dedicated server batch mode.";

	public override string[] Arguments => new string[3] { "enable", "disable", "draw" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("proxy"))
		{
			return null;
		}
		OcclusionManager instance = OcclusionManager.Instance;
		if (instance == null)
		{
			ConsoleWindow.PrintError("OcclusionManager is not available.", suppressStacktrace: true);
			return null;
		}
		if (args.Length < 1)
		{
			instance.PrintProxyDebugReport();
			return null;
		}
		switch (args[0])
		{
		case "enable":
			instance.SetDistantProxyEnabled(enabled: true);
			return "Distant proxy rendering enabled.";
		case "disable":
			instance.SetDistantProxyEnabled(enabled: false);
			return "Distant proxy rendering disabled.";
		case "draw":
			return "Distant proxy debug draw " + (instance.ToggleDistantProxyDebugDraw() ? "enabled (Scene view)" : "disabled") + ".";
		default:
			return "Invalid syntax";
		}
	}
}
