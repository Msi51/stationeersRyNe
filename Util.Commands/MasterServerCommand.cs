using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Networking.GameSessions;

namespace Util.Commands;

internal class MasterServerCommand : CommandBase
{
	public override string HelpText => "Interacts with the master server. With no arguments, prints the current NetConfig; pass 'refresh' to re-register the current session with the master server using the latest settings.";

	public override string[] Arguments => new string[1] { "[refresh]" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		switch (args.Length)
		{
		case 0:
			return NetworkManager.Config.ToString();
		case 1:
			SingleArgument(args[0]);
			return null;
		default:
			return "Invalid syntax";
		}
	}

	private static void SingleArgument(string arg)
	{
		if (arg == "refresh")
		{
			NetworkManager.StartSession(new GameSessionConfig
			{
				gameName = Settings.CurrentData.ServerName,
				password = !string.IsNullOrEmpty(Settings.CurrentData.ServerPassword),
				maxPlayers = Settings.CurrentData.ServerMaxPlayers,
				port = ushort.Parse(Settings.CurrentData.GamePort),
				mapName = WorldManager.CurrentWorldName,
				ipAddress = NetworkManager.CurrentTransport.PublicIp,
				SteamId = (Settings.CurrentData.UseSteamP2P ? GameManager.GetSteamId() : 0)
			});
		}
		else
		{
			ConsoleWindow.PrintError("Unknown argument '" + arg + "'.", suppressStacktrace: true);
		}
	}
}
