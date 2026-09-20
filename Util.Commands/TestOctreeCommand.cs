using System;
using System.Diagnostics;
using Assets.Scripts;
using TerrainSystem;
using UnityEngine;

namespace Util.Commands;

public class TestOctreeCommand : CommandBase
{
	public override string HelpText => "Benchmarks voxel terrain density reads at random world locations. Runs the requested number of iterations and prints the elapsed time.";

	public override string[] Arguments => new string[1] { "<iterations>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("testoctree"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "iterations", out int result))
		{
			return null;
		}
		ConsoleWindow.Print($"Building test environment for {result} iterations.");
		System.Random random = new System.Random(30123);
		Vector3Int[] array = new Vector3Int[result];
		for (int i = 0; i < result; i++)
		{
			array[i] = new Vector3Int(random.Next(VoxelConstants.Size), random.Next(1023), random.Next(VoxelConstants.Size));
		}
		ConsoleWindow.Print("Beginning test.");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		for (int j = 0; j < result; j++)
		{
			VoxelTerrain.GetDensityWorldSpace(array[j]);
		}
		stopwatch.Stop();
		return $"Queried {result} random densities in {(float)stopwatch.ElapsedMilliseconds / 1000f} s.";
	}
}
