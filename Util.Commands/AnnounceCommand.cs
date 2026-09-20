using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Util.Commands;

public class AnnounceCommand : CommandBase
{
	public override string HelpText => "Displays a popup announcement on all connected clients. Multiplayer only; can only be sent by the host or a dedicated server.";

	public override string[] Arguments => new string[1] { "<message...>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.HostOrSinglePlayer | CommandScope.MultiplayerOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("announce"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		AnnounceMessage obj = new AnnounceMessage
		{
			AnnounceText = string.Join(" ", args)
		};
		NetworkServer.SendToClients(obj, NetworkChannel.GeneralTraffic, -1L);
		AnnounceMessage.Display(obj.AnnounceText);
		return null;
	}
}
