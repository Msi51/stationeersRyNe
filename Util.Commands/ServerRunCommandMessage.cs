using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;

namespace Util.Commands;

internal class ServerRunCommandMessage : ProcessedMessage<ServerRunCommandMessage>
{
	public ulong ClientId { get; set; }

	public string Secret { get; set; }

	public string Command { get; set; }

	public override void Process(long hostId)
	{
		if (string.IsNullOrEmpty(Settings.CurrentData.ServerAuthSecret))
		{
			ConsoleWindow.PrintError("serverrun command can only be used if a ServerAuthSecret is set in setting.xml", suppressStacktrace: true);
			return;
		}
		Client client = NetworkBase.Clients.Find((Client x) => x.ClientId == ClientId);
		if (client == null)
		{
			ConsoleWindow.PrintError(string.Format("{0} '{1}' tried to run a command but is not found.", "ClientId", ClientId), suppressStacktrace: true);
		}
		else if (Secret != Settings.CurrentData.ServerAuthSecret)
		{
			ConsoleWindow.PrintError("invalid ServerAuthSecret. client '" + client.ToStringNameAndId() + "' tried to run command '" + Command + "'", suppressStacktrace: true);
		}
		else
		{
			ConsoleWindow.PrintAction("client '" + client.ToStringNameAndId() + "' ran command '" + Command + "'");
			CommandLine.Process(Command);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ClientId = reader.ReadUInt64();
		Secret = reader.ReadString();
		Command = reader.ReadString();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteUInt64(ClientId);
		writer.WriteString(Secret);
		writer.WriteString(Command);
	}
}
