using System;
using UnityEngine;

[ExecuteInEditMode]
public class Decal : Projection
{
	[SerializeField]
	private DecalType decalType;

	[SerializeField]
	private LightingModel lightingModel = LightingModel.PBR;

	[SerializeField]
	private GlossType glossType;

	[SerializeField]
	private Texture2D shapeTex;

	[SerializeField]
	private float shapeMultiplier = 1f;

	[SerializeField]
	private Texture2D albedoTex;

	[SerializeField]
	private Color albedoColor = Color.grey;

	[SerializeField]
	private Texture2D smoothnessTex;

	[SerializeField]
	private float smoothness = 0.2f;

	[SerializeField]
	private Texture2D specularTex;

	[SerializeField]
	private Color specularColor = Color.white;

	[SerializeField]
	private Texture2D metallicTex;

	[SerializeField]
	private float metallicity = 0.5f;

	[SerializeField]
	private Texture2D normalTex;

	[SerializeField]
	private float normalStrength = 1f;

	[SerializeField]
	private bool emissive;

	[SerializeField]
	private Texture2D emissionTex;

	[SerializeField]
	private Color emissionColor = Color.white;

	[SerializeField]
	private float projectionLimit = 80f;

	private Material renderMaterial;

	private int deferredPass;

	private bool[] buffers;

	public DecalType DecalType
	{
		get
		{
			return decalType;
		}
		set
		{
			decalType = value;
			ReplaceMaterial();
			UpdateMaterial();
		}
	}

	public LightingModel LightModel
	{
		get
		{
			return lightingModel;
		}
		set
		{
			lightingModel = value;
			ReplaceMaterial();
			UpdateMaterial();
		}
	}

	public GlossType GlossType
	{
		get
		{
			return glossType;
		}
		set
		{
			glossType = value;
			ReplaceMaterial();
			UpdateMaterial();
		}
	}

	public bool Emissive
	{
		get
		{
			return emissive;
		}
		set
		{
			emissive = value;
			UpdateMaterial();
		}
	}

	public Texture2D ShapeMap
	{
		get
		{
			return shapeTex;
		}
		set
		{
			shapeTex = value;
			UpdateMaterial();
		}
	}

	public float ShapeMultiplier
	{
		get
		{
			return shapeMultiplier;
		}
		set
		{
			shapeMultiplier = value;
			UpdateMaterial();
		}
	}

	public Texture2D AlbedoMap
	{
		get
		{
			return albedoTex;
		}
		set
		{
			albedoTex = value;
			UpdateMaterial();
		}
	}

	public Color AlbedoColor
	{
		get
		{
			return albedoColor;
		}
		set
		{
			albedoColor = value;
			UpdateMaterial();
		}
	}

	public Texture2D SmoothnessMap
	{
		get
		{
			return smoothnessTex;
		}
		set
		{
			smoothnessTex = value;
			UpdateMaterial();
		}
	}

	public float Smoothness
	{
		get
		{
			return smoothness;
		}
		set
		{
			smoothness = Mathf.Clamp01(value);
			UpdateMaterial();
		}
	}

	public Texture2D MetallicMap
	{
		get
		{
			return metallicTex;
		}
		set
		{
			metallicTex = value;
			UpdateMaterial();
		}
	}

	public float Metallicity
	{
		get
		{
			return metallicity;
		}
		set
		{
			metallicity = value;
			UpdateMaterial();
		}
	}

	public Texture2D SpecularMap
	{
		get
		{
			return specularTex;
		}
		set
		{
			specularTex = value;
			UpdateMaterial();
		}
	}

	public Color SpecularColor
	{
		get
		{
			return specularColor;
		}
		set
		{
			specularColor = value;
			UpdateMaterial();
		}
	}

	public Texture2D NormalMap
	{
		get
		{
			return normalTex;
		}
		set
		{
			normalTex = value;
			UpdateMaterial();
		}
	}

	public float NormalStrength
	{
		get
		{
			return normalStrength;
		}
		set
		{
			normalStrength = Mathf.Clamp(value, 0f, 4f);
			UpdateMaterial();
		}
	}

	public Texture2D EmissionMap
	{
		get
		{
			return emissionTex;
		}
		set
		{
			emissionTex = value;
			UpdateMaterial();
		}
	}

	public Color EmissionColor
	{
		get
		{
			return emissionColor;
		}
		set
		{
			emissionColor = value;
			UpdateMaterial();
		}
	}

