using Assets.Scripts;

namespace Util.Commands;

public class VersionCommand : CommandBase
{
	public override string HelpText => "Prints the current game version.";

	public override string[] Arguments => null;

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		return GameManager.GetGameVersion();
	}
}
