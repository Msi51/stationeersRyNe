using Assets.Scripts;

namespace Util.Commands;

public class KickCommand : CommandBase
{
	public override string HelpText => "Disconnects a client from the server. Host or dedicated server only.";

	public override string[] Arguments => new string[1] { "<clientId>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.HostOrSinglePlayer | CommandScope.MultiplayerOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("kick"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "clientId", out ulong result))
		{
			return null;
		}
		Client client = Client.Find(result);
		if (client == null)
		{
			ConsoleWindow.PrintError($"Client '{result}' not found.", suppressStacktrace: true);
			return null;
		}
		client.Disconnect();
		return "Client '" + client.name + "' kicked from the server.";
	}
}
