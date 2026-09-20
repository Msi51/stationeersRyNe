using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class ColorSwatch
{
	[ReadOnly]
	public string Name;

	private int _index = -1;

	public int Bit;

	[ReadOnly]
	public int StringKey;

	[Tooltip("When true, this colour can ONLY be applied by spray painting - it is hidden from the logic colour dropdowns and rejected by IC10/logic Color writes. Used for cosmetic paint-only colours (e.g. the metallic spray cans). Default false so all existing colours stay logic-selectable (an absent value deserialises to false).")]
	public bool PaintOnly;

	public Material Normal;

	public Material Emissive;

	public Material Cutable;

	public Color Light = Color.white;

	[ReadOnly]
	public Color Color = Color.white;

	private static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");

	private static readonly int MaskMetallicId = Shader.PropertyToID("_MaskMetallic");

	private static readonly int MaskSmoothnessId = Shader.PropertyToID("_MaskSmoothness");

	public int Index
	{
		get
		{
			if (_index < 0)
			{
				_index = GameManager.GetColorIndex(this);
			}
			return _index;
		}
	}

	public bool IsSet => Index != -1;

	public string DisplayName => Localization.GetName(this);

	public string ToTooltip()
	{
		return "<color=yellow>" + DisplayName + "</color>";
	}

	public void ApplyToSuitMaterial(Material mat)
	{
		if (!(mat == null))
		{
			mat.SetColor(MaskColorId, Color);
			mat.SetFloat(MaskMetallicId, PaintOnly ? 0.85f : 0f);
			mat.SetFloat(MaskSmoothnessId, PaintOnly ? 0.85f : 0f);
		}
	}
}
