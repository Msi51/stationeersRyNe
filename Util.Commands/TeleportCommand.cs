using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using TerrainSystem;
using UnityEngine;

namespace Util.Commands;

internal class TeleportCommand : CommandBase
{
	public override string HelpText => "Teleports the local player to the given world (x, y). Y picks a safe ground point.";

	public override string[] Arguments => new string[1] { "<x> <y>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.CreativeOnly;

	public override string Execute(string[] args)
	{
		if (GameManager.IsBatchMode)
		{
			ConsoleWindow.PrintError("teleport is not available in batch mode", suppressStacktrace: true);
			return null;
		}
		if (!EnforceScope("teleport"))
		{
			return null;
		}
		if (args.Length < 2)
		{
			return "Invalid syntax";
		}
		if (!float.TryParse(args[0], out var result))
		{
			ConsoleWindow.PrintError("invalid x '" + args[0] + "'", suppressStacktrace: true);
			return null;
		}
		if (!float.TryParse(args[1], out var result2))
		{
			ConsoleWindow.PrintError("invalid y '" + args[1] + "'", suppressStacktrace: true);
			return null;
		}
		if (VoxelTerrain.Octree.OutsideBounds(VoxelTerrain.WorldToOctreeSpace(new Vector3Int((int)result, 1, (int)result2))))
		{
			ConsoleWindow.PrintError($"({result}, {result2}) is outside the world bounds", suppressStacktrace: true);
			return null;
		}
		if (InventoryManager.Parent == null)
		{
			ConsoleWindow.PrintError("no local player to teleport", suppressStacktrace: true);
			return null;
		}
		Vector3 safePoint = SpawnPoint.GetSafePoint(new Vector3(result, 1f, result2), Vector3.up);
		InventoryManager.Parent.Transform.position = safePoint;
		return $"Teleported {InventoryManager.Parent.DisplayName} to ({result}, {result2}).";
	}
}
