using System;
using Assets.Scripts.Objects;
using UnityEngine;

[Serializable]
public struct BatchInstance
{
	public string Name;

	public Transform Transform;

	private RenderBatch _parent;

	public float ColorIndex;

	public IBatchable Batchable;

	public BatchInstance(RenderBatch batch, IBatchable batchable)
	{
		Batchable = batchable;
		Transform rendererTransform = batchable.GetRendererTransform();
		Name = "~transform_" + rendererTransform.GetInstanceID();
		_parent = batch;
		Transform = rendererTransform;
		if (batchable.FloatProperties.TryGetValue(Thing.DiffuseIndexPropertyID, out var value))
		{
			ColorIndex = value;
		}
		else
		{
			ColorIndex = 0f;
		}
	}

	public Matrix4x4 GetTRS()
	{
		return Matrix4x4.TRS(Transform.position, Transform.rotation, Transform.lossyScale);
	}
}
