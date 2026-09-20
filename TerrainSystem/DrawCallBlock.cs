using UnityEngine;

namespace TerrainSystem;

public abstract class DrawCallBlock
{
	public InstancedIndirectDrawCall[] DrawCalls;

	public Bounds RenderBounds;

	public BoundsInt BlockBounds;

	public virtual bool RandomRotation => false;

	public virtual bool HideInTerrain => false;

	public abstract void Render();

	public void Clear()
	{
		if (DrawCalls != null)
		{
			for (int i = 0; i < DrawCalls.Length; i++)
			{
				DrawCalls[i]?.Clear();
			}
		}
	}

	public void CacheRenderBounds()
	{
		RenderBounds = default(Bounds);
		if (DrawCalls == null)
		{
			return;
		}
		for (int i = 0; i < DrawCalls.Length; i++)
		{
			if (DrawCalls[i] != null && DrawCalls[i].InstanceCount > 0)
			{
				RenderBounds.Encapsulate(DrawCalls[i].Bounds);
			}
		}
	}
}
