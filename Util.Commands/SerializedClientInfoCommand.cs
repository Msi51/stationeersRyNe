using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Util.Commands;

public class SerializedClientInfoCommand : CommandBase
{
	private static readonly List<ulong> CachedClientIds = new List<ulong>();

	public override string HelpText => "Lists or removes serialized client info entries. Run 'list' to populate the index cache, then 'remove <index>' to delete a specific entry. The cache is cleared after each remove and must be repopulated.";

	public override string[] Arguments => new string[2] { "list", "remove <index>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.HostOrSinglePlayer | CommandScope.MultiplayerOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("clientinfo"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return "Invalid syntax";
		}
		string text = args[0].ToLower();
		if (!(text == "list"))
		{
			if (text == "remove")
			{
				if (args.Length < 2)
				{
					return "Invalid syntax";
				}
				if (CachedClientIds.Count == 0)
				{
					ConsoleWindow.PrintError("Cache is empty. Run 'clientinfo list' first.", suppressStacktrace: true);
					return null;
				}
				if (!int.TryParse(args[1], out var result))
				{
					CachedClientIds.Clear();
					ConsoleWindow.PrintError("Invalid index '" + args[1] + "'. Cache cleared, run 'list' again.", suppressStacktrace: true);
					return null;
				}
				if (result < 1 || result > CachedClientIds.Count)
				{
					CachedClientIds.Clear();
					ConsoleWindow.PrintError($"Invalid index {result}. Must be between 1 and {CachedClientIds.Count}. Cache cleared, run 'list' again.", suppressStacktrace: true);
					return null;
				}
				ulong num = CachedClientIds[result - 1];
				CachedClientIds.Clear();
				if (GameManager.ClientInfo.Remove(num))
				{
					return $"Removed entry {result} (ClientID: {num}). Cache cleared, run 'list' again.";
				}
				return $"Entry {result} (ClientID: {num}) was not found in the live list (it may have been removed by another process). Cache cleared, run 'list' again.";
			}
			return "Invalid syntax";
		}
		CachedClientIds.Clear();
		List<KeyValuePair<ulong, SerializedClientInfo>> list = GameManager.ClientInfo.ToList();
		List<Client> source = NetworkBase.Clients.ToList();
		Client hostClient = NetworkManager.HostClient;
		if (list.Count == 0)
		{
			ConsoleWindow.Print("No serialized client info found.");
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Listing {list.Count} client info entries (cache populated):");
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Deconstruct(out var key, out var value);
			ulong clientId = key;
			SerializedClientInfo serializedClientInfo = value;
			string arg = "[Offline or Not Found]";
			if (hostClient != null && hostClient.ClientId == clientId)
			{
				arg = hostClient.name;
			}
			else
			{
				Client client = source.FirstOrDefault((Client c) => c.ClientId == clientId);
				if (client != null)
				{
					arg = client.name;
				}
			}
			stringBuilder.AppendLine($"  {i + 1}. ClientID: {clientId} (Name: {arg})");
			stringBuilder.AppendLine($"     StartHash: {serializedClientInfo.StartLocationHash}, SpawnRef: {serializedClientInfo.SpawnPointReference}");
			CachedClientIds.Add(clientId);
		}
		ConsoleWindow.Print(stringBuilder.ToString());
		return null;
	}
}
