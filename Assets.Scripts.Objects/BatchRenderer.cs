using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects;

public static class BatchRenderer
{
	private static readonly List<IBatchRendered> _renderedObjects = new List<IBatchRendered>(65535);

	private static readonly List<DrawBatch> _batchedDrawCalls = new List<DrawBatch>(DrawBatch.BatchPool.Size);

	private static DrawBatch FindBatch(Mesh mesh, Material material)
	{
		foreach (DrawBatch batchedDrawCall in _batchedDrawCalls)
		{
			if (!batchedDrawCall.IsFull && !(batchedDrawCall.Mesh != mesh))
			{
				return batchedDrawCall;
			}
		}
		return null;
	}

	private static DrawBatch FindBatch(ThingRenderer rendered)
	{
		return FindBatch(rendered.SharedMesh, rendered.sharedMaterials[0]);
	}

	public static void RenderAll()
	{
		for (int num = _renderedObjects.Count - 1; num >= 0; num--)
		{
			IBatchRendered batchRendered = _renderedObjects[num];
			if (batchRendered == null)
			{
				_renderedObjects.RemoveAt(num);
			}
			else if (batchRendered is IThingBatched thingBatched)
			{
				foreach (ThingRenderer thingRenderer in thingBatched.GetThingRenderers())
				{
					if (thingRenderer.Enabled && (object)thingRenderer.SharedMesh != null)
					{
						DrawBatch drawBatch = FindBatch(thingRenderer);
						if (drawBatch == null)
						{
							drawBatch = DrawBatch.Make(thingRenderer);
							_batchedDrawCalls.Add(drawBatch);
						}
						drawBatch.Add(batchRendered.GetBatchMatrix());
					}
				}
			}
			else
			{
				DrawBatch drawBatch2 = FindBatch(batchRendered);
				if (drawBatch2 == null)
				{
					drawBatch2 = DrawBatch.Make(batchRendered);
					_batchedDrawCalls.Add(drawBatch2);
				}
				drawBatch2.Add(batchRendered.GetBatchMatrix());
			}
		}
		foreach (DrawBatch batchedDrawCall in _batchedDrawCalls)
		{
			batchedDrawCall.Render();
			batchedDrawCall.ReturnToPool();
		}
	}

	private static DrawBatch FindBatch(IBatchRendered rendered)
	{
		return FindBatch(rendered.GetMesh(), rendered.GetMaterial());
	}

	public static void ClearBatches()
	{
		_batchedDrawCalls.Clear();
	}

	public static void ClearAll()
	{
		_renderedObjects.Clear();
	}

	public static void Add(IBatchRendered batchRendered)
	{
		if (batchRendered != null)
		{
			_renderedObjects.Add(batchRendered);
		}
	}

	public static void Add(IThingBatched batchRendered)
	{
		if (batchRendered == null)
		{
			return;
		}
		for (int num = _renderedObjects.Count - 1; num >= 0; num--)
		{
			IBatchRendered batchRendered2 = _renderedObjects[num];
			if (batchRendered2 == null)
			{
				_renderedObjects.RemoveAt(num);
			}
			else if (batchRendered2 is IThingBatched thingBatched && thingBatched.ReferenceId == batchRendered.ReferenceId)
			{
				return;
			}
		}
		_renderedObjects.Add(batchRendered);
	}

	public static void Remove(IBatchRendered batchRendered)
	{
		_renderedObjects.Remove(batchRendered);
	}
}
