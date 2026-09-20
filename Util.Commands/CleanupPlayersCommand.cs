using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Util.Commands;

public class CleanupPlayersCommand : CommandBase
{
	public override string HelpText => "Cleans up player bodies (dead, disconnected, or all) and optionally body bags. Host or singleplayer only.";

	public override string[] Arguments => new string[1] { "<dead | disconnected | all | bodybags>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("cleanupplayers"))
		{
			return null;
		}
		if (args.Length != 1)
		{
			return "Invalid syntax";
		}
		if (args[0] == "bodybags")
		{
			int count = DynamicBodyBag.AllBodyBags.Count;
			foreach (DynamicBodyBag allBodyBag in DynamicBodyBag.AllBodyBags)
			{
				Object.Destroy(allBodyBag.gameObject);
			}
			ConsoleWindow.PrintAction($"Deleted {count} body bags.");
		}
		if (args[0] == "emptybodybags")
		{
			int count2 = DynamicBodyBag.AllBodyBags.Count;
			foreach (DynamicBodyBag allBodyBag2 in DynamicBodyBag.AllBodyBags)
			{
				if (allBodyBag2.PlayerHasRespawned || allBodyBag2.BrainSlot.Contains<Brain>())
				{
					Object.Destroy(allBodyBag2.gameObject);
				}
			}
			ConsoleWindow.PrintAction($"Deleted {count2} body bags.");
		}
		List<Human> list = new List<Human>();
		foreach (Human allHuman in Human.AllHumans)
		{
			if (!(allHuman.ParentSlot.Parent is CryoTube))
			{
				switch (args[0])
				{
				case "dead":
					CleanupDead(allHuman, list);
					break;
				case "disconnected":
					CleanupDisconnected(allHuman, list);
					break;
				case "all":
					CleanupDead(allHuman, list);
					CleanupDisconnected(allHuman, list);
					break;
				}
			}
		}
		foreach (Human item in list)
		{
			item.OnEntityDecay();
		}
		return null;
	}

	private void CleanupDead(Human human, List<Human> toDestroy)
	{
		if (human.State == EntityState.Dead && !toDestroy.Contains(human))
		{
			toDestroy.Add(human);
		}
	}

	private void CleanupDisconnected(Human human, List<Human> toDestroy)
	{
		Brain brain = human?.BrainSlot?.Occupant as Brain;
		if ((bool)brain && Client.Find(brain.ClientId) == null && !toDestroy.Contains(human))
		{
			toDestroy.Add(human);
		}
	}
}
