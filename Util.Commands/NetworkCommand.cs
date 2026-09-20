using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Util.Commands;

public class NetworkCommand : CommandBase
{
	public enum NetworkView
	{
		Status,
		Clients,
		Stats,
		Adapters,
		Debug,
		Sections,
		Awake
	}

	public override string HelpText => "Prints network information: 'status' (role/host/tick), 'clients' (connected players), 'stats' (per-client RocketNet link health), 'adapters' (local interfaces), 'debug' (toggles the on-screen graph window), 'sections' (state-tick payload bytes by section, server only), 'awake' (physics batch set by prefab, server only).";

	public override string[] Arguments => new string[1] { "<status | clients | stats | adapters | debug | sections | awake>" };

	public override bool IsLaunchCmd => false;

	public override IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		if (argIndex != 0)
		{
			return null;
		}
		return from n in Enum.GetNames(typeof(NetworkView))
			select n.ToLowerInvariant();
	}

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.GetEnum<NetworkView>(args, 0, "view", out var result))
		{
			return null;
		}
		switch (result)
		{
		case NetworkView.Status:
			NetworkManager.LogStatusToConsole();
			break;
		case NetworkView.Clients:
			if (CommandBase.CannotInSinglePlayer("network clients"))
			{
				return null;
			}
			NetworkManager.LogClientRosterToConsole();
			break;
		case NetworkView.Stats:
			if (CommandBase.CannotInSinglePlayer("network stats"))
			{
				return null;
			}
			NetworkManager.LogConnectionStatsToConsole();
			break;
		case NetworkView.Adapters:
			NetworkManager.LogAdaptersToConsole();
			break;
		case NetworkView.Debug:
			if (!CommandBase.IsInGame("network debug"))
			{
				return null;
			}
			NetworkDebugWindow.Toggle();
			return $"Network debug window: {NetworkDebugWindow.Show}";
		case NetworkView.Sections:
			if (CommandBase.CannotInSinglePlayer("network sections"))
			{
				return null;
			}
			NetworkManager.LogSectionsToConsole();
			break;
		case NetworkView.Awake:
			if (CommandBase.CannotInSinglePlayer("network awake"))
			{
				return null;
			}
			NetworkManager.LogAwakeBodiesToConsole();
			break;
		}
		return null;
	}
}
