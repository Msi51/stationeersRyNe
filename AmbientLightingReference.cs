using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("AmbientLighting")]
public class AmbientLightingReference
{
	public static AmbientLightingReference Default = new AmbientLightingReference
	{
		Sky = new ColorRGB(37),
		Equator = new ColorRGB(20),
		Ground = new ColorRGB(8)
	};

	public ColorRGB Sky = new ColorRGB();

	public ColorRGB Equator = new ColorRGB();

	public ColorRGB Ground = new ColorRGB();

	public void Apply(float fogIntensity = 1f)
	{
		RenderSettings.ambientSkyColor = WorldSetting.Current.AmbientLighting.Sky.ToColor() * fogIntensity;
		RenderSettings.ambientEquatorColor = WorldSetting.Current.AmbientLighting.Equator.ToColor() * fogIntensity;
		RenderSettings.ambientGroundColor = WorldSetting.Current.AmbientLighting.Ground.ToColor() * fogIntensity;
		RenderSettings.skybox.SetFloat(AtmosphericScattering.Exposure, AtmosphericScattering.DefaultSkyboxExposure * fogIntensity);
	}
}
