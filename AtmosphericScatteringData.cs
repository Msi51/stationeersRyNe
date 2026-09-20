using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot]
public class AtmosphericScatteringData
{
	[XmlArray("RayleighColorRampColorKey")]
	public List<GradientColorKey> RayleighColorRampColorKey = new List<GradientColorKey>();

	[XmlArray("RayleighColorRampAlphaKey")]
	public List<GradientAlphaKey> RayleighColorRampAlphaKey = new List<GradientAlphaKey>();

	[XmlElement]
	public GradientMode RayleighColorRampMode;

	[XmlElement]
	public float WorldRayleighColorIntensity = 1f;

	[XmlElement]
	public float WorldRayleighDensity = 10f;

	[XmlElement]
	public float WorldRayleighExtinctionFactor = 1.1f;

	[XmlElement]
	public float WorldRayleighIndirectScatter = 0.33f;

	[XmlElement]
	public float WorldMieColorIntensity = 1f;

	[XmlArray("MieColorRampColorKey")]
	public List<GradientColorKey> MieColorRampColorKey = new List<GradientColorKey>();

	[XmlArray("MieColorRampAlphaKey")]
	public List<GradientAlphaKey> MieColorRampAlphaKey = new List<GradientAlphaKey>();

	[XmlElement]
	public GradientMode MieColorRampMode;

	[XmlElement]
	public float WorldMieDensity = 15f;

	[XmlElement]
	public float WorldMieExtinctionFactor;

	[XmlElement]
	public float WorldMiePhaseAnisotropy = 0.9f;

	[XmlElement]
	public float WorldNearScatterPush;

	[XmlElement]
	public float WorldNormalDistance = 1000f;

	[XmlElement]
	public Color HeightRayleighColor = Color.white;

	[XmlElement]
	public float HeightRayleighIntensity = 1f;

	[XmlElement]
	public float HeightRayleighDensity = 10f;

	[XmlElement]
	public float HeightMieDensity;

	[XmlElement]
	public float HeightExtinctionFactor = 1.1f;

	[XmlElement]
	public float HeightSeaLevel;

	[XmlElement]
	public float HeightDistance = 50f;

	[XmlElement]
	public Vector3 HeightPlaneShift = Vector3.zero;

	[XmlElement]
	public float HeightNearScatterPush;

	[XmlElement]
	public float HeightNormalDistance = 1000f;

	[XmlElement]
	public bool UseOcclusion;

	[XmlElement]
	public float OcclusionBias;

	[XmlElement]
	public float OcclusionBiasIndirect = 0.6f;

	[XmlElement]
	public float OcclusionBiasClouds = 0.3f;

	[XmlElement]
	public AtmosphericScattering.OcclusionDownscale OcclusionDownscale = AtmosphericScattering.OcclusionDownscale.x2;

	[XmlElement]
	public AtmosphericScattering.OcclusionSamples OcclusionSamples;

	[XmlElement]
	public bool OcclusionDepthFixup = true;

	[XmlElement]
	public float OcclusionDepthThreshold = 25f;

	[XmlElement]
	public bool OcclusionFullSky;

	[XmlElement]
	public float OcclusionBiasSkyRayleigh = 0.2f;

	[XmlElement]
	public float OcclusionBiasSkyMie = 0.4f;

	[XmlElement]
	public float WorldScaleExponent = 1f;

	[XmlElement]
	public bool ForcePerPixel;

	[XmlElement]
	public bool ForcePostEffect;
}
