using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;

namespace Util.Commands;

public class SayCommand : CommandBase
{
	public override string HelpText => "Sends a chat message to all connected players. Multiplayer only; the host appears tagged as '(Host)'.";

	public override string[] Arguments => new string[1] { "<message...>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.MultiplayerOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("say"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		Say(string.Join(" ", args));
		return null;
	}

	private static void Say(string input)
	{
		if (!string.IsNullOrEmpty(input))
		{
			ChatMessage chatMessage = new ChatMessage
			{
				ChatText = input,
				DisplayName = (GameManager.IsBatchMode ? "Server" : Human.LocalHuman.DisplayName),
				HumanId = (GameManager.IsBatchMode ? (-1) : Human.LocalHuman.ReferenceId)
			};
			if (NetworkManager.IsServer && !GameManager.IsBatchMode)
			{
				chatMessage.DisplayName += " (Host)";
			}
			if ((bool)Human.LocalHuman && !input.Contains("hi") && !input.Contains("hello"))
			{
				input.Contains("wave");
			}
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(chatMessage);
			}
			else if (NetworkManager.IsServer)
			{
				chatMessage.PrintToConsole();
				NetworkServer.SendToClients(chatMessage, NetworkChannel.GeneralTraffic, -1L);
			}
			else
			{
				ConsoleWindow.PrintError("Unable to send message.", suppressStacktrace: true);
			}
		}
	}
}
