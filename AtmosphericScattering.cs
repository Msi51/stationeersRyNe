using System;
using System.Collections.Generic;
using Assets.Features.AtmosphericScattering.Code;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class AtmosphericScattering : ManagerBase
{
	public enum OcclusionDownscale
	{
		x1 = 1,
		x2 = 2,
		x4 = 4
	}

	public enum OcclusionSamples
	{
		x64,
		x164,
		x244
	}

	public enum ScatterDebugMode
	{
		None,
		Scattering,
		Occlusion,
		OccludedScattering,
		Rayleigh,
		Mie,
		Height
	}

	public enum DepthTexture
	{
		Enable,
		Disable,
		Ignore
	}

	[Header("World Components")]
	public Gradient worldRayleighColorRamp;

	public float worldRayleighColorIntensity = 1f;

	public float worldRayleighDensity = 10f;

	public float worldRayleighExtinctionFactor = 1.1f;

	public float worldRayleighIndirectScatter = 0.33f;

	public Gradient worldMieColorRamp;

	public float worldMieColorIntensity = 1f;

	public float worldMieDensity = 15f;

	public float worldMieExtinctionFactor;

	public float worldMiePhaseAnisotropy = 0.9f;

	public float worldNearScatterPush;

	public float worldNormalDistance = 1000f;

	[Header("Height Components")]
	public Color heightRayleighColor = Color.white;

	public float heightRayleighIntensity = 1f;

	public float heightRayleighDensity = 10f;

	public float heightMieDensity;

	public float heightExtinctionFactor = 1.1f;

	public float heightSeaLevel;

	public float heightDistance = 50f;

	public Vector3 heightPlaneShift = Vector3.zero;

	public float heightNearScatterPush;

	public float heightNormalDistance = 1000f;

	[Header("Sky Dome")]
	public Vector3 skyDomeScale = new Vector3(1f, 0.1f, 1f);

	public Vector3 skyDomeRotation = Vector3.zero;

	public Transform skyDomeTrackedYawRotation;

	public bool skyDomeVerticalFlip;

	public Cubemap skyDomeCube;

	public float skyDomeExposure = 1f;

	public Color skyDomeTint = Color.white;

	[HideInInspector]
	public Vector3 skyDomeOffset = Vector3.zero;

	[Header("Scatter Occlusion")]
	public bool useOcclusion;

	public float occlusionBias;

	public float occlusionBiasIndirect = 0.6f;

	public float occlusionBiasClouds = 0.3f;

	public OcclusionDownscale occlusionDownscale = OcclusionDownscale.x2;

	public OcclusionSamples occlusionSamples;

	public bool occlusionDepthFixup = true;

	public float occlusionDepthThreshold = 25f;

	public bool occlusionFullSky;

	public float occlusionBiasSkyRayleigh = 0.2f;

	public float occlusionBiasSkyMie = 0.4f;

	[Header("Other")]
	public float worldScaleExponent = 1f;

	public bool forcePerPixel;

	public bool forcePostEffect;

	[Tooltip("Soft clouds need depth values. Ignore means externally controlled.")]
	public DepthTexture depthTexture;

	public ScatterDebugMode debugMode;

	[HideInInspector]
	public Shader occlusionShader;

	private bool m_isAwake;

	private Camera m_currentCamera;

	private Material m_occlusionMaterial;

	private CommandBuffer m_occlusionCmdAfterShadows;

	private CommandBuffer m_occlusionCmdBeforeScreen;

	private static readonly int USkyDomeOffset = Shader.PropertyToID("u_SkyDomeOffset");

	private static readonly int USkyDomeScale = Shader.PropertyToID("u_SkyDomeScale");

	private static readonly int USkyDomeCube = Shader.PropertyToID("u_SkyDomeCube");

	private static readonly int USkyDomeExposure = Shader.PropertyToID("u_SkyDomeExposure");

	private static readonly int USkyDomeTint = Shader.PropertyToID("u_SkyDomeTint");

	private static readonly int UShadowBias = Shader.PropertyToID("u_ShadowBias");

	private static readonly int UShadowBiasIndirect = Shader.PropertyToID("u_ShadowBiasIndirect");

	private static readonly int UShadowBiasClouds = Shader.PropertyToID("u_ShadowBiasClouds");

	private static readonly int UShadowBiasSkyRayleighMie = Shader.PropertyToID("u_ShadowBiasSkyRayleighMie");

	private static readonly int UOcclusionDepthThreshold = Shader.PropertyToID("u_OcclusionDepthThreshold");

	private static readonly int UWorldScaleExponent = Shader.PropertyToID("u_WorldScaleExponent");

	private static readonly int UWorldNormalDistanceRcp = Shader.PropertyToID("u_WorldNormalDistanceRcp");

	private static readonly int UWorldNearScatterPush = Shader.PropertyToID("u_WorldNearScatterPush");

	private static readonly int UWorldRayleighDensity = Shader.PropertyToID("u_WorldRayleighDensity");

	private static readonly int UMiePhaseAnisotropy = Shader.PropertyToID("u_MiePhaseAnisotropy");

	private static readonly int URayleighInScatterPct = Shader.PropertyToID("u_RayleighInScatterPct");

	private static readonly int UHeightNormalDistanceRcp = Shader.PropertyToID("u_HeightNormalDistanceRcp");

	private static readonly int UHeightNearScatterPush = Shader.PropertyToID("u_HeightNearScatterPush");

	private static readonly int UHeightRayleighDensity = Shader.PropertyToID("u_HeightRayleighDensity");

	private static readonly int UHeightSeaLevel = Shader.PropertyToID("u_HeightSeaLevel");

	private static readonly int UHeightDistanceRcp = Shader.PropertyToID("u_HeightDistanceRcp");

	private static readonly int UHeightPlaneShift = Shader.PropertyToID("u_HeightPlaneShift");

	private static readonly int UHeightRayleighColor = Shader.PropertyToID("u_HeightRayleighColor");

	private static readonly int UHeightExtinctionFactor = Shader.PropertyToID("u_HeightExtinctionFactor");

	private static readonly int URayleighExtinctionFactor = Shader.PropertyToID("u_RayleighExtinctionFactor");

	private static readonly int UMieExtinctionFactor = Shader.PropertyToID("u_MieExtinctionFactor");

	private static readonly int URayleighColorM20 = Shader.PropertyToID("u_RayleighColorM20");

	private static readonly int URayleighColorM10 = Shader.PropertyToID("u_RayleighColorM10");

	private static readonly int URayleighColorO00 = Shader.PropertyToID("u_RayleighColorO00");

	private static readonly int URayleighColorP10 = Shader.PropertyToID("u_RayleighColorP10");

	private static readonly int URayleighColorP20 = Shader.PropertyToID("u_RayleighColorP20");

	private static readonly int UMieColorM20 = Shader.PropertyToID("u_MieColorM20");

	private static readonly int UMieColorO00 = Shader.PropertyToID("u_MieColorO00");

	private static readonly int UMieColorP20 = Shader.PropertyToID("u_MieColorP20");

	private static readonly int UAtmosphericsDebugMode = Shader.PropertyToID("u_AtmosphericsDebugMode");

	private static readonly int USkyDomeRotation = Shader.PropertyToID("u_SkyDomeRotation");

	private static readonly int USunDirection = Shader.PropertyToID("u_SunDirection");

	private static readonly int UWorldMieDensity = Shader.PropertyToID("u_WorldMieDensity");

	private static readonly int UHeightMieDensity = Shader.PropertyToID("u_HeightMieDensity");

	private static readonly int UDepthTextureScaledTexelSize = Shader.PropertyToID("u_DepthTextureScaledTexelSize");

	private static readonly int UCameraPosition = Shader.PropertyToID("u_CameraPosition");

	private static readonly int UViewportCorner = Shader.PropertyToID("u_ViewportCorner");

	private static readonly int UViewportRight = Shader.PropertyToID("u_ViewportRight");

	private static readonly int UViewportUp = Shader.PropertyToID("u_ViewportUp");

	private static readonly int UOcclusionSkyRefDistance = Shader.PropertyToID("u_OcclusionSkyRefDistance");

	private static readonly int UOcclusionTexture = Shader.PropertyToID("u_OcclusionTexture");

	private static float _fogIntensity;

	private static readonly int AtmosphereThickness = Shader.PropertyToID("_AtmosphereThickness");

	public static readonly int Exposure = Shader.PropertyToID("_Exposure");

	public static float DefaultAtmosphereThickness = 1f;

	public static float DefaultSkyboxExposure = 1f;

	private const float FOG_CHANGE_SPEED = 2f;

	private const float ECLIPSE_FOG_MULTIPLIER = 0.2f;

	public const float INTENSITY_MIN = 0.1f;

	public const float INTENSITY_MAX = 0.15f;

	public static AtmosphericScattering instance { get; private set; }

	public static float FogIntensity => _fogIntensity;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (!GetComponent<MeshFilter>())
		{
			MeshFilter meshFilter = base.gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = new Mesh();
			meshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
			meshFilter.sharedMesh.SetTriangles((int[])null, 0);
		}
		if (!GetComponent<MeshRenderer>())
		{
			MeshRenderer meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
			bool useLightProbes = (meshRenderer.receiveShadows = false);
			meshRenderer.useLightProbes = useLightProbes;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		}
		if (occlusionShader == null)
		{
			occlusionShader = Shader.Find("Hidden/AtmosphericScattering_Occlusion");
		}
		m_occlusionMaterial = new Material(occlusionShader);
		m_occlusionMaterial.hideFlags = HideFlags.HideAndDontSave;
		InitRayleighColorRamp();
		InitMieColorRampColorRamp();
		m_isAwake = true;
	}

	private void UpdateAtmosphericScatteringToGlobalAtmosphere(ref float desiredFogIntensity)
	{
		Atmosphere atmosphere = PlanetaryAtmosphereSimulation.ReadOnlyGlobal(new WorldGrid(Grid3.zero));
		AtmosphericScatteringBlend atmosphericScatteringBlend = new AtmosphericScatteringBlend(WorldSetting.Current.Data, atmosphere);
		worldRayleighDensity = atmosphericScatteringBlend.WorldRayleighDensity;
		worldMieDensity = atmosphericScatteringBlend.WorldMieDensity;
		heightRayleighDensity = atmosphericScatteringBlend.HeightRayleighDensity;
		heightMieDensity = atmosphericScatteringBlend.HeightMieDensity;
		if (worldRayleighColorRamp == null)
		{
			worldRayleighColorRamp = new Gradient();
		}
		worldRayleighColorRamp.SetKeys(atmosphericScatteringBlend.RayleighColorRampColorKey.ToArray(), atmosphericScatteringBlend.RayleighColorRampAlphaKey.ToArray());
		if (worldMieColorRamp == null)
		{
			worldMieColorRamp = new Gradient();
		}
		worldMieColorRamp.SetKeys(atmosphericScatteringBlend.MieColorRampColorKey.ToArray(), atmosphericScatteringBlend.MieColorRampAlphaKey.ToArray());
		heightRayleighColor = atmosphericScatteringBlend.HeightRayleighColor;
		desiredFogIntensity = Mathf.Lerp(0f, desiredFogIntensity, Mathf.Clamp01(atmosphere.PressureGasses.ToFloat() / 2f));
		RenderSettings.skybox.SetFloat(AtmosphereThickness, atmosphericScatteringBlend.AtmosphereThickness);
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.GameState != GameState.Running || WorldManager.IsGamePaused)
		{
			return;
		}
		ShadowQualitySetting.Current.Update(OrbitalSimulation.WorldSunVector);
		if (!WorldManager.AtmosphericScattering || !OrbitalSimulation.WorldSun)
		{
			RenderSettings.ambientIntensity = 0.1f;
			return;
		}
		float num = Mathf.Clamp((OrbitalSimulation.WorldSunVector.y + 0.02f) / 0.05f, WorldSetting.Current.AmbientLightMin, WorldSetting.Current.AmbientLightMax);
		float desiredFogIntensity = num;
		if (OrbitalSimulation.IsEclipse)
		{
			desiredFogIntensity = num * Mathf.Lerp(1f, 0.2f, OrbitalSimulation.EclipseRatio);
		}
		if (PlanetaryAtmosphereSimulation.IsGlobalInteraction)
		{
			UpdateAtmosphericScatteringToGlobalAtmosphere(ref desiredFogIntensity);
		}
		float y = CameraController.Instance.MainCamera.transform.position.y;
		float t = 1f - y / 1000f;
		desiredFogIntensity = Mathf.Lerp(0f, desiredFogIntensity, t);
		_fogIntensity = Mathf.Lerp(_fogIntensity, desiredFogIntensity, Time.deltaTime * 2f);
		worldRayleighColorIntensity = _fogIntensity;
		float t2 = (OrbitalSimulation.WorldSunFlare ? OrbitalSimulation.WorldSunFlare.Opacity : 1f);
		worldMieColorIntensity = _fogIntensity * 1.3f;
		worldMiePhaseAnisotropy = Mathf.Lerp(worldMiePhaseAnisotropy, Mathf.Lerp(0f, WorldSetting.Current.Data.AtmosphericScatteringData.WorldMiePhaseAnisotropy, t2), Time.deltaTime * 2f);
		heightRayleighIntensity = _fogIntensity;
		float num2 = WorldSetting.Current.Data.AtmosphericScatteringData.HeightSeaLevel;
		num2 = Mathf.Min(num2, num2 - y + 32f);
		num2 = Mathf.Lerp(num2, -1000000f, OrbitalViewController.SpaceBlend);
		heightSeaLevel = num2;
		UpdateKeywords(enable: true);
		UpdateStaticUniforms();
		WorldSetting.Current?.AmbientLighting.Apply(_fogIntensity);
		RenderSettings.ambientIntensity = RocketMath.MapToScale(WorldSetting.Current.AmbientLightMin, WorldSetting.Current.AmbientLightMax, 0.1f, 0.15f, num);
	}

	private void OnEnable()
	{
		if (m_isAwake)
		{
			UpdateKeywords(enable: true);
			UpdateStaticUniforms();
			if ((bool)instance && instance != this)
			{
				Debug.LogErrorFormat("Unexpected: AtmosphericScattering.instance already set (to: {0}). Still overriding with: {1}.", instance.name, base.name);
			}
			instance = this;
		}
	}

	private void EnsureHookedLightSource(Light light)
	{
		if ((bool)light && light.commandBufferCount != 2)
		{
			light.RemoveAllCommandBuffers();
			if (m_occlusionCmdAfterShadows != null)
			{
				m_occlusionCmdAfterShadows.Dispose();
			}
			if (m_occlusionCmdBeforeScreen != null)
			{
				m_occlusionCmdBeforeScreen.Dispose();
			}
			m_occlusionCmdAfterShadows = new CommandBuffer();
			m_occlusionCmdAfterShadows.name = "Scatter Occlusion Pass 1";
			m_occlusionCmdAfterShadows.SetGlobalTexture("u_CascadedShadowMap", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
			m_occlusionCmdBeforeScreen = new CommandBuffer();
			m_occlusionCmdBeforeScreen.name = "Scatter Occlusion Pass 2";
			light.AddCommandBuffer(LightEvent.AfterShadowMap, m_occlusionCmdAfterShadows);
			light.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, m_occlusionCmdBeforeScreen);
		}
	}

	private void OnDisable()
	{
		UpdateKeywords(enable: false);
		if (instance != this)
		{
			if ((bool)instance)
			{
				Debug.LogErrorFormat("Unexpected: AtmosphericScattering.instance set to: {0}, not to: {1}. Leaving alone.", instance.name, base.name);
			}
		}
		else
		{
			instance = null;
		}
	}

	public void UpdateKeywords(bool enable)
	{
		Shader.DisableKeyword("ATMOSPHERICS");
		Shader.DisableKeyword("ATMOSPHERICS_PER_PIXEL");
		Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION");
		Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
		Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
		Shader.DisableKeyword("ATMOSPHERICS_SUNRAYS");
		Shader.DisableKeyword("ATMOSPHERICS_DEBUG");
		if (!enable)
		{
			return;
		}
		if (!forcePerPixel)
		{
			Shader.EnableKeyword("ATMOSPHERICS");
		}
		else
		{
			Shader.EnableKeyword("ATMOSPHERICS_PER_PIXEL");
		}
		if (useOcclusion)
		{
			Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION");
			if (occlusionDepthFixup && occlusionDownscale != OcclusionDownscale.x1)
			{
				Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
			}
			if (occlusionFullSky)
			{
				Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
			}
		}
		if (debugMode != ScatterDebugMode.None)
		{
			Shader.EnableKeyword("ATMOSPHERICS_DEBUG");
		}
	}

	public void OnValidate()
	{
		if (m_isAwake)
		{
			occlusionBias = Mathf.Clamp01(occlusionBias);
			occlusionBiasIndirect = Mathf.Clamp01(occlusionBiasIndirect);
			occlusionBiasClouds = Mathf.Clamp01(occlusionBiasClouds);
			occlusionBiasSkyRayleigh = Mathf.Clamp01(occlusionBiasSkyRayleigh);
			occlusionBiasSkyMie = Mathf.Clamp01(occlusionBiasSkyMie);
			worldScaleExponent = Mathf.Clamp(worldScaleExponent, 1f, 2f);
			worldNormalDistance = Mathf.Clamp(worldNormalDistance, 1f, 10000f);
			worldNearScatterPush = Mathf.Clamp(worldNearScatterPush, -200f, 300f);
			worldRayleighDensity = Mathf.Clamp(worldRayleighDensity, 0f, 5000f);
			worldMieDensity = Mathf.Clamp(worldMieDensity, 0f, 1000f);
			worldRayleighIndirectScatter = Mathf.Clamp(worldRayleighIndirectScatter, 0f, 1f);
			heightNormalDistance = Mathf.Clamp(heightNormalDistance, 1f, 10000f);
			heightNearScatterPush = Mathf.Clamp(heightNearScatterPush, -200f, 300f);
			heightRayleighDensity = Mathf.Clamp(heightRayleighDensity, 0f, 5000f);
			heightMieDensity = Mathf.Clamp(heightMieDensity, 0f, 1000f);
			worldMiePhaseAnisotropy = Mathf.Clamp01(worldMiePhaseAnisotropy);
			skyDomeExposure = Mathf.Clamp(skyDomeExposure, 0f, 8f);
			if (instance == this)
			{
				OnDisable();
				OnEnable();
			}
		}
	}

	private void OnWillRenderObject()
	{
		if (!m_isAwake || (bool)m_currentCamera)
		{
			return;
		}
		AtmosphericScatteringSun atmosphericScatteringSun = AtmosphericScatteringSun.Instance;
		if (!atmosphericScatteringSun)
		{
			UpdateDynamicUniforms();
			return;
		}
		EnsureHookedLightSource(atmosphericScatteringSun.Light);
		m_currentCamera = Camera.current;
		if ((SystemInfo.graphicsShaderLevel >= 40 || depthTexture == DepthTexture.Enable) && m_currentCamera.depthTextureMode == DepthTextureMode.None)
		{
			m_currentCamera.depthTextureMode = DepthTextureMode.Depth;
		}
		else if (depthTexture == DepthTexture.Disable && m_currentCamera.depthTextureMode != DepthTextureMode.None)
		{
			m_currentCamera.depthTextureMode = DepthTextureMode.None;
		}
		UpdateDynamicUniforms();
		if (useOcclusion)
		{
			Transform obj = m_currentCamera.transform;
			Vector3 right = obj.right;
			Vector3 up = obj.up;
			Vector3 forward = obj.forward;
			float num = Mathf.Tan(m_currentCamera.fieldOfView * 0.5f * (MathF.PI / 180f));
			float num2 = num * m_currentCamera.aspect;
			float farClipPlane = m_currentCamera.farClipPlane;
			Vector3 vector = forward * farClipPlane;
			Vector3 vector2 = right * num2 * farClipPlane;
			Vector3 vector3 = up * num * farClipPlane;
			m_occlusionMaterial.SetVector(UCameraPosition, m_currentCamera.transform.position);
			m_occlusionMaterial.SetVector(UViewportCorner, vector - vector2 - vector3);
			m_occlusionMaterial.SetVector(UViewportRight, vector2 * 2f);
			m_occlusionMaterial.SetVector(UViewportUp, vector3 * 2f);
			float num3 = (m_currentCamera ? farClipPlane : 1000f);
			float value = (Mathf.Min(num3, QualitySettings.shadowDistance) - 1f) / num3;
			m_occlusionMaterial.SetFloat(UOcclusionSkyRefDistance, value);
			Rect pixelRect = m_currentCamera.pixelRect;
			float num4 = 1f / (float)occlusionDownscale;
			int width = Mathf.RoundToInt(pixelRect.width * num4);
			int height = Mathf.RoundToInt(pixelRect.height * num4);
			m_occlusionCmdBeforeScreen.Clear();
			m_occlusionCmdBeforeScreen.GetTemporaryRT(UOcclusionTexture, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
			m_occlusionCmdBeforeScreen.Blit(null, UOcclusionTexture, m_occlusionMaterial, (int)occlusionSamples);
			m_occlusionCmdBeforeScreen.SetGlobalTexture(UOcclusionTexture, UOcclusionTexture);
		}
	}

	private void OnRenderObject()
	{
		if (m_currentCamera == Camera.current)
		{
			m_currentCamera = null;
		}
	}

	public void UpdateStaticUniforms()
	{
		Shader.SetGlobalVector(USkyDomeOffset, skyDomeOffset);
		Shader.SetGlobalVector(USkyDomeScale, skyDomeScale);
		Shader.SetGlobalTexture(USkyDomeCube, skyDomeCube);
		Shader.SetGlobalFloat(USkyDomeExposure, skyDomeExposure);
		Shader.SetGlobalColor(USkyDomeTint, skyDomeTint);
		Shader.SetGlobalFloat(UShadowBias, useOcclusion ? occlusionBias : 1f);
		Shader.SetGlobalFloat(UShadowBiasIndirect, useOcclusion ? occlusionBiasIndirect : 1f);
		Shader.SetGlobalFloat(UShadowBiasClouds, useOcclusion ? occlusionBiasClouds : 1f);
		Shader.SetGlobalVector(UShadowBiasSkyRayleighMie, useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
		Shader.SetGlobalFloat(UOcclusionDepthThreshold, occlusionDepthThreshold);
		Shader.SetGlobalFloat(UWorldScaleExponent, worldScaleExponent);
		Shader.SetGlobalFloat(UWorldNormalDistanceRcp, 1f / worldNormalDistance);
		Shader.SetGlobalFloat(UWorldNearScatterPush, (0f - Mathf.Pow(Mathf.Abs(worldNearScatterPush), worldScaleExponent)) * Mathf.Sign(worldNearScatterPush));
		Shader.SetGlobalFloat(UWorldRayleighDensity, (0f - worldRayleighDensity) / 100000f);
		Shader.SetGlobalFloat(UMiePhaseAnisotropy, worldMiePhaseAnisotropy);
		Shader.SetGlobalVector(URayleighInScatterPct, new Vector4(1f - worldRayleighIndirectScatter, worldRayleighIndirectScatter, 0f, 0f));
		Shader.SetGlobalFloat(UHeightNormalDistanceRcp, 1f / heightNormalDistance);
		Shader.SetGlobalFloat(UHeightNearScatterPush, (0f - Mathf.Pow(Mathf.Abs(heightNearScatterPush), worldScaleExponent)) * Mathf.Sign(heightNearScatterPush));
		Shader.SetGlobalFloat(UHeightRayleighDensity, (0f - heightRayleighDensity) / 100000f);
		Shader.SetGlobalFloat(UHeightSeaLevel, heightSeaLevel);
		Shader.SetGlobalFloat(UHeightDistanceRcp, 1f / heightDistance);
		Shader.SetGlobalVector(UHeightPlaneShift, heightPlaneShift);
		Shader.SetGlobalVector(UHeightRayleighColor, (Vector4)heightRayleighColor * heightRayleighIntensity);
		Shader.SetGlobalFloat(UHeightExtinctionFactor, heightExtinctionFactor);
		Shader.SetGlobalFloat(URayleighExtinctionFactor, worldRayleighExtinctionFactor);
		Shader.SetGlobalFloat(UMieExtinctionFactor, worldMieExtinctionFactor);
		Color color = worldRayleighColorRamp.Evaluate(0f);
		Color color2 = worldRayleighColorRamp.Evaluate(0.25f);
		Color color3 = worldRayleighColorRamp.Evaluate(0.5f);
		Color color4 = worldRayleighColorRamp.Evaluate(0.75f);
		Color color5 = worldRayleighColorRamp.Evaluate(1f);
		Color color6 = worldMieColorRamp.Evaluate(0f);
		Color color7 = worldMieColorRamp.Evaluate(0.5f);
		Color color8 = worldMieColorRamp.Evaluate(1f);
		Shader.SetGlobalVector(URayleighColorM20, (Vector4)color * worldRayleighColorIntensity);
		Shader.SetGlobalVector(URayleighColorM10, (Vector4)color2 * worldRayleighColorIntensity);
		Shader.SetGlobalVector(URayleighColorO00, (Vector4)color3 * worldRayleighColorIntensity);
		Shader.SetGlobalVector(URayleighColorP10, (Vector4)color4 * worldRayleighColorIntensity);
		Shader.SetGlobalVector(URayleighColorP20, (Vector4)color5 * worldRayleighColorIntensity);
		Shader.SetGlobalVector(UMieColorM20, (Vector4)color6 * worldMieColorIntensity);
		Shader.SetGlobalVector(UMieColorO00, (Vector4)color7 * worldMieColorIntensity);
		Shader.SetGlobalVector(UMieColorP20, (Vector4)color8 * worldMieColorIntensity);
		Shader.SetGlobalFloat(UAtmosphericsDebugMode, (float)debugMode);
	}

	public void UpdateDynamicUniforms()
	{
		AtmosphericScatteringSun atmosphericScatteringSun = AtmosphericScatteringSun.Instance;
		bool flag = (object)atmosphericScatteringSun != null;
		float num = (skyDomeTrackedYawRotation ? skyDomeTrackedYawRotation.eulerAngles.x : 0f);
		Shader.SetGlobalMatrix(USkyDomeRotation, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skyDomeRotation.x, 0f, 0f), Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skyDomeRotation.x - num, 0f, 0f), Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, skyDomeVerticalFlip ? (-1f) : 1f, 1f)));
		Shader.SetGlobalVector(USunDirection, flag ? (-atmosphericScatteringSun.Transform.forward) : Vector3.down);
		Shader.SetGlobalFloat(UWorldMieDensity, flag ? ((0f - worldMieDensity) / 100000f) : 0f);
		Shader.SetGlobalFloat(UHeightMieDensity, flag ? ((0f - heightMieDensity) / 100000f) : 0f);
		Rect rect = (m_currentCamera ? m_currentCamera.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height));
		float num2 = (float)occlusionDownscale;
		Shader.SetGlobalVector(value: new Vector4(num2 / rect.width, num2 / rect.height, (0f - num2) / rect.width, (0f - num2) / rect.height), nameID: UDepthTextureScaledTexelSize);
	}

	private void InitRayleighColorRamp()
	{
		if (worldRayleighColorRamp == null)
		{
			worldRayleighColorRamp = new Gradient();
			worldRayleighColorRamp.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(new Color(0.3f, 0.4f, 0.6f), 0f),
				new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			});
		}
	}

	private void InitMieColorRampColorRamp()
	{
		if (worldMieColorRamp == null)
		{
			worldMieColorRamp = new Gradient();
			worldMieColorRamp.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(new Color(0.95f, 0.75f, 0.5f), 0f),
				new GradientColorKey(new Color(1f, 0.9f, 8f), 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			});
		}
	}

	public void Import(WorldSetting worldSetting)
	{
		AtmosphericScatteringData atmosphericScatteringData = worldSetting?.AtmosphericScatteringData;
		if (atmosphericScatteringData != null)
		{
			Import(atmosphericScatteringData);
		}
	}

	public void Import(AtmosphericScatteringData data)
	{
		if (data == null)
		{
			return;
		}
		if (data.RayleighColorRampColorKey.Count == 0)
		{
			InitRayleighColorRamp();
			ExportRayleighColorRampKeys(ref data);
		}
		else
		{
			if (worldRayleighColorRamp == null)
			{
				worldRayleighColorRamp = new Gradient();
			}
			worldRayleighColorRamp.SetKeys(data.RayleighColorRampColorKey.ToArray(), data.RayleighColorRampAlphaKey.ToArray());
			worldRayleighColorRamp.mode = data.RayleighColorRampMode;
		}
		if (data.MieColorRampAlphaKey.Count == 0)
		{
			InitMieColorRampColorRamp();
			ExportMieColorRampKeys(ref data);
		}
		else
		{
			if (worldMieColorRamp == null)
			{
				worldMieColorRamp = new Gradient();
			}
			worldMieColorRamp.SetKeys(data.MieColorRampColorKey.ToArray(), data.MieColorRampAlphaKey.ToArray());
			worldMieColorRamp.mode = data.MieColorRampMode;
		}
		DefaultAtmosphereThickness = WorldSetting.Current.SkyBox.GetFloat(AtmosphereThickness);
		DefaultSkyboxExposure = WorldSetting.Current.SkyBox.GetFloat(Exposure);
		worldRayleighColorIntensity = data.WorldRayleighColorIntensity;
		worldRayleighDensity = data.WorldRayleighDensity;
		worldRayleighExtinctionFactor = data.WorldRayleighExtinctionFactor;
		worldRayleighIndirectScatter = data.WorldRayleighIndirectScatter;
		worldMieColorIntensity = data.WorldMieColorIntensity;
		worldMieDensity = data.WorldMieDensity;
		worldMieExtinctionFactor = data.WorldMieExtinctionFactor;
		worldMiePhaseAnisotropy = data.WorldMiePhaseAnisotropy;
		worldNearScatterPush = data.WorldNearScatterPush;
		worldNormalDistance = data.WorldNormalDistance;
		heightRayleighColor = data.HeightRayleighColor;
		heightRayleighIntensity = data.HeightRayleighIntensity;
		heightRayleighDensity = data.HeightRayleighDensity;
		heightMieDensity = data.HeightMieDensity;
		heightExtinctionFactor = data.HeightExtinctionFactor;
		heightSeaLevel = data.HeightSeaLevel;
		heightDistance = data.HeightDistance;
		heightPlaneShift = data.HeightPlaneShift;
		heightNearScatterPush = data.HeightNearScatterPush;
		heightNormalDistance = data.HeightNormalDistance;
		useOcclusion = data.UseOcclusion;
		occlusionBias = data.OcclusionBias;
		occlusionBiasIndirect = data.OcclusionBiasIndirect;
		occlusionBiasClouds = data.OcclusionBiasClouds;
		occlusionDownscale = data.OcclusionDownscale;
		occlusionSamples = data.OcclusionSamples;
		occlusionDepthFixup = data.OcclusionDepthFixup;
		occlusionDepthThreshold = data.OcclusionDepthThreshold;
		occlusionFullSky = data.OcclusionFullSky;
		occlusionBiasSkyRayleigh = data.OcclusionBiasSkyRayleigh;
		occlusionBiasSkyMie = data.OcclusionBiasSkyMie;
		worldScaleExponent = data.WorldScaleExponent;
		forcePerPixel = data.ForcePerPixel;
		forcePostEffect = data.ForcePostEffect;
	}

	private void ExportRayleighColorRampKeys(ref AtmosphericScatteringData data)
	{
		if (data.RayleighColorRampColorKey == null)
		{
			data.RayleighColorRampColorKey = new List<GradientColorKey>(worldRayleighColorRamp.colorKeys);
		}
		else
		{
			data.RayleighColorRampColorKey.Clear();
			data.RayleighColorRampColorKey.AddRange(worldRayleighColorRamp.colorKeys);
		}
		if (data.RayleighColorRampAlphaKey == null)
		{
			data.RayleighColorRampAlphaKey = new List<GradientAlphaKey>(worldRayleighColorRamp.alphaKeys);
		}
		else
		{
			data.RayleighColorRampAlphaKey.Clear();
			data.RayleighColorRampAlphaKey.AddRange(worldRayleighColorRamp.alphaKeys);
		}
		data.RayleighColorRampMode = worldRayleighColorRamp.mode;
	}

	private void ExportMieColorRampKeys(ref AtmosphericScatteringData data)
	{
		if (data.MieColorRampColorKey == null)
		{
			data.MieColorRampColorKey = new List<GradientColorKey>(worldMieColorRamp.colorKeys);
		}
		else
		{
			data.MieColorRampColorKey.Clear();
			data.MieColorRampColorKey.AddRange(worldMieColorRamp.colorKeys);
		}
		if (data.MieColorRampAlphaKey == null)
		{
			data.MieColorRampAlphaKey = new List<GradientAlphaKey>(worldMieColorRamp.alphaKeys);
		}
		else
		{
			data.MieColorRampAlphaKey.Clear();
			data.MieColorRampAlphaKey.AddRange(worldMieColorRamp.alphaKeys);
		}
		data.MieColorRampMode = worldMieColorRamp.mode;
	}

	internal void Import()
	{
		Import(WorldSetting.Current);
	}
}
