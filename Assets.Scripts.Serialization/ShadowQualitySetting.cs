using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Serialization;

public struct ShadowQualitySetting
{
	private string Name;

	private ShadowQuality ShadowQuality;

	private ShadowmaskMode ShadowmaskMode;

	private ShadowResolution ShadowResolution;

	private ShadowProjection Projection;

	private int ShadowDistance;

	private int LightShadowDistance;

	private float ShadowNearPlaneOffset;

	private int Cascades;

	public float ShadowCascade2Split;

	private Vector3 ShadowCascade4Split;

	public ThingShadowMode ThingShadowMode;

	private float ThingShadowDistanceMultiplier;

	private int MaxShadowedLights;

	public static ShadowQualitySetting Current;

	private const float POW_SCALE = 0.85f;

	private const float MAX_SHADOW_DIST_MULTIPLIER = 7f;

	public static ShadowQualitySetting[] ShadowSettings = new ShadowQualitySetting[5]
	{
		new ShadowQualitySetting
		{
			Name = "Extreme",
			ShadowmaskMode = ShadowmaskMode.DistanceShadowmask,
			ShadowQuality = ShadowQuality.All,
			ShadowResolution = ShadowResolution.VeryHigh,
			Projection = ShadowProjection.StableFit,
			ShadowDistance = 104,
			LightShadowDistance = 70,
			ShadowNearPlaneOffset = 0.2f,
			Cascades = 4,
			ShadowCascade2Split = 0.18f,
			ShadowCascade4Split = new Vector3(0.039f, 0.21f, 0.65f),
			ThingShadowMode = ThingShadowMode.Extreme,
			ThingShadowDistanceMultiplier = 2f,
			MaxShadowedLights = 16
		},
		new ShadowQualitySetting
		{
			Name = "High",
			ShadowmaskMode = ShadowmaskMode.DistanceShadowmask,
			ShadowQuality = ShadowQuality.All,
			ShadowResolution = ShadowResolution.VeryHigh,
			Projection = ShadowProjection.StableFit,
			ShadowDistance = 104,
			LightShadowDistance = 50,
			ShadowNearPlaneOffset = 0.2f,
			Cascades = 4,
			ShadowCascade2Split = 0.18f,
			ShadowCascade4Split = new Vector3(0.039f, 0.21f, 0.65f),
			ThingShadowMode = ThingShadowMode.High,
			ThingShadowDistanceMultiplier = 1.8f,
			MaxShadowedLights = 8
		},
		new ShadowQualitySetting
		{
			Name = "Medium",
			ShadowmaskMode = ShadowmaskMode.DistanceShadowmask,
			ShadowQuality = ShadowQuality.All,
			ShadowResolution = ShadowResolution.VeryHigh,
			Projection = ShadowProjection.StableFit,
			ShadowDistance = 104,
			ShadowCascade2Split = 0.2f,
			ShadowCascade4Split = new Vector3(0.039f, 0.21f, 0.65f),
			LightShadowDistance = 25,
			ShadowNearPlaneOffset = 0.2f,
			Cascades = 4,
			ThingShadowMode = ThingShadowMode.Medium,
			ThingShadowDistanceMultiplier = 1.5f,
			MaxShadowedLights = 4
		},
		new ShadowQualitySetting
		{
			Name = "Low",
			ShadowmaskMode = ShadowmaskMode.Shadowmask,
			ShadowQuality = ShadowQuality.All,
			ShadowResolution = ShadowResolution.High,
			Projection = ShadowProjection.StableFit,
			ShadowDistance = 51,
			ShadowCascade2Split = 0.21f,
			ShadowCascade4Split = new Vector3(0.05f, 0.05f, 0.3f),
			LightShadowDistance = 15,
			ShadowNearPlaneOffset = 2f,
			Cascades = 2,
			ThingShadowMode = ThingShadowMode.Low,
			ThingShadowDistanceMultiplier = 1f,
			MaxShadowedLights = 2
		},
		new ShadowQualitySetting
		{
			Name = "Disabled",
			ShadowmaskMode = ShadowmaskMode.Shadowmask,
			ShadowQuality = ShadowQuality.Disable,
			ShadowResolution = ShadowResolution.High,
			Projection = ShadowProjection.StableFit,
			ShadowDistance = 51,
			ShadowCascade2Split = 0.18f,
			ShadowCascade4Split = new Vector3(0.01f, 0.025f, 0.5f),
			LightShadowDistance = 10,
			ShadowNearPlaneOffset = 2f,
			Cascades = 0,
			ThingShadowMode = ThingShadowMode.Low,
			ThingShadowDistanceMultiplier = 1f
		}
	};

