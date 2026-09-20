using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeTai.Asset.TranslucentImage;

public class TranslucentImage : Image, IMeshModifier
{
	public TranslucentImageSource source;

	[Tooltip("(De)Saturate them image, 1 is normal, 0 is black and white, below zero make the image negative")]
	[Range(-1f, 3f)]
	public float vibrancy = 1f;

	[Tooltip("Brighten/darken them image")]
	[Range(-1f, 1f)]
	public float brightness;

	[Tooltip("Flatten the color behind to help keep contrast on varying background")]
	[Range(0f, 1f)]
	public float flatten = 0.1f;

	private Shader correctShader;

	private static int _vibrancyPropId;

	private static int _brightnessPropId;

	private static int _flattenPropId;

	private static int _blurTexPropId;

	private static int _cropRegionPropId;

	private float oldVibrancy;

	private float oldBrightness;

	private float oldFlatten;

	[Tooltip("Blend between the sprite and background blur")]
	[Range(0f, 1f)]
	public float spriteBlending = 0.65f;

	protected override void Start()
	{
		base.Start();
		PrepShader();
		oldVibrancy = vibrancy;
		oldBrightness = brightness;
		oldFlatten = flatten;
		source = (source ? source : Object.FindObjectOfType<TranslucentImageSource>());
		material.SetTexture(_blurTexPropId, source.BlurredScreen);
		if (base.canvas != null)
		{
			base.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
		}
	}

	private void PrepShader()
	{
		correctShader = Shader.Find("UI/TranslucentImage");
		_vibrancyPropId = Shader.PropertyToID("_Vibrancy");
		_brightnessPropId = Shader.PropertyToID("_Brightness");
		_flattenPropId = Shader.PropertyToID("_Flatten");
		_blurTexPropId = Shader.PropertyToID("_BlurTex");
		_cropRegionPropId = Shader.PropertyToID("_CropRegion");
	}

	private void LateUpdate()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		if (!source)
		{
			Debug.LogError("Source missing. Add TranslucentImageSource component to your main camera, then drag the camera to Source slot");
		}
		else if (IsActive() && (bool)source.BlurredScreen)
		{
			if (!material || material.shader != correctShader)
			{
				Debug.LogError("Material using \"UI/TranslucentImage\" is required");
			}
			materialForRendering.SetTexture(_blurTexPropId, source.BlurredScreen);
			materialForRendering.SetVector(_cropRegionPropId, new Vector4(source.BlurRegion.xMin, source.BlurRegion.yMin, source.BlurRegion.xMax, source.BlurRegion.yMax));
		}
	}

	private void Update()
	{
		if (_vibrancyPropId != 0 && _brightnessPropId != 0 && _flattenPropId != 0 && !WorldManager.IsGamePaused)
		{
			SyncMaterialProperty(_vibrancyPropId, ref vibrancy, ref oldVibrancy);
			SyncMaterialProperty(_brightnessPropId, ref brightness, ref oldBrightness);
			SyncMaterialProperty(_flattenPropId, ref flatten, ref oldFlatten);
		}
	}

	private void SyncMaterialProperty(int propId, ref float value, ref float oldValue)
	{
		float num = materialForRendering.GetFloat(propId);
		if (!Mathf.Approximately(num, value))
		{
			if (!Mathf.Approximately(value, oldValue))
			{
				material.SetFloat(propId, value);
				materialForRendering.SetFloat(propId, value);
				SetMaterialDirty();
			}
			else
			{
				value = num;
			}
		}
		oldValue = value;
	}

	public virtual void ModifyMesh(VertexHelper vh)
	{
		List<UIVertex> list = new List<UIVertex>();
		vh.GetUIVertexStream(list);
		for (int i = 0; i < list.Count; i++)
		{
			UIVertex value = list[i];
			value.uv1 = new Vector2(spriteBlending, 0f);
			list[i] = value;
		}
		vh.Clear();
		vh.AddUIVertexTriangleStream(list);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetVerticesDirty();
	}

	protected override void OnDisable()
	{
		SetVerticesDirty();
		base.OnDisable();
	}

	protected override void OnDidApplyAnimationProperties()
	{
		SetVerticesDirty();
		base.OnDidApplyAnimationProperties();
	}

	public virtual void ModifyMesh(Mesh mesh)
	{
		using VertexHelper vertexHelper = new VertexHelper(mesh);
		ModifyMesh(vertexHelper);
		vertexHelper.FillMesh(mesh);
	}
}
