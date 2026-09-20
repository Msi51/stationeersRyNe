using UnityEngine;

namespace Objects.Electrical;

public static class GLLines
{
	private static Material _material;

	public static void SetPass()
	{
		if (!_material)
		{
			_material = new Material(Shader.Find("Hidden/Internal-Colored"))
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			_material.SetInt("_SrcBlend", 5);
			_material.SetInt("_DstBlend", 10);
			_material.SetInt("_Cull", 2);
			_material.SetInt("_ZWrite", 1);
		}
		_material.SetPass(0);
	}
}