	public float ProjectionLimit
	{
		get
		{
			return projectionLimit;
		}
		set
		{
			projectionLimit = Mathf.Clamp(value, 0f, 180f);
			UpdateMaterial();
		}
	}

	public override Material RenderMaterial => renderMaterial;

	public override int DeferredPass => deferredPass;

	public override bool DeferredPrePass
	{
		get
		{
			if (DecalType == DecalType.Roughness)
			{
				return true;
			}
			if (transparencyType != TransparencyType.Blend)
			{
				return false;
			}
			return true;
		}
	}

	protected override bool RequiresRenderer => decalType == DecalType.Full;

	protected override void UpdateMaterialProperties()
	{
		base.UpdateMaterialProperties();
		UpdateProjectionClipping();
		switch (decalType)
		{
		case DecalType.Full:
			if (lightingModel == LightingModel.PBR)
			{
				UpdateGloss();
				UpdateNormal();
				UpdateEmissive();
			}
			UpdateColor();
			break;
		case DecalType.Roughness:
			UpdateShape();
			materialProperties.SetFloat("_Glossiness", smoothness);
			if (smoothnessTex != null)
			{
				materialProperties.SetTexture("_GlossTex", smoothnessTex);
			}
			break;
		case DecalType.Normal:
			UpdateShape();
			UpdateNormal();
			break;
		}
	}

	private void UpdateShape()
	{
		if (shapeTex != null)
		{
			materialProperties.SetTexture("_MainTex", shapeTex);
		}
		materialProperties.SetFloat("_Multiplier", shapeMultiplier * base.AlphaModifier);
	}

	private void UpdateColor()
	{
		if (albedoTex != null)
		{
			materialProperties.SetTexture("_MainTex", albedoTex);
		}
		Color value = albedoColor;
		value.a *= base.AlphaModifier;
		materialProperties.SetColor("_Color", value);
	}

	private void UpdateGloss()
	{
		materialProperties.SetFloat("_Glossiness", smoothness);
		switch (GlossType)
		{
		case GlossType.Metallic:
			if (metallicTex != null)
			{
				materialProperties.SetTexture("_MetallicGlossMap", metallicTex);
			}
			else
			{
				materialProperties.SetTexture("_MetallicGlossMap", Texture2D.whiteTexture);
			}
			materialProperties.SetFloat("_Metallic", metallicity);
			break;
		case GlossType.Specular:
			if (specularTex != null)
			{
				materialProperties.SetTexture("_SpecGlossMap", specularTex);
			}
			else
			{
				materialProperties.SetTexture("_SpecGlossMap", Texture2D.whiteTexture);
			}
			materialProperties.SetColor("_SpecColor", specularColor);
			break;
		}
	}

	private void UpdateNormal()
	{
		if (normalTex != null)
		{
			materialProperties.SetTexture("_BumpMap", normalTex);
		}
		materialProperties.SetFloat("_BumpScale", normalStrength);
	}

	private void UpdateEmissive()
	{
		if (emissive)
		{
			if (emissionTex != null)
			{
				materialProperties.SetTexture("_EmissionMap", emissionTex);
			}
			materialProperties.SetColor("_EmissionColor", emissionColor);
		}
	}

	private void UpdateProjectionClipping()
	{
		float value = Mathf.Cos(MathF.PI / 180f * projectionLimit);
		materialProperties.SetFloat("_NormalCutoff", value);
	}

