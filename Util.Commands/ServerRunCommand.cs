using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;

namespace Util.Commands;

internal class ServerRunCommand : CommandBase
{
	public override string HelpText => "Sends a console command to the server for execution. Client-only; requires Settings.ServerAuthSecret to match between client and server.";

	public override string[] Arguments => new string[1] { "<command...>" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (NetworkManager.IsClient)
		{
			SendMessageToServer(string.Join(" ", args));
		}
		else
		{
			ConsoleWindow.PrintError("Only clients can use this command.", suppressStacktrace: true);
		}
		return null;
	}

	private static void SendMessageToServer(string command)
	{
		NetworkClient.SendToServer(new ServerRunCommandMessage
		{
			ClientId = NetworkManager.LocalClientId,
			Secret = Settings.CurrentData.ServerAuthSecret,
			Command = command
		});
	}
}
