using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace Util.Commands;

internal class DeleteNearCommand : CommandBase
{
	public override string HelpText => "Deletes every thing within <distance> metres of a player, sparing the player and anything they are carrying. Omit the name to target yourself (not available on a dedicated server). Creative or dedicated server only.";

	public override string[] Arguments => new string[2] { "<distance>", "<playerName> <distance>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer | CommandScope.CreativeOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("deletenear"))
		{
			return null;
		}
		if (!GameManager.RunSimulation)
		{
			return "Can only be run on the server";
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		int valueArgIndex;
		Human player = CommandBase.ResolveTargetPlayer(args, out valueArgIndex);
		if (player == null)
		{
			return null;
		}
		if (!CommandBase.Get(args, valueArgIndex, "distance", out float result))
		{
			return null;
		}
		if (result <= 0f)
		{
			ConsoleWindow.PrintError("distance must be greater than zero", suppressStacktrace: true);
			return null;
		}
		Vector3 center = player.ThingTransform.position;
		float sqrRadius = result * result;
		List<Thing> toDelete = new List<Thing>();
		OcclusionManager.AllThings.ForEach(delegate(Thing thing)
		{
			if (!(thing == null) && !(thing.ThingTransform == null) && !(thing.RootParent == player) && !((thing.ThingTransform.position - center).sqrMagnitude > sqrRadius))
			{
				toDelete.Add(thing);
			}
		});
		foreach (Thing item in toDelete)
		{
			OnServer.Destroy(item);
		}
		return $"Deleted {toDelete.Count} things within {result}m of {player.DisplayName}.";
	}

	public override IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		if (argIndex != 0)
		{
			return null;
		}
		return Human.AllHumans.Select((Human h) => h.DisplayName);
	}
}
