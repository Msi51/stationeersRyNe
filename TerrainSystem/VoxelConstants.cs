using UnityEngine;

namespace TerrainSystem;

public static class VoxelConstants
{
	public const float HALF_VOXEL_SIZE = 0.5f;

	public const int VOXEL_SIZE = 1;

	public const float TERRAIN_MESH_OFFSET = 0.499f;

	public const int DEFAULT_SIZE = 1024;

	public static int Size = 1024;

	public static int Offset = 512;

	public const int CHILD_COUNT = 8;

	public static Vector3Int OriginOffsetInt = new Vector3Int(Offset, 0, Offset);

	public static Vector3 TerrainMeshOffset = new Vector3(0.499f, 0.499f, 0.499f);

	private static Vector3 TerrainLodOffset = new Vector3(0f, 0.5f, 0f);

	public static float WorldCurvature = 0f;

	public const float CURVATURE_START_DISTANCE = 144f;

	public const float VOXEL_TYPE_SAMPLE_OFFSET = 1.1f;

	public static Vector3 TerrainLodLevelOffset(int level)
	{
		return TerrainLodOffset * level switch
		{
			0 => 0f, 
			1 => 0.5f, 
			2 => 1f, 
			3 => 2f, 
			4 => 4f, 
			5 => 8f, 
			6 => 16f, 
			_ => 0f, 
		};
	}

	public static void SetWorldSizeOffset(int size)
	{
		Size = size;
		Offset = size / 2;
		OriginOffsetInt = new Vector3Int(Offset, 0, Offset);
	}

	public static float GetCurvatureOffset(Vector3 origin, Vector3 worldPosition)
	{
		Vector2 vector = new Vector2(origin.x, origin.z);
		float magnitude = (new Vector2(worldPosition.x, worldPosition.z) - vector).magnitude;
		float num = Mathf.Max(0f, magnitude - 144f) * WorldCurvature;
		return 0f - num * num * 0.001f;
	}
}
