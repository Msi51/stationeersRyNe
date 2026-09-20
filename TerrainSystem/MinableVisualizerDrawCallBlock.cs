using Assets.Scripts.Voxel;
using UnityEngine;

namespace TerrainSystem;

public class MinableVisualizerDrawCallBlock : DrawCallBlock
{
	private static readonly int Color1 = Shader.PropertyToID("_Color");

	private const int DRAW_CALL_CAPACITY = 512;

	public MinableVisualizerDrawCallBlock()
	{
		DrawCalls = new InstancedIndirectDrawCall[17];
		for (int i = 2; i < 17; i++)
		{
			if (VoxelTerrain.GetMineableInfo((MinableType)i, out var info) && MinableVisualiserData.MinableVisualizers.TryGetValue(info.MinableType, out var value))
			{
				Mesh baseMesh = null;
				Material material = null;
				if (value != null)
				{
					baseMesh = value.Mesh;
					material = Object.Instantiate(VoxelTerrain.Instance.oreVisualiserMaterial);
					material.SetColor(Color1, value.ColorReference);
				}
				DrawCalls[i] = new InstancedIndirectDrawCall(baseMesh, material, 0, 512);
			}
		}
	}

	public override void Render()
	{
		for (int i = 2; i < 17; i++)
		{
			DrawCalls[i]?.Draw(DrawCalls[i].Bounds);
		}
	}
}
