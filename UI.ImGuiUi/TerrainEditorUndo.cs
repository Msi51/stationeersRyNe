using System;
using Assets.Scripts;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace UI.ImGuiUi;

public class TerrainEditorUndo
{
	private readonly byte[][][] _undoData;

	private readonly byte[][][] _undoDataVoxelTypes;

	public Vector3Int Size;

	public Vector3 LocalPosition { get; private set; }

	public TerrainEditorUndo(Bounds bounds)
	{
		LocalPosition = bounds.min;
		Vector3Int vector3Int = (Size = new Vector3Int(Mathf.CeilToInt(bounds.size.x), Mathf.CeilToInt(bounds.size.y), Mathf.CeilToInt(bounds.size.z)));
		int x = vector3Int.x;
		int y = vector3Int.y;
		int z = vector3Int.z;
		_undoData = new byte[x][][];
		_undoDataVoxelTypes = new byte[x][][];
		for (int i = 0; i < x; i++)
		{
			_undoData[i] = new byte[y][];
			_undoDataVoxelTypes[i] = new byte[y][];
			for (int j = 0; j < y; j++)
			{
				_undoData[i][j] = new byte[z];
				_undoDataVoxelTypes[i][j] = new byte[z];
			}
		}
	}

	public void SetVoxelValue(int x, int y, int z, byte density, byte nodeType)
	{
		_undoData[x][y][z] = density;
		_undoDataVoxelTypes[x][y][z] = nodeType;
	}

	public byte GetDensityValue(int x, int y, int z)
	{
		return _undoData[x][y][z];
	}

	public byte GetVoxelType(int x, int y, int z)
	{
		return _undoDataVoxelTypes[x][y][z];
	}

	public void Clear()
	{
		Array.Clear(_undoData, 0, _undoData.Length);
		Array.Clear(_undoDataVoxelTypes, 0, _undoDataVoxelTypes.Length);
	}

	public void Apply()
	{
		for (int i = 0; i < Size.x; i++)
		{
			for (int j = 0; j < Size.y; j++)
			{
				for (int k = 0; k < Size.z; k++)
				{
					VoxelTerrain.SetDensityWorldSpace(LocalPosition + new Vector3(i, j, k), VoxelTerrain.DensityToFloat(GetDensityValue(i, j, k)), RoomChangeSource.VoxelAdd, dirtyLods: false, setNodeType: true, (VoxelNodeType)GetVoxelType(i, j, k));
				}
			}
		}
		LodManager.Instance.DirtyLodsBounds(LocalPosition, LocalPosition + Size);
		Clear();
	}
}
