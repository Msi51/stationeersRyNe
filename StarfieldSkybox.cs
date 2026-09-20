using System;
using Assets.Scripts;
using UnityEngine;

[Serializable]
public class StarfieldSkybox
{
	[SerializeField]
	public string Name;

	[SerializeField]
	public GameObject GameObject;

	[SerializeField]
	public MeshRenderer Renderer;

	[SerializeField]
	public Material Material;

	public float DefaultTransparency = 1f;

	public float DefaultMinimum;

	[ReadOnly]
	public bool Initialized;

	public static readonly int MAGNITUDE_PROPERTY = Shader.PropertyToID("_Magnitude");

	public static readonly int SUN_POSITION_PROPERTY = Shader.PropertyToID("_SunPosition");

	public static readonly int HORIZON_HEIGHT_PROPERTY = Shader.PropertyToID("_HorizonHeight");

	public static readonly int HORIZON_FADE_PROPERTY = Shader.PropertyToID("_HorizonFade");

	public static readonly int MINIMUM_PROPERTY = Shader.PropertyToID("_Minimum");

	public static readonly int BRIGHTNESS_PROPERTY = Shader.PropertyToID("_Brightness");

	public static readonly int ATMOSPHERE_PROPERTY = Shader.PropertyToID("_Atmosphere");

	public static readonly int ECLIPSE_PROPERTY = Shader.PropertyToID("_Eclipse");

	public const float NO_FADE_HEIGHT = 10000f;

	public static void SetDefaults(Material material, float defaultMinimum, float defaultTransparency)
	{
		material.SetFloat(ATMOSPHERE_PROPERTY, 0f);
		material.SetFloat(MINIMUM_PROPERTY, defaultMinimum);
		material.SetFloat(BRIGHTNESS_PROPERTY, defaultTransparency);
		material.SetVector(SUN_POSITION_PROPERTY, Vector3.zero);
	}

	public void SetDefaults()
	{
		SetDefaults(Material, DefaultMinimum, DefaultTransparency);
		SetHorizon(Material, 10000f, 0f);
		Initialized = false;
	}

	public static void UpdateInGame(Material material)
	{
		if (WorldSetting.Current != null && WorldSetting.Current.SetSunInSkybox && (object)OrbitalSimulation.WorldSun != null)
		{
			material.SetVector(SUN_POSITION_PROPERTY, OrbitalSimulation.WorldSunVector);
			material.SetFloat(ATMOSPHERE_PROPERTY, AtmosphericScattering.FogIntensity);
		}
	}

	public void Initialize()
	{
		if (WorldManager.HasGravity)
		{
			SetHorizon(Material, 0f, 0f);
		}
		else
		{
			SetHorizon(Material, 10000f, 0f);
		}
		Initialized = true;
	}

	public static void SetHorizon(Material material, float fadeHeight, float fadeSize)
	{
		material.SetFloat(HORIZON_HEIGHT_PROPERTY, fadeHeight);
		material.SetFloat(HORIZON_FADE_PROPERTY, fadeSize);
	}

	public static void SetHorizonHeight(Material material, float fadeHeight)
	{
		material.SetFloat(HORIZON_HEIGHT_PROPERTY, fadeHeight);
	}

	public static void Apply(Material material, StarData stars, float defaultTransparency = 1f)
	{
		material.SetFloat(ATMOSPHERE_PROPERTY, 0f);
		material.SetFloat(MINIMUM_PROPERTY, stars.Minimum);
		material.SetFloat(BRIGHTNESS_PROPERTY, float.IsNaN(stars.Brightness) ? defaultTransparency : stars.Brightness);
		material.SetFloat(HORIZON_HEIGHT_PROPERTY, WorldManager.HasGravity ? 0f : 10000f);
		material.SetFloat(HORIZON_FADE_PROPERTY, stars.FadeHeight);
	}

	public void SetVisible(bool toggle)
	{
		GameObject.SetActive(toggle);
	}

	public void Apply(StarData data)
	{
		Initialize();
		Apply(Material, data, DefaultTransparency);
	}
}
