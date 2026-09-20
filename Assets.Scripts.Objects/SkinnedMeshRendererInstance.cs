using System;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class SkinnedMeshRendererInstance
{
	public Thing Parent;

	public SkinnedMeshRenderer Renderer;

	public int PaintableIndex = 1;

	private int _colorIndex = -1;

	public bool SetColorMaskOnMainMaterial { get; set; }

	public void SetColor(ColorSwatch colorSwatch, bool force = false)
	{
		if (!force && colorSwatch.Index == _colorIndex)
		{
			return;
		}
		_colorIndex = colorSwatch.Index;
		if (Renderer == null || Renderer.materials == null)
		{
			return;
		}
		Material[] materials = Renderer.materials;
		if (materials.ValidIndex(PaintableIndex))
		{
			if (SetColorMaskOnMainMaterial)
			{
				colorSwatch.ApplyToSuitMaterial(materials[PaintableIndex]);
			}
			else
			{
				materials[PaintableIndex] = colorSwatch.Normal;
			}
		}
		else
		{
			Debug.LogError($"IndexOutOfRange. Index: {PaintableIndex}, length: {materials.Length}", Renderer);
		}
		Renderer.materials = materials;
	}

	public override string ToString()
	{
		return $"Parent: {Parent}, Renderer: {Renderer}, PaintableIndex: {PaintableIndex}";
	}
}
