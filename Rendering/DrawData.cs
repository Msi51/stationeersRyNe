using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Rendering;

[Serializable]
public struct DrawData
{
	public Mesh mesh;

	[FormerlySerializedAs("_materials")]
	public Material[] materials;

	public ShadowCastingMode shadowMode;

	public bool IsValid()
	{
		if (mesh != null)
		{
			return materials.Length != 0;
		}
		return false;
	}
}