	protected override void UpdateDeferredRendering()
	{
		if (buffers == null || buffers.Length != 3)
		{
			buffers = new bool[3];
		}
		switch (decalType)
		{
		case DecalType.Full:
			if (lightingModel == LightingModel.Unlit)
			{
				if (base.TransparencyType == TransparencyType.Blend)
				{
					renderMaterial = DynamicDecals.Mat_Decal_Unlit;
				}
				else
				{
					renderMaterial = DynamicDecals.Mat_Decal_UnlitCutout;
				}
				buffers[0] = true;
				buffers[1] = true;
				buffers[2] = false;
				deferredPass = 1;
				break;
			}
			if (GlossType == GlossType.Metallic)
			{
				if (base.TransparencyType == TransparencyType.Blend)
				{
					renderMaterial = DynamicDecals.Mat_Decal_Metallic;
				}
				else
				{
					renderMaterial = DynamicDecals.Mat_Decal_MetallicCutout;
				}
			}
			else if (base.TransparencyType == TransparencyType.Blend)
			{
				renderMaterial = DynamicDecals.Mat_Decal_Specular;
			}
			else
			{
				renderMaterial = DynamicDecals.Mat_Decal_SpecularCutout;
			}
			buffers[0] = true;
			buffers[1] = true;
			buffers[2] = true;
			deferredPass = 2;
			break;
		case DecalType.Roughness:
			if (base.TransparencyType == TransparencyType.Blend)
			{
				renderMaterial = DynamicDecals.Mat_Decal_Roughness;
			}
			else
			{
				renderMaterial = DynamicDecals.Mat_Decal_RoughnessCutout;
			}
			buffers[0] = false;
			buffers[1] = true;
			buffers[2] = false;
			deferredPass = 0;
			break;
		case DecalType.Normal:
			if (base.TransparencyType == TransparencyType.Blend)
			{
				renderMaterial = DynamicDecals.Mat_Decal_Normal;
			}
			else
			{
				renderMaterial = DynamicDecals.Mat_Decal_NormalCutout;
			}
			buffers[0] = false;
			buffers[1] = false;
			buffers[2] = true;
			deferredPass = 0;
			break;
		}
		base.DeferredBuffers = buffers;
		base.UpdateDeferredRendering();
	}

	protected override void UpdateForwardRendering(MeshRenderer Renderer)
	{
		if (Renderer.sharedMaterials.Length != 1)
		{
			Renderer.sharedMaterials = new Material[1];
		}
		if (lightingModel == LightingModel.Unlit)
		{
			if (transparencyType == TransparencyType.Blend)
			{
				Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_Unlit);
			}
			else
			{
				Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_UnlitCutout);
			}
		}
		else
		{
			switch (glossType)
			{
			case GlossType.Metallic:
				if (transparencyType == TransparencyType.Blend)
				{
					Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_Metallic);
				}
				else
				{
					Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_MetallicCutout);
				}
				break;
			case GlossType.Specular:
				if (transparencyType == TransparencyType.Blend)
				{
					Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_Specular);
				}
				else
				{
					Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Decal_SpecularCutout);
				}
				break;
			}
		}
		base.UpdateForwardRendering(Renderer);
	}

	public void CopyAllProperties(Decal Target, bool IncludeTextures = true)
	{
		if (Target != null)
		{
			CopyBaseProperties(Target);
			ProjectionLimit = Target.ProjectionLimit;
			DecalType = Target.DecalType;
			LightModel = Target.LightModel;
			if (IncludeTextures)
			{
				ShapeMap = Target.ShapeMap;
			}
			ShapeMultiplier = Target.ShapeMultiplier;
			CopyAlbedoProperties(Target, IncludeTextures);
			CopyGlossProperties(Target, IncludeTextures);
			CopyNormalProperties(Target, IncludeTextures);
			CopyEmissiveProperties(Target, IncludeTextures);
			CopyMaskProperties(Target);
		}
		else
		{
			Debug.LogWarning("No Decal found to copy from");
		}
	}

	public void CopyAlbedoProperties(Decal Target, bool IncludeTextures = true)
	{
		if (IncludeTextures)
		{
			AlbedoMap = Target.AlbedoMap;
		}
		AlbedoColor = Target.AlbedoColor;
	}

	public void CopyGlossProperties(Decal Target, bool IncludeTextures = true)
	{
		Smoothness = Target.Smoothness;
		GlossType = Target.GlossType;
		if (IncludeTextures)
		{
			MetallicMap = Target.MetallicMap;
		}
		if (IncludeTextures)
		{
			MetallicMap = Target.MetallicMap;
		}
		Metallicity = Target.Metallicity;
		if (IncludeTextures)
		{
			SpecularMap = Target.SpecularMap;
		}
		SpecularColor = Target.SpecularColor;
	}

	public void CopyNormalProperties(Decal Target, bool IncludeTextures = true)
	{
		if (IncludeTextures)
		{
			NormalMap = Target.NormalMap;
		}
		NormalStrength = Target.NormalStrength;
	}

	public void CopyEmissiveProperties(Decal Target, bool IncludeTextures = true)
	{
		Emissive = Target.Emissive;
		if (Emissive)
		{
			if (IncludeTextures)
			{
				EmissionMap = Target.EmissionMap;
			}
			EmissionColor = Target.EmissionColor;
		}
	}
}
