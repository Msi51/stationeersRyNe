using TerrainSystem;
using UnityEngine;

public class TerrainShaderScript
{
	private static readonly int SPHERE_AMOUNT = Shader.PropertyToID("_GlobalSphereAmount");

	private static readonly int SPHERE_START = Shader.PropertyToID("_GlobalSphereStart");

	private static readonly int MINABLE_RENDER_DISTANCE = Shader.PropertyToID("_GlobalMinableRenderDistance");

	private static readonly int MACRO_TEX = Shader.PropertyToID("_MacroTex");

	private static readonly int MACRO_NORMAL_TEX = Shader.PropertyToID("_MacroNormal");

	private static readonly int MACRO_SCALE = Shader.PropertyToID("_MacroScale");

	private static readonly int MACRO_BLEND = Shader.PropertyToID("_MacroBlend");

	private static readonly int MACRO_NORMAL_SCALE = Shader.PropertyToID("_MacroNormalScale");

	private static readonly int DETAIL_FADE_START = Shader.PropertyToID("_DetailFadeStart");

	private static readonly int DETAIL_FADE_END = Shader.PropertyToID("_DetailFadeEnd");

	private static readonly int BLEND_SHARPNESS = Shader.PropertyToID("_BlendSharpness");

	private static readonly int TESS_EDGE = Shader.PropertyToID("_TessEdge");

	private static readonly int DETAIL_TEX_UV_SCALE = Shader.PropertyToID("_UVScale");

	private static readonly int FRACTAL_DEPTH_SCALE = Shader.PropertyToID("_FractalDepthScale");

	private static readonly int LOD_MIN = Shader.PropertyToID("_LodMin");

	private static readonly int LOD_MAX = Shader.PropertyToID("_LodMax");

	private static readonly int MIPMAP_BIAS = Shader.PropertyToID("_Bias");

	private static readonly int DISPLACEMENT_MAP_COMBINED = Shader.PropertyToID("_DisplacementMapPacked");

	private static readonly int DETAIL_TEX_ONE = Shader.PropertyToID("_MainTex1");

	private static readonly int BUMP_MAP_ONE = Shader.PropertyToID("_BumpMap1");

	private static readonly int DISP_STRENGTH_ONE = Shader.PropertyToID("_DispStrengthOne");

	private static readonly int ROUGHNESS_ONE = Shader.PropertyToID("_RoughnessOne");

	private static readonly int NORMAL_POWER_ONE = Shader.PropertyToID("_NormalPowerOne");

	private static readonly int EMISSION_INTENSITY_ONE = Shader.PropertyToID("_EmissionIntensityOne");

	private static readonly int DETAIL_TEX_TWO = Shader.PropertyToID("_MainTex2");

	private static readonly int BUMP_MAP_TWO = Shader.PropertyToID("_BumpMap2");

	private static readonly int DISP_STRENGTH_TWO = Shader.PropertyToID("_DispStrengthTwo");

	private static readonly int ROUGHNESS_TWO = Shader.PropertyToID("_RoughnessTwo");

	private static readonly int NORMAL_POWER_TWO = Shader.PropertyToID("_NormalPowerTwo");

	private static readonly int EMISSION_INTENSITY_TWO = Shader.PropertyToID("_EmissionIntensityTwo");

	public const float DEFAULT_SPHERE_START = 144f;

	private const float LOW_TESS_EDGE = 3f;

	private const float MED_TESS_EDGE = 8f;

	private const float HIGH_TESS_EDGE = 30f;

	private const float EXTREME_TESS_EDGE = 60f;

