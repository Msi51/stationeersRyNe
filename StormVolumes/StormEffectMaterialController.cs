using UnityEngine;

namespace StormVolumes;

public static class StormEffectMaterialController
{
	public static readonly int MainLightColor = Shader.PropertyToID("_MainLightColor");

	public static readonly int MainLightDir = Shader.PropertyToID("_MainLightDir");

	public static readonly int LightCount = Shader.PropertyToID("_LightCount");

	public static readonly int LightPositions = Shader.PropertyToID("_LightPositions");

	public static readonly int LightColors = Shader.PropertyToID("_LightColors");

	public static readonly int LightParams = Shader.PropertyToID("_LightParams");

	public static readonly int Steps = Shader.PropertyToID("_Steps");

	public static readonly int StepSizeStart = Shader.PropertyToID("_StepSizeStart");

	public static readonly int StepSizeGrowth = Shader.PropertyToID("_StepSizeGrowth");

	public static readonly int DensityMult = Shader.PropertyToID("_DensityMult");

	public static readonly int NoiseSize = Shader.PropertyToID("_NoiseSize1");

	public static readonly int NoiseSpeed = Shader.PropertyToID("_MoveSpeed1");

	public static readonly int CloudThreshold1 = Shader.PropertyToID("_CloudThreshold1");

	public static readonly int CloudPower1 = Shader.PropertyToID("_CloudPower1");

	public static readonly int NoiseMult1 = Shader.PropertyToID("_NoiseMult1");

	public static readonly int CloudThreshold2 = Shader.PropertyToID("_CloudThreshold2");

	public static readonly int CloudPower2 = Shader.PropertyToID("_CloudPower2");

	public static readonly int NoiseMult2 = Shader.PropertyToID("_NoiseMult2");

	public static readonly int CloudThreshold3 = Shader.PropertyToID("_CloudThreshold3");

	public static readonly int CloudPower3 = Shader.PropertyToID("_CloudPower3");

	public static readonly int NoiseMult3 = Shader.PropertyToID("_NoiseMult3");

	public static readonly int MaskMult1 = Shader.PropertyToID("_MaskMult1");

	public static readonly int MaskMult2 = Shader.PropertyToID("_MaskMult2");

	public static readonly int MaskMult3 = Shader.PropertyToID("_MaskMult3");

	public static readonly int Color1 = Shader.PropertyToID("_Color1");

	public static readonly int Color2 = Shader.PropertyToID("_Color2");

	public static readonly int MaskColor = Shader.PropertyToID("_MaskColor");

	public static readonly int EmissiveMult = Shader.PropertyToID("_EmissiveMult");

	public static readonly int NoiseTex1 = Shader.PropertyToID("_NoiseTex1");

	public static void ApplySetting(Material material, WeatherEvent currentWeatherEvent, Vector3 direction)
	{
		if (currentWeatherEvent?.StormEffect != null && currentWeatherEvent.StormEffect.IsValid())
		{
			direction *= -1f;
			RayMarchData rayMarchData = currentWeatherEvent.StormEffect.RayMarchData;
			material.SetInt(Steps, rayMarchData.Steps);
			material.SetFloat(StepSizeStart, rayMarchData.StepSize);
			material.SetFloat(StepSizeGrowth, rayMarchData.StepGrowth);
			material.SetVector(NoiseSpeed, direction * currentWeatherEvent.StormEffect.Speed);
			material.SetFloat(NoiseSize, currentWeatherEvent.StormEffect.Size);
			NoiseLayerData layer = currentWeatherEvent.StormEffect.Layer1;
			material.SetFloat(CloudThreshold1, layer.Threshold);
			material.SetFloat(CloudPower1, layer.Power);
			material.SetFloat(NoiseMult1, layer.Multiplier);
			material.SetFloat(MaskMult1, layer.MaskMultiplier);
			NoiseLayerData layer2 = currentWeatherEvent.StormEffect.Layer2;
			material.SetFloat(CloudThreshold2, layer2.Threshold);
			material.SetFloat(CloudPower2, layer2.Power);
			material.SetFloat(NoiseMult2, layer2.Multiplier);
			material.SetFloat(MaskMult2, layer2.MaskMultiplier);
			NoiseLayerData layer3 = currentWeatherEvent.StormEffect.Layer3;
			material.SetFloat(CloudThreshold3, layer3.Threshold);
			material.SetFloat(CloudPower3, layer3.Power);
			material.SetFloat(NoiseMult3, layer3.Multiplier);
			material.SetFloat(MaskMult3, layer3.MaskMultiplier);
			material.SetColor(Color1, currentWeatherEvent.StormEffect.Color1);
			material.SetColor(Color2, currentWeatherEvent.StormEffect.Color2);
			material.SetColor(MaskColor, currentWeatherEvent.StormEffect.MaskColor);
			material.SetFloat(EmissiveMult, currentWeatherEvent.StormEffect.EmissiveMult);
		}
	}

	public static void ApplyNoiseTexture(Material material, Texture3D noiseTexture)
	{
		if (noiseTexture != null)
		{
			material.SetTexture(NoiseTex1, noiseTexture);
		}
	}
}
