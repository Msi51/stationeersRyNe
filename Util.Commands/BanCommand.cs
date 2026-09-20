using System;
using Assets.Scripts;

namespace Util.Commands;

internal class BanCommand : CommandBase
{
	public override string HelpText => "Bans a client from the server. Pass a clientId to ban a specific player (adds them to the blacklist file even if they are offline). Pass 'refresh' to reload the blacklist from disk.";

	public override string[] Arguments => new string[1] { "<clientId | refresh>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.HostOrSinglePlayer | CommandScope.MultiplayerOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("ban"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (string.Equals(args[0], "refresh", StringComparison.OrdinalIgnoreCase))
		{
			NetworkServer.LoadBlacklist();
			return "Reloaded blacklist from '" + BlacklistedClient.PATH + "'.";
		}
		if (!ulong.TryParse(args[0], out var result))
		{
			ConsoleWindow.PrintError("'" + args[0] + "' is not a valid clientId.", suppressStacktrace: true);
			return null;
		}
		Client client = Client.Find(result);
		if (client != null)
		{
			client.Ban();
			return "Client '" + client.name + "' banned from the server.";
		}
		NetworkServer.AddToBlacklist(result);
		return $"Client '{result}' added to the blacklist.";
	}
}
