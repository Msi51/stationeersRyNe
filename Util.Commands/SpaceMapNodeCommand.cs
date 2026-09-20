using Assets.Scripts;
using Assets.Scripts.Util;
using Objects.Rockets;

namespace Util.Commands;

public class SpaceMapNodeCommand : CommandBase
{
	private const string ARG_ID = "<id>";

	public override string HelpText => "Prints debug info for the space-map node with the given reference id.";

	public override string[] Arguments => new string[1] { "<id>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("spacemapnode"))
		{
			return null;
		}
		int num = args.Length;
		if (num > 2 || num == 0)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "id", out int result))
		{
			return null;
		}
		SpaceMapNode spaceMapNode = Referencable.Find<SpaceMapNode>(result);
		if (spaceMapNode == null)
		{
			ConsoleWindow.PrintError("Node with referenceId " + StringManager.Get(result) + " not found.", suppressStacktrace: true);
			return null;
		}
		spaceMapNode.PrintNodeDebug();
		return null;
	}
}
