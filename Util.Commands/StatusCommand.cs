using System;
using System.Text;
using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

internal class StatusCommand : CommandBase
{
	public override string HelpText => "Prints world name, game state, uptime, and pause status.";

	public override string[] Arguments => null;

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("World:    " + WorldManager.CurrentWorldName);
		stringBuilder.AppendLine($"State:    {GameManager.GameState}");
		stringBuilder.AppendLine($"Uptime:   {TimeSpan.FromSeconds(Time.realtimeSinceStartup):g}");
		stringBuilder.Append("Paused:   " + (WorldManager.IsGamePaused ? "yes" : "no"));
		return stringBuilder.ToString();
	}
}
