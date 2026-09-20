using System;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using UnityEngine;

namespace Util.Commands;

public class UnstuckCommand : CommandBase
{
	public override string HelpText => "Bumps the local player upward by a small amount to free them from terrain or geometry.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("unstuck"))
		{
			return null;
		}
		if ((object)InventoryManager.Parent == null)
		{
			ConsoleWindow.PrintError("Cannot unstick: no character is assigned to the local player.", suppressStacktrace: true);
			return null;
		}
		InventoryManager.Parent.Transform.position += Vector3.up * 0.25f;
		return "Player bumped upward by 0.25 units.";
	}
}
