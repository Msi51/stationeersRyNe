using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace Util.Commands;

internal class VoxelFillNearCommand : CommandBase
{
	private const int MaxRadius = 100;

	public override string HelpText => "Reverts terrain voxels within <radius> metres of a player back to their originally generated state. Omit the name to target yourself (not available on a dedicated server). Creative or dedicated server only, and only while no clients are connected.";

	public override string[] Arguments => new string[2] { "<radius>", "<playerName> <radius>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer | CommandScope.CreativeOnly;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("voxelfillnear"))
		{
			return null;
		}
		if (!GameManager.RunSimulation)
		{
			return "Can only be run on the server";
		}
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			ConsoleWindow.PrintError("voxelfillnear cannot be used while clients are connected.", suppressStacktrace: true);
			return null;
		}
		if (VoxelTerrain.Octree == null || VoxelTerrain.ReadOnlyOctree == null)
		{
			ConsoleWindow.PrintError("Terrain is not loaded.", suppressStacktrace: true);
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		int valueArgIndex;
		Human human = CommandBase.ResolveTargetPlayer(args, out valueArgIndex);
		if (human == null)
		{
			return null;
		}
		if (!CommandBase.Get(args, valueArgIndex, "radius", out int result))
		{
			return null;
		}
		if (result <= 0)
		{
			ConsoleWindow.PrintError("radius must be greater than zero", suppressStacktrace: true);
			return null;
		}
		if (result > 100)
		{
			ConsoleWindow.PrintError($"radius capped at {100}", suppressStacktrace: true);
			result = 100;
		}
		Vector3Int vector3Int = human.ThingTransform.position.FloorToInt();
		int num = result * result;
		sbyte desiredDepth = (sbyte)VoxelTerrain.MaxDepth;
		int num2 = 0;
		for (int i = -result; i <= result; i++)
		{
			for (int j = -result; j <= result; j++)
			{
				for (int k = -result; k <= result; k++)
				{
					if (i * i + j * j + k * k > num)
					{
						continue;
					}
					Vector3Int vector3Int2 = new Vector3Int(vector3Int.x + i, vector3Int.y + j, vector3Int.z + k);
					if (!LodManager.IsAtOrUnderBedrock(vector3Int2))
					{
						byte readonlyDensityWorldSpace = VoxelTerrain.GetReadonlyDensityWorldSpace(vector3Int2);
						if (readonlyDensityWorldSpace != VoxelTerrain.GetNodeInfoWorldSpace(vector3Int2).Density)
						{
							VoxelNodeType readonlyNodeTypeWorldSpace = VoxelTerrain.GetReadonlyNodeTypeWorldSpace(vector3Int2, desiredDepth);
							VoxelTerrain.SetDensityWorldSpace(vector3Int2, VoxelTerrain.DensityToFloat(readonlyDensityWorldSpace), RoomChangeSource.VoxelAdd, dirtyLods: false, setNodeType: true, readonlyNodeTypeWorldSpace);
							num2++;
						}
					}
				}
			}
		}
		if (num2 > 0)
		{
			LodManager.Instance.DirtyLodsBounds(new Vector3(vector3Int.x - result, vector3Int.y - result, vector3Int.z - result), new Vector3(vector3Int.x + result, vector3Int.y + result, vector3Int.z + result));
		}
		return $"Reverted {num2} voxels within {result}m of {human.DisplayName} to generated state.";
	}
}