	public static void ApplyMaterialSettings(Material terrainMaterial, TerrainSettings terrainSettings)
	{
		MaterialSettings materialSettings = terrainSettings.MaterialSettings;
		float v = 1f / (float)terrainSettings.Size;
		if (terrainSettings.Curvature != null)
		{
			SetGlobalCurvature(terrainSettings.Curvature);
			VoxelConstants.WorldCurvature = terrainSettings.Curvature;
		}
		else
		{
			SetGlobalCurvature(0f);
			VoxelConstants.WorldCurvature = 0f;
		}
		SetGlobalSphereStart(144f);
		SetLodMin(terrainMaterial, materialSettings.DetailTextureData.LodMin);
		SetMipmapBias(terrainMaterial, materialSettings.DetailTextureData.MipmapBias);
		SetDetailTextureUVScale(terrainMaterial, materialSettings.DetailTextureData.UVScale);
		SetFractalDepthScale(terrainMaterial, materialSettings.DetailTextureData.FractalDepthScale);
		SetNormalPowerOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Normal.Value);
		SetNormalPowerTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Normal.Value);
		SetSmoothnessOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Roughness.Value);
		SetSmoothnessTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Roughness.Value);
		SetMacroTexture(terrainMaterial, materialSettings.MacroTextureData.Albedo.Texture);
		SetMacroNormalTexture(terrainMaterial, materialSettings.MacroTextureData.Normal.Texture);
		SetMacroScale(terrainMaterial, v);
		SetMacroNormalScale(terrainMaterial, materialSettings.MacroTextureData.Normal.Value);
		SetMacroBlend(terrainMaterial, materialSettings.MacroTextureData.MacroTextureBlend);
		SetDetailFadeStart(terrainMaterial, materialSettings.DetailTextureData.DetailFadeStart);
		SetDetailFadeEnd(terrainMaterial, materialSettings.DetailTextureData.DetailFadeEnd);
		SetDisplacementAmountOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Displacement.Value);
		SetDisplacementAmountTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Displacement.Value);
		SetDetailAlbedoOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Albedo.Texture);
		SetNormalMapOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Normal.Texture);
		SetDetailTexTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Albedo.Texture);
		SetNormalMapTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Normal.Texture);
		SetBlendSharpness(terrainMaterial, materialSettings.DetailTextureData.BlendSharpness);
		Texture2D packedAlbedoAndEmissive = materialSettings.DetailTextureData.GroupOne.GetPackedAlbedoAndEmissive();
		SetDetailAlbedoOne(terrainMaterial, packedAlbedoAndEmissive);
		SetEmissionIntensityOne(terrainMaterial, materialSettings.DetailTextureData.GroupOne.Emissive?.Value ?? 0f);
		Texture2D packedAlbedoAndEmissive2 = materialSettings.DetailTextureData.GroupTwo.GetPackedAlbedoAndEmissive();
		SetDetailTexTwo(terrainMaterial, packedAlbedoAndEmissive2);
		SetEmissionIntensityTwo(terrainMaterial, materialSettings.DetailTextureData.GroupTwo.Emissive?.Value ?? 0f);
		Color32[] array = PackDisplacementAndRoughness(materialSettings);
		int num = (int)Mathf.Sqrt(array.Length);
		Texture2D texture2D = new Texture2D(num, num, TextureFormat.RGBA32, mipChain: false);
		texture2D.SetPixels32(array);
		texture2D.Apply();
		SetPackedDisplacementMap(terrainMaterial, texture2D);
	}

	private static Color32[] PackDisplacementAndRoughness(MaterialSettings settings)
	{
		Texture2D texture2D = settings?.DetailTextureData?.GroupOne?.Displacement?.Texture;
		Texture2D texture2D2 = settings?.DetailTextureData?.GroupTwo?.Displacement?.Texture;
		Texture2D texture2D3 = settings?.DetailTextureData?.GroupOne?.Roughness?.Texture;
		Texture2D texture2D4 = settings?.DetailTextureData?.GroupTwo?.Roughness?.Texture;
		if (!texture2D && !texture2D2 && !texture2D3 && !texture2D4)
		{
			return null;
		}
		int targetW = 1;
		int targetH = 1;
		UpdateSize(texture2D);
		UpdateSize(texture2D2);
		UpdateSize(texture2D3);
		UpdateSize(texture2D4);
		byte[] redChannelOrFallback = GetRedChannelOrFallback(texture2D, targetW, targetH, 0);
		byte[] redChannelOrFallback2 = GetRedChannelOrFallback(texture2D2, targetW, targetH, 0);
		byte[] redChannelOrFallback3 = GetRedChannelOrFallback(texture2D3, targetW, targetH, byte.MaxValue);
		byte[] redChannelOrFallback4 = GetRedChannelOrFallback(texture2D4, targetW, targetH, byte.MaxValue);
		int num = targetW * targetH;
		Color32[] array = new Color32[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new Color32(redChannelOrFallback[i], redChannelOrFallback2[i], redChannelOrFallback3[i], redChannelOrFallback4[i]);
		}
		return array;
		void UpdateSize(Texture texture)
		{
			if ((bool)texture)
			{
				if (texture.width > targetW)
				{
					targetW = texture.width;
				}
				if (texture.height > targetH)
				{
					targetH = texture.height;
				}
			}
		}
	}

	private static byte[] GetRedChannelOrFallback(Texture source, int w, int h, byte fallback)
	{
		byte[] array = new byte[w * h];
		if (!source)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = fallback;
			}
			return array;
		}
		Color32[] pixels = ResizeToReadable(source, w, h).GetPixels32();
		for (int j = 0; j < pixels.Length; j++)
		{
			array[j] = pixels[j].r;
		}
		return array;
	}

	private static Texture2D ResizeToReadable(Texture source, int width, int height)
	{
		RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		RenderTexture active = RenderTexture.active;
		FilterMode filterMode = source.filterMode;
		source.filterMode = FilterMode.Bilinear;
		Graphics.Blit(source, temporary);
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true);
		RenderTexture.active = temporary;
		texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		source.filterMode = filterMode;
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		return texture2D;
	}

	public static void SetMinableRenderDistance(float v)
	{
		Shader.SetGlobalFloat(MINABLE_RENDER_DISTANCE, v);
	}

	public static void SetGlobalCurvature(float v)
	{
		Shader.SetGlobalFloat(SPHERE_AMOUNT, v);
	}

	public static void SetGlobalSphereStart(float v)
	{
		Shader.SetGlobalFloat(SPHERE_START, v);
	}

	public static void SetMacroTexture(Material m, Texture t)
	{
		m?.SetTexture(MACRO_TEX, t);
	}

	public static void SetMacroNormalTexture(Material m, Texture t)
	{
		m?.SetTexture(MACRO_NORMAL_TEX, t);
	}

	public static void SetMacroScale(Material m, float v)
	{
		m?.SetFloat(MACRO_SCALE, v);
	}

	public static void SetMacroBlend(Material m, float v)
	{
		m?.SetFloat(MACRO_BLEND, v);
	}

	public static void SetMacroNormalScale(Material m, float v)
	{
		m?.SetFloat(MACRO_NORMAL_SCALE, v);
	}

	public static void SetDetailFadeStart(Material m, float v)
	{
		m?.SetFloat(DETAIL_FADE_START, v);
	}

	public static void SetDetailFadeEnd(Material m, float v)
	{
		m?.SetFloat(DETAIL_FADE_END, v);
	}

	public static void SetDetailAlbedoOne(Material m, Texture t)
	{
		m?.SetTexture(DETAIL_TEX_ONE, t);
	}

	public static void SetNormalMapOne(Material m, Texture t)
	{
		m?.SetTexture(BUMP_MAP_ONE, t);
	}

	public static void SetDisplacementAmountOne(Material m, float v)
	{
		m?.SetFloat(DISP_STRENGTH_ONE, v);
	}

	public static void SetDisplacementAmountTwo(Material m, float v)
	{
		m?.SetFloat(DISP_STRENGTH_TWO, v);
	}

	public static void SetDetailTexTwo(Material m, Texture t)
	{
		m?.SetTexture(DETAIL_TEX_TWO, t);
	}

	public static void SetNormalMapTwo(Material m, Texture t)
	{
		m?.SetTexture(BUMP_MAP_TWO, t);
	}

	public static void SetBlendSharpness(Material m, float v)
	{
		m?.SetFloat(BLEND_SHARPNESS, v);
	}

	public static void SetPackedDisplacementMap(Material m, Texture t)
	{
		m?.SetTexture(DISPLACEMENT_MAP_COMBINED, t);
	}

	public static void SetTessEdge(Material m, float v)
	{
		m?.SetFloat(TESS_EDGE, v);
	}

	public static void SetSmoothnessOne(Material m, float v)
	{
		m?.SetFloat(ROUGHNESS_ONE, v);
	}

	public static void SetSmoothnessTwo(Material m, float v)
	{
		m?.SetFloat(ROUGHNESS_TWO, v);
	}

	public static void SetDetailTextureUVScale(Material m, float v)
	{
		m?.SetFloat(DETAIL_TEX_UV_SCALE, v);
	}

	public static void SetFractalDepthScale(Material m, float v)
	{
		m?.SetFloat(FRACTAL_DEPTH_SCALE, v);
	}

	public static void SetLodMin(Material m, float v)
	{
		m?.SetFloat(LOD_MIN, v);
	}

	public static void SetLodMax(Material m, float v)
	{
		m?.SetFloat(LOD_MAX, v);
	}

	public static void SetMipmapBias(Material m, float v)
	{
		m?.SetFloat(MIPMAP_BIAS, v);
	}

	public static void SetNormalPowerOne(Material m, float v)
	{
		m?.SetFloat(NORMAL_POWER_ONE, v);
	}

	public static void SetNormalPowerTwo(Material m, float v)
	{
		m?.SetFloat(NORMAL_POWER_TWO, v);
	}

	public static void SetEmissionIntensityOne(Material m, float v)
	{
		m?.SetFloat(EMISSION_INTENSITY_ONE, v);
	}

	public static void SetEmissionIntensityTwo(Material m, float v)
	{
		m?.SetFloat(EMISSION_INTENSITY_TWO, v);
	}

	public static void SetTerrainDetail(string detailString)
	{
		if ((bool)VoxelTerrain.Instance?.TerrainMaterial)
		{
			switch (detailString)
			{
			case "Off":
				SetTessEdge(VoxelTerrain.Instance.TerrainMaterial, 1f);
				break;
			case "Low":
				SetTessEdge(VoxelTerrain.Instance.TerrainMaterial, 3f);
				break;
			case "Medium":
				SetTessEdge(VoxelTerrain.Instance.TerrainMaterial, 8f);
				break;
			case "High":
				SetTessEdge(VoxelTerrain.Instance.TerrainMaterial, 30f);
				break;
			case "Extreme":
				SetTessEdge(VoxelTerrain.Instance.TerrainMaterial, 60f);
				break;
			}
		}
	}
}
