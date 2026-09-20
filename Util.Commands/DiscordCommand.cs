using System;
using Assets.Scripts.Util;
using Networking;

namespace Util.Commands;

internal class DiscordCommand : CommandBase
{
	public override string HelpText => "Prints the current Discord SDK rich-presence activity, or notes that Discord is not enabled in this build.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (!Singleton<DiscordClient>.Instance.IsInitialised)
		{
			return "Discord is not initialised.";
		}
		return "Current activity: " + Singleton<DiscordClient>.Instance.CurrentActivityJson;
	}
}
