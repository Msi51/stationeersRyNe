using UnityEngine;

namespace ColorBlindUtility.UGUI;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Color-Blind Utility/Color-Blind Filter")]
public class ColorBlindFilter : MonoBehaviour
{
	public Shader filterShader;

	protected Material filterMaterial;

	private Matrix4x4 filterMatrix = Matrix4x4.identity;

	public ColorBlindMode colorBlindMode;

	protected Material FilterMaterial
	{
		get
		{
			if (!filterMaterial)
			{
				filterMaterial = new Material(filterShader);
				filterMaterial.hideFlags = HideFlags.DontSave;
			}
			return filterMaterial;
		}
	}

	private void OnEnable()
	{
		if (!SystemInfo.supportsImageEffects || ((bool)filterShader && !filterShader.isSupported))
		{
			base.enabled = false;
		}
	}

	private void OnDisable()
	{
		if ((bool)filterMaterial)
		{
			Object.DestroyImmediate(filterMaterial);
		}
	}

	public void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		switch (colorBlindMode)
		{
		case ColorBlindMode.None:
			filterMatrix = Matrix4x4.identity;
			break;
		case ColorBlindMode.Protanopia:
			filterMatrix.SetColumn(0, new Vector4(0.567f, 0.433f, 0f, 0f));
			filterMatrix.SetColumn(1, new Vector4(0.558f, 0.442f, 0f, 0f));
			filterMatrix.SetColumn(2, new Vector4(0f, 0.242f, 0.758f, 0f));
			filterMatrix.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
			break;
		case ColorBlindMode.Deuteranopia:
			filterMatrix.SetColumn(0, new Vector4(0.625f, 0.375f, 0f, 0f));
			filterMatrix.SetColumn(1, new Vector4(0.7f, 0.3f, 0f, 0f));
			filterMatrix.SetColumn(2, new Vector4(0f, 0.3f, 0.7f, 0f));
			filterMatrix.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
			break;
		case ColorBlindMode.Tritanopia:
			filterMatrix.SetColumn(0, new Vector4(0.95f, 0.05f, 0f, 0f));
			filterMatrix.SetColumn(1, new Vector4(0f, 0.433f, 0.567f, 0f));
			filterMatrix.SetColumn(2, new Vector4(0f, 0.475f, 0.525f, 0f));
			filterMatrix.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
			break;
		}
		FilterMaterial.SetMatrix("_Filter", filterMatrix);
		Graphics.Blit(source, destination, FilterMaterial);
	}
}
