using System;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

namespace Rendering;

[Serializable]
public class DrawCall
{
	[ReadOnly]
	public readonly DrawData drawData;

	private List<Matrix4x4> _matrices = new List<Matrix4x4>(BatchedRenderer.DrawCallMaxInstances);

	private Dictionary<int, float[]> _floatProperties = new Dictionary<int, float[]>();

	private Dictionary<int, Vector4[]> _vectorProperties = new Dictionary<int, Vector4[]>();

	[ReadOnly]
	public int instanceCount;

	private static MaterialPropertyBlock _materialPropertyBlock = new MaterialPropertyBlock();

	public DrawCall(DrawData drawData)
	{
		this.drawData = drawData;
	}

	public void Register(Matrix4x4 localToWorldTransform)
	{
		_matrices.Add(localToWorldTransform);
		instanceCount = _matrices.Count;
		int num = instanceCount - 1;
		foreach (KeyValuePair<int, float[]> floatProperty in _floatProperties)
		{
			floatProperty.Value[num] = 0f;
		}
		foreach (KeyValuePair<int, Vector4[]> vectorProperty in _vectorProperties)
		{
			vectorProperty.Value[num] = default(Vector4);
		}
	}

	public void Deregister(Matrix4x4 localToWorldTransform)
	{
		int num = IndexOf(localToWorldTransform);
		if (num < 0)
		{
			return;
		}
		_matrices.RemoveAt(num);
		foreach (KeyValuePair<int, float[]> floatProperty in _floatProperties)
		{
			float[] value = floatProperty.Value;
			for (int i = num; i < instanceCount - 1; i++)
			{
				value[i] = value[i + 1];
			}
		}
		foreach (KeyValuePair<int, Vector4[]> vectorProperty in _vectorProperties)
		{
			Vector4[] value2 = vectorProperty.Value;
			for (int j = num; j < instanceCount - 1; j++)
			{
				value2[j] = value2[j + 1];
			}
		}
		instanceCount = _matrices.Count;
	}

	public void SetFloat(Matrix4x4 localToWorldTransform, int propertyID, float value)
	{
		int num = IndexOf(localToWorldTransform);
		if (num >= 0)
		{
			GetFloatPropertyList(propertyID)[num] = value;
		}
	}

	private float[] GetFloatPropertyList(int propertyID)
	{
		if (!_floatProperties.TryGetValue(propertyID, out var value))
		{
			value = new float[BatchedRenderer.DrawCallMaxInstances];
			_materialPropertyBlock.SetFloatArray(propertyID, value);
			_floatProperties[propertyID] = value;
		}
		return value;
	}

	public void SetVector(Matrix4x4 localToWorldTransform, int propertyID, Vector4 value)
	{
		int num = IndexOf(localToWorldTransform);
		if (num >= 0)
		{
			GetVectorPropertyList(propertyID)[num] = value;
		}
	}

	private Vector4[] GetVectorPropertyList(int propertyID)
	{
		if (!_vectorProperties.TryGetValue(propertyID, out var value))
		{
			value = new Vector4[BatchedRenderer.DrawCallMaxInstances];
			_materialPropertyBlock.SetVectorArray(propertyID, value);
			_vectorProperties[propertyID] = value;
		}
		return value;
	}

	private int IndexOf(Matrix4x4 transformMatrix)
	{
		for (int i = 0; i < _matrices.Count; i++)
		{
			if (transformMatrix.Equals(_matrices[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public void Draw()
	{
		foreach (KeyValuePair<int, float[]> floatProperty in _floatProperties)
		{
			_materialPropertyBlock.SetFloatArray(floatProperty.Key, floatProperty.Value);
		}
		foreach (KeyValuePair<int, Vector4[]> vectorProperty in _vectorProperties)
		{
			_materialPropertyBlock.SetVectorArray(vectorProperty.Key, vectorProperty.Value);
		}
		for (int i = 0; i < drawData.materials.Length; i++)
		{
			Graphics.DrawMeshInstanced(drawData.mesh, i, drawData.materials[i], _matrices, _materialPropertyBlock, drawData.shadowMode);
		}
	}

	public bool IsSameDrawData(DrawData otherDrawData)
	{
		if (drawData.mesh != otherDrawData.mesh)
		{
			return false;
		}
		if (drawData.materials.Length != otherDrawData.materials.Length)
		{
			return false;
		}
		for (int i = 0; i < drawData.materials.Length; i++)
		{
			if (drawData.materials[i] != otherDrawData.materials[i])
			{
				return false;
			}
		}
		return true;
	}
}