	public string GetName => Name;

	public static ShadowQualitySetting GetSetting(string settingName)
	{
		ShadowQualitySetting[] shadowSettings = ShadowSettings;
		for (int i = 0; i < shadowSettings.Length; i++)
		{
			ShadowQualitySetting result = shadowSettings[i];
			if (string.Equals(settingName, result.Name))
			{
				return result;
			}
		}
		return ShadowSettings[0];
	}

	public static ShadowQualitySetting GetSettingFromOverall(string overallSettingName)
	{
		switch (overallSettingName)
		{
		case "Fantastic":
			return GetSetting("High");
		case "Beautiful":
		case "Good":
			return GetSetting("Medium");
		case "Simple":
		case "Fast":
			return GetSetting("Low");
		case "Fastest":
			return GetSetting("Disabled");
		default:
			return ShadowSettings[0];
		}
	}

	public void Apply()
	{
		Current = this;
		QualitySettings.shadows = ShadowQuality;
		OcclusionManager.ShadowQualitySetting = ShadowQuality;
		Settings.CurrentData.Shadows = Name;
		QualitySettings.shadowmaskMode = ShadowmaskMode;
		QualitySettings.shadowResolution = ShadowResolution;
		Settings.CurrentData.ShadowResolution = ShadowResolution.ToString();
		QualitySettings.shadowProjection = Projection;
		QualitySettings.shadowDistance = ShadowDistance;
		Settings.CurrentData.ShadowDistance = ShadowDistance;
		Settings.CurrentData.LightShadowDistance = LightShadowDistance;
		QualitySettings.shadowNearPlaneOffset = ShadowNearPlaneOffset;
		Settings.CurrentData.ShadowNearPlaneOffset = ShadowNearPlaneOffset;
		QualitySettings.shadowCascades = Cascades;
		Settings.CurrentData.ShadowCascades = Cascades;
		QualitySettings.shadowCascade2Split = ShadowCascade2Split;
		Settings.CurrentData.ShadowCascade2Split = ShadowCascade2Split;
		QualitySettings.shadowCascade4Split = ShadowCascade4Split;
		Settings.CurrentData.ShadowCascade4Split = ShadowCascade4Split;
		Settings.CurrentData.ThingShadowMode = ThingShadowMode.ToString();
		OcclusionManager.ThingShadowMode = ThingShadowMode;
		Settings.CurrentData.ThingShadowDistanceMultiplier = ThingShadowDistanceMultiplier;
		OcclusionManager.MaxShadowedLights = MaxShadowedLights;
		if (GameManager.GameState != GameState.None)
		{
			OcclusionManager.CheckAllOcclusion();
		}
	}

	public void Update(Vector3 solarAngle)
	{
		if (Settings.CurrentData != null && Settings.CurrentData.DistantShadows)
		{
			QualitySettings.shadowCascade4Split = GetShadowCascade4Split(solarAngle);
			QualitySettings.shadowCascade2Split = GetShadowCascade2Split(solarAngle);
			QualitySettings.shadowDistance = GetShadowDistance(solarAngle);
		}
	}

	public Vector3 GetShadowCascade4Split(Vector3 solarAngle)
	{
		float num = GetShadowDistance(solarAngle) / (float)Current.ShadowDistance;
		float x = Current.ShadowCascade4Split.x;
		float y = Current.ShadowCascade4Split.y;
		float z = Current.ShadowCascade4Split.z;
		float x2 = x / num;
		y /= num;
		z /= num;
		return new Vector3(x2, y, z);
	}

	public float GetShadowCascade2Split(Vector3 solarAngle)
	{
		float num = GetShadowDistance(solarAngle) / (float)Current.ShadowDistance;
		return Current.ShadowCascade2Split / num;
	}

	public float GetShadowDistance(Vector3 solarAngle)
	{
		float f = Mathf.Clamp01(solarAngle.y);
		return Mathf.Clamp(1f / Mathf.Pow(f, 0.85f) * (float)Current.ShadowDistance, Current.ShadowDistance, (float)Current.ShadowDistance * 7f);
	}
}
