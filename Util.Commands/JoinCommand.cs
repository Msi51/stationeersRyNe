using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

public class JoinCommand : CommandBase
{
	public override string HelpText => "Connects to a multiplayer server at the given address and port.";

	public override string[] Arguments => new string[1] { "<address:port>" };

	public override bool IsLaunchCmd => true;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		NetworkClient networkClient = Object.FindObjectOfType<NetworkClient>();
		if (networkClient == null)
		{
			ConsoleWindow.PrintError("network client not initialised yet", suppressStacktrace: true);
			return null;
		}
		networkClient.JoinClientFromMenu(args[0]);
		return "Connecting to " + args[0] + "...";
	}
}
