using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class RenderBatch
{
	public const int BATCH_SIZE = 1023;

	public string Name;

	public Mesh Mesh;

	public Material Material;

	public List<BatchInstance> Instances = new List<BatchInstance>(1023);

	public ShadowCastingMode ShadowMode = ShadowCastingMode.On;

	private List<Matrix4x4[]> _matricesArrays;

	private List<float[]> _diffuseIndicesArrays;

	private List<int> _batchCounts;

	private List<MaterialPropertyBlock> _propertyBlocks;

	public bool IsDirty = true;

	private static Dictionary<int, RenderBatch> _renderBatchLookup = new Dictionary<int, RenderBatch>(512);

	public void UpdatePropertyBlocks()
	{
		SetPropertyBlock();
	}

	public void Render(ShadowCastingMode shadowCastingMode)
	{
		if ((object)Mesh != null && (object)Material != null && _matricesArrays != null)
		{
			for (int i = 0; i < _matricesArrays.Count; i++)
			{
				Graphics.DrawMeshInstanced(Mesh, 0, Material, _matricesArrays[i], _batchCounts[i], _propertyBlocks[i], shadowCastingMode);
			}
		}
	}

	public void Populate()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			int num = 0;
			if (i >= 1023 + num * 1023)
			{
				num++;
			}
			if (i + num * 1023 >= Instances.Count)
			{
				FillArrayWithEmpty(num, i);
				break;
			}
			Populate(num, i, Instances[i]);
		}
	}

	private void FillArrayWithEmpty(int arrayIndex, int startingIndex)
	{
		for (int i = startingIndex; i <= 1023; i++)
		{
			_matricesArrays[arrayIndex][i] = Matrix4x4.identity;
			_diffuseIndicesArrays[arrayIndex][i] = 0f;
		}
	}

	public void Populate(int arrayIndex, int i, BatchInstance instance)
	{
		if (i >= 1023)
		{
			Debug.LogWarning("error " + Name + " batch size exceeded", instance.Transform);
			return;
		}
		if (_matricesArrays.Count <= arrayIndex)
		{
			_matricesArrays.Add(new Matrix4x4[1023]);
		}
		if (_diffuseIndicesArrays.Count <= arrayIndex)
		{
			_diffuseIndicesArrays.Add(new float[1023]);
		}
		if (_batchCounts.Count <= arrayIndex)
		{
			_batchCounts.Add(0);
		}
		_diffuseIndicesArrays[arrayIndex][i] = instance.ColorIndex;
		_matricesArrays[arrayIndex][i] = instance.GetTRS();
		IsDirty = true;
		_batchCounts[arrayIndex] = i + 1;
	}

	public RenderBatch(IBatchable batchable)
	{
		Mesh sharedMesh = batchable.SharedMesh;
		Name = sharedMesh.name;
		Mesh = sharedMesh;
		Material = batchable.GetMaterial();
		ShadowMode = batchable.ShadowCastingMode;
		_matricesArrays = new List<Matrix4x4[]> { new Matrix4x4[1023] };
		_diffuseIndicesArrays = new List<float[]> { new float[1023] };
		_batchCounts = new List<int> { 0 };
		_propertyBlocks = new List<MaterialPropertyBlock>
		{
			new MaterialPropertyBlock()
		};
		Singleton<StaticRendering>.Instance.Batches.Add(this);
		_renderBatchLookup.Add(sharedMesh.GetInstanceID(), this);
	}

	public static RenderBatch Get(int instanceId)
	{
		_renderBatchLookup.TryGetValue(instanceId, out var value);
		return value;
	}

	public static void Register(IBatchable batchable)
	{
		if ((object)batchable?.SharedMesh != null)
		{
			Mesh sharedMesh = batchable.SharedMesh;
			batchable.GetRendererTransform();
			RenderBatch renderBatch = Get(sharedMesh.GetInstanceID()) ?? new RenderBatch(batchable);
			int num = renderBatch.Instances.Count;
			BatchInstance instance = renderBatch.Add(batchable);
			int num2 = renderBatch.Instances.Count;
			int num3 = 0;
			while (num2 >= 1023)
			{
				num2 -= 1023;
				num3++;
			}
			if (num3 > 0)
			{
				num = num - num3 * 1023 + 1;
			}
			renderBatch.Populate(num3, num, instance);
		}
	}

	public static void Deregister(IBatchable batchable)
	{
		Get(batchable.SharedMesh.GetInstanceID())?.Remove(batchable.GetRendererTransform());
	}

	private BatchInstance Add(IBatchable batchable)
	{
		BatchInstance batchInstance = new BatchInstance(this, batchable);
		Instances.Add(batchInstance);
		return batchInstance;
	}

	private void Remove(Transform transform)
	{
		for (int num = Instances.Count - 1; num >= 0; num--)
		{
			if (!(Instances[num].Transform != transform))
			{
				Instances.RemoveAt(num);
			}
		}
	}

	public void SetPropertyBlock()
	{
		for (int i = 0; i < _diffuseIndicesArrays.Count; i++)
		{
			if (_propertyBlocks.Count <= i)
			{
				_propertyBlocks.Add(new MaterialPropertyBlock());
			}
			_propertyBlocks[i].SetFloatArray(Thing.DiffuseIndexPropertyID, _diffuseIndicesArrays[i]);
		}
		IsDirty = false;
	}
}
