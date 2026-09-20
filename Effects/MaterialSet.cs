using System;
using UnityEngine;

namespace Effects;

[Serializable]
public struct MaterialSet
{
	public Material[] Materials;

	public void ApplyTo(MeshRenderer renderer)
	{
		Material[] sharedMaterials = renderer.sharedMaterials;
		for (int i = 0; i < Materials.Length; i++)
		{
			if ((bool)Materials[i])
			{
				sharedMaterials[i] = Materials[i];
			}
		}
		renderer.materials = sharedMaterials;
	}
}
