using DLC;

namespace Util.Commands;

public class DLCCommand : CommandBase
{
	public override string HelpText => "Provides DLC debug functions. Host or singleplayer only.";

	public override string[] Arguments => new string[1] { "shared : print the shared (server-union) owned DLC" };

	public override string HelpTextSeparator => "\n";

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("dlc"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (args[0].ToLowerInvariant() == "shared")
		{
			return HandleShared();
		}
		return "Invalid syntax";
	}

	private string HandleShared()
	{
		return ((DLCType)SharedDLCManager.SharedDLC/*cast due to constrained. prefix*/).ToString();
	}
}
