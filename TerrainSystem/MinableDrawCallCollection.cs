using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public class MinableDrawCallCollection : IThreadable
{
	private const int PARTITION_LENGTH = 3;

	public const int WORK_PARTITIONS = 9;

	public const int CENTER_INDEX = 4;

	public readonly MinableDrawCallBlock[] DrawCallBlocks;

	public MinableVisualizerDrawCallBlock VisualizerDrawCallBlock;

	private static int _renderBlockSize = 72;

	private const int RENDER_BLOCK_SIZE_LOW = 48;

	private const int RENDER_BLOCK_SIZE_MED = 72;

	private const int RENDER_BLOCK_SIZE_HIGH = 88;

	public bool IsWriting;

	private Plane[] _frustumPlanes = new Plane[6];

	public int ThreadCost => 1;

	public MinableDrawCallCollection()
	{
		DrawCallBlocks = new MinableDrawCallBlock[9];
		for (int i = 0; i < DrawCallBlocks.Length; i++)
		{
			DrawCallBlocks[i] = new MinableDrawCallBlock();
		}
		VisualizerDrawCallBlock = new MinableVisualizerDrawCallBlock();
	}

	public void RefreshAll()
	{
		for (int i = 0; i < 9; i++)
		{
			BoundsInt blockBounds = GetBlockBounds(i);
			VoxelTerrain.QueueMinableRenderRefresh(new MinableRenderJob(DrawCallBlocks[i], blockBounds, MinableRenderJob.BoundsRule.RootInBounds));
		}
		RefreshVisualisers();
	}

	public void RefreshVisualisers()
	{
		BoundsInt blockBounds = GetBlockBounds(4);
		if (CameraController.Instance.IsSensorLensesFxActive)
		{
			VoxelTerrain.QueueMinableRenderRefresh(new MinableRenderJob(VisualizerDrawCallBlock, blockBounds, MinableRenderJob.BoundsRule.AnyInBounds));
		}
	}

	public void RenderMinables()
	{
		GeometryUtility.CalculateFrustumPlanes(CameraController.CurrentCamera, _frustumPlanes);
		MinableDrawCallBlock[] drawCallBlocks = DrawCallBlocks;
		foreach (MinableDrawCallBlock minableDrawCallBlock in drawCallBlocks)
		{
			if (GeometryUtility.TestPlanesAABB(_frustumPlanes, minableDrawCallBlock.RenderBounds))
			{
				minableDrawCallBlock.Render();
			}
		}
	}

	public void RenderVisualisers()
	{
		VisualizerDrawCallBlock.Render();
	}

	public static void RefreshMinableRenderDistance()
	{
		_renderBlockSize = Settings.CurrentData.MinableDistance switch
		{
			"Low" => 48, 
			"Medium" => 72, 
			"High" => 88, 
			_ => 88, 
		};
		TerrainShaderScript.SetMinableRenderDistance((float)_renderBlockSize * 1.5f);
	}

	private BoundsInt GetBlockBounds(int index)
	{
		int num = index % 3 * _renderBlockSize;
		int num2 = index / 3 % 3 * _renderBlockSize;
		int num3 = 0;
		num -= 3 * _renderBlockSize / 2;
		num2 -= 3 * _renderBlockSize / 2;
		num3 -= _renderBlockSize / 2;
		return new BoundsInt(InventoryManager.WorldPosition.FloorToInt() + new Vector3Int(num, num3, num2), new Vector3Int(_renderBlockSize, _renderBlockSize, _renderBlockSize));
	}

	public void ClearAll()
	{
		if (DrawCallBlocks != null)
		{
			MinableDrawCallBlock[] drawCallBlocks = DrawCallBlocks;
			for (int i = 0; i < drawCallBlocks.Length; i++)
			{
				drawCallBlocks[i]?.Clear();
			}
		}
		VisualizerDrawCallBlock?.Clear();
	}

	public bool CanThread()
	{
		return IsWriting;
	}

	public string DebugName()
	{
		return "MinableRenderCollection";
	}
}
