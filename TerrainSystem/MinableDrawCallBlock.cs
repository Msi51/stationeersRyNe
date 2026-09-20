using Assets.Scripts.Voxel;
using UnityEngine.Rendering;

namespace TerrainSystem;

public class MinableDrawCallBlock : DrawCallBlock
{
	private const int DRAW_CALL_CAPACITY = 512;

	public override bool RandomRotation => true;

	public override bool HideInTerrain => true;

	public MinableDrawCallBlock()
	{
		DrawCalls = new InstancedIndirectDrawCall[17];
		for (int i = 2; i < 17; i++)
		{
			if (VoxelTerrain.GetMineableInfo((MinableType)i, out var info))
			{
				DrawCalls[i] = new InstancedIndirectDrawCall(info.mesh, info.material, 0, 512);
			}
		}
	}

	public override void Render()
	{
		for (int i = 2; i < 17; i++)
		{
			DrawCalls[i]?.Draw(DrawCalls[i].Bounds, ShadowCastingMode.On);
		}
	}
}
