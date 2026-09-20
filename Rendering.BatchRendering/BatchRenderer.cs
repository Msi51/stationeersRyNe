using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering.BatchRendering;

public class BatchRenderer
{
	private static readonly int MaxBatchSize = 1023;

	private static MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

	private static Matrix4x4[] _matricesPart = new Matrix4x4[MaxBatchSize];

	public static void Render(Material material, Mesh mesh, Matrix4x4[] matrices, Vector4[] data, int dataMaterialProperty, Vector4[] data2, int data2MaterialProperty, int length)
	{
		_propertyBlock.Clear();
		_propertyBlock.SetVectorArray(dataMaterialProperty, data);
		_propertyBlock.SetVectorArray(data2MaterialProperty, data2);
		Graphics.DrawMeshInstanced(mesh, 0, material, matrices, length, _propertyBlock, ShadowCastingMode.Off, receiveShadows: false);
	}

	public static void Render(Material material, Mesh mesh, Matrix4x4[] matrices, Vector4[] colors, int colorMaterialProperty, int count, ShadowCastingMode shadowCastingMode = ShadowCastingMode.On, bool receiveShadows = true)
	{
		if (count > 0)
		{
			if (count > MaxBatchSize)
			{
				count = MaxBatchSize;
			}
			_propertyBlock.Clear();
			_propertyBlock.SetVectorArray(colorMaterialProperty, colors);
			Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count, _propertyBlock, shadowCastingMode, receiveShadows);
		}
	}

	public static void Render(Material material, Mesh mesh, Matrix4x4[] matrices, Vector4[] data, int dataMaterialProperty)
	{
		int num = matrices.Length;
		int sourceIndex = 0;
		int num2 = 0;
		while (num > 0)
		{
			if (num > MaxBatchSize)
			{
				num2 = MaxBatchSize;
				num -= MaxBatchSize;
			}
			else
			{
				num2 = num;
				num = 0;
			}
			_propertyBlock.Clear();
			Vector4[] array = new Vector4[num2];
			Array.Copy(data, sourceIndex, array, 0, num2);
			_propertyBlock.SetVectorArray(dataMaterialProperty, array);
			Matrix4x4[] array2 = new Matrix4x4[num2];
			Array.Copy(matrices, sourceIndex, array2, 0, num2);
			Graphics.DrawMeshInstanced(mesh, 0, material, array2, num2, _propertyBlock, ShadowCastingMode.Off, receiveShadows: false);
			sourceIndex = num2;
		}
	}

	public static void Render(Material material, Mesh mesh, List<Matrix4x4> matrices)
	{
		int num = matrices.Count;
		int num2 = 0;
		int num3 = 0;
		while (num > 0)
		{
			if (num > MaxBatchSize)
			{
				num3 = MaxBatchSize;
				num -= MaxBatchSize;
			}
			else
			{
				num3 = num;
				num = 0;
			}
			_propertyBlock.Clear();
			for (int i = 0; i < num3; i++)
			{
				_matricesPart[i] = matrices[i + num2];
			}
			Graphics.DrawMeshInstanced(mesh, 0, material, _matricesPart, num3, _propertyBlock, ShadowCastingMode.Off, receiveShadows: false);
			num2 = num3;
		}
	}
}
