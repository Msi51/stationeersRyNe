using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Util.Commands;

public class ValidateSourcePrefabsCommands : CommandBase
{
	public override string HelpText => "Runs validation passes over all source prefabs. 'Thumbnails' verifies that every source prefab and structure build state has a non-null thumbnail.";

	public override string[] Arguments => new string[1] { "<Thumbnails>" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "type", out string result))
		{
			return null;
		}
		result = result.ToLower();
		if (result == "thumbnails")
		{
			ValidateThumbnails();
			return null;
		}
		ConsoleWindow.PrintError("Unknown validation type '" + result + "'.", suppressStacktrace: true);
		return null;
	}

	private static void ValidateThumbnails()
	{
		int num = 0;
		foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
		{
			if (Thing.ThingHasThumbnailVariations(sourcePrefab) && !(sourcePrefab is Structure))
			{
				Sprite[] thumbnails = sourcePrefab.Thumbnails;
				for (int i = 0; i < thumbnails.Length; i++)
				{
					if (thumbnails[i] == null)
					{
						ConsoleWindow.PrintAction(sourcePrefab.DisplayName + ": Is Missing color thumbnails.");
						num++;
						break;
					}
				}
			}
			else if (sourcePrefab.Thumbnail == null)
			{
				ConsoleWindow.PrintAction(sourcePrefab.DisplayName + ": Thumbnail field is null.");
				num++;
			}
			if (!(sourcePrefab is Structure structure) || structure.BuildStates.Count <= 1)
			{
				continue;
			}
			for (int j = 0; j < structure.BuildStates.Count; j++)
			{
				if (structure.BuildStates[j].Thumbnail == null)
				{
					ConsoleWindow.PrintAction($"{structure.DisplayName}: BuildState {j} Thumbnail field is null.");
					num++;
				}
			}
		}
		if (num > 0)
		{
			ConsoleWindow.PrintError($"{num} thumbnail errors detected.", suppressStacktrace: true);
		}
		else
		{
			ConsoleWindow.PrintAction("No thumbnail errors detected.");
		}
	}
}
