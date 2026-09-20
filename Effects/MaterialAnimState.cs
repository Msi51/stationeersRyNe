using System;
using Assets.Scripts;
using UnityEngine;

namespace Effects;

[Serializable]
public class MaterialAnimState
{
	[SerializeField]
	private string name;

	[ReadOnly]
	public int Id;

	[SerializeField]
	private Material[] materials;

	public void ApplyTo(MeshRenderer renderer)
	{
		Material[] sharedMaterials = renderer.sharedMaterials;
		for (int i = 0; i < materials.Length; i++)
		{
			if ((bool)materials[i])
			{
				sharedMaterials[i] = materials[i];
			}
		}
		renderer.materials = sharedMaterials;
	}

	public void ApplyTo(Light l)
	{
		l.enabled = true;
		l.color = materials[0].color;
	}
}
