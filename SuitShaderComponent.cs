using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

public class SuitShaderComponent : MonoBehaviour
{
	[SerializeField]
	private Material _material;

	private static readonly int MaskColor = Shader.PropertyToID("_MaskColor");

	private static readonly int MaskMetallic = Shader.PropertyToID("_MaskMetallic");

	private static readonly int MaskSmoothness = Shader.PropertyToID("_MaskSmoothness");

	public void SetColor(int index)
	{
		SetColor(Singleton<GameManager>.Instance.CustomColors[index]);
	}

	public void SetColor(ColorSwatch colorSwatch)
	{
		_material.SetColor(MaskColor, colorSwatch.Color);
		_material.SetFloat(MaskMetallic, colorSwatch.PaintOnly ? 0.85f : 0f);
		_material.SetFloat(MaskSmoothness, colorSwatch.PaintOnly ? 0.85f : 0f);
	}
}
