using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts;

public static class OrbitalViewController
{
	public static bool Enabled = true;

	private const float BLEND_START_HEIGHT = 1000f;

	private const float BLEND_END_HEIGHT = 1500f;

	private const float ORBITAL_FAR_CLIP = 25000f;

	private const float ORBITAL_CURVATURE = 0.5f;

	private const float TERRAIN_SURFACE_HEIGHT = 120f;

	private const float RING_CLEARANCE = 20f;

	private const float SKYBOX_SWAP_HEIGHT = 1000f;

	private const float SKYBOX_SWAP_HYSTERESIS = 100f;

	private const string STARFIELD_SKYBOX_PATH = "WorldEnvironment/SkyBoxes/Starfield Skybox";

	private const float ATMOSPHERE_RESAMPLE_INTERVAL = 5f;

	private static readonly int AtmosphereColorId = Shader.PropertyToID("_AtmosphereColor");

	private static readonly int AtmosphereDensityId = Shader.PropertyToID("_Density");

	private static readonly int AtmosphereSunDirectionId = Shader.PropertyToID("_SunDirection");

	private static readonly int AtmosphereScatterStrengthId = Shader.PropertyToID("_ScatterStrength");

	private static readonly int AtmosphereFalloffId = Shader.PropertyToID("_FalloffFraction");

	public const float DEFAULT_ATMOSPHERE_FALLOFF = 0.25f;

	private static readonly int AtmosphereSunIntensityId = Shader.PropertyToID("_SunIntensity");

	private const float ATMOSPHERE_SUN_INTENSITY = 10f;

	private static readonly int AtmospherePlanetCenterId = Shader.PropertyToID("_PlanetCenter");

	private static readonly int AtmospherePlanetRadiusId = Shader.PropertyToID("_PlanetRadius");

	private static readonly int AtmosphereRadiusId = Shader.PropertyToID("_AtmosphereRadius");

	private static readonly int GlobalSphereFoldId = Shader.PropertyToID("_GlobalSphereFold");

	private static readonly int GlobalSphereFoldStartId = Shader.PropertyToID("_GlobalSphereFoldStart");

	private static readonly int GlobalSphereLiftId = Shader.PropertyToID("_GlobalSphereLift");

	private static readonly int GlobalSphereRecenterXId = Shader.PropertyToID("_GlobalSphereRecenterX");

	private static readonly int GlobalSphereRecenterZId = Shader.PropertyToID("_GlobalSphereRecenterZ");

	private const float FOLD_STRENGTH = 8f;

	private const float RECENTER_PARALLAX_RATIO = 0.05f;

	private const float RECENTER_PARALLAX_MAX = 50f;

	private static bool _active;

	private static float _baseFarClip;

	private static float _baseCurvature;

	private static bool _skyboxSwapped;

	private static bool _starfieldSkyboxLoadFailed;

	private static Material _worldSkybox;

	private static Material _starfieldSkybox;

	private static bool _scatteringDisabled;

	private static string _capturedWorldId;

	private static GameObject _atmosphereObject;

	private static Material _atmosphereMaterial;

	private static float _atmosphereResampleTimer;

	private static float _atmosphereDensity;

	private static Color _atmosphereColour = new Color(0.4f, 0.62f, 1f);

	private static float _sunElevationOffset;

	public static float SpaceBlend { get; private set; }

	public static float RecenterX { get; private set; }

	public static float RecenterZ { get; private set; }

	public static Color DerivedAtmosphereColour { get; private set; } = new Color(0.4f, 0.62f, 1f);

	public static float AutoAtmosphereDepth { get; private set; }

	public static float AdjustSunElevation(float sunVectorY)
	{
		if (_sunElevationOffset <= 0f)
		{
			return sunVectorY;
		}
		return Mathf.Sin(Mathf.Asin(Mathf.Clamp(sunVectorY, -1f, 1f)) + _sunElevationOffset);
	}

	public static void Update()
	{
		try
		{
			UpdateInternal();
		}
		catch (Exception arg)
		{
			Enabled = false;
			Debug.LogError($"OrbitalViewController disabled after exception: {arg}");
		}
	}

	private static void UpdateInternal()
	{
		if (!Enabled || GameManager.GameState != GameState.Running)
		{
			if (_active)
			{
				Restore(CameraController.CurrentCamera);
				return;
			}
			SpaceBlend = 0f;
			_sunElevationOffset = 0f;
			RecenterX = 0f;
			RecenterZ = 0f;
			Shader.SetGlobalFloat(GlobalSphereFoldId, 0f);
			Shader.SetGlobalFloat(GlobalSphereLiftId, 0f);
			Shader.SetGlobalFloat(GlobalSphereRecenterXId, 0f);
			Shader.SetGlobalFloat(GlobalSphereRecenterZId, 0f);
			LodMeshRenderer.SetSpaceCulling(enabled: false);
			RestoreSkybox();
			RestoreAtmosphericScattering();
			if (_atmosphereObject != null && _atmosphereObject.activeSelf)
			{
				_atmosphereObject.SetActive(value: false);
			}
			return;
		}
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera == null)
		{
			return;
		}
		Vector3 position = currentCamera.transform.position;
		float y = position.y;
		float num = (SpaceBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1000f, 1500f, y)));
		if (num <= 0f)
		{
			if (_active)
			{
				Restore(currentCamera);
			}
			return;
		}
		if (!_active)
		{
			_active = true;
			_baseFarClip = currentCamera.farClipPlane;
			_baseCurvature = VoxelConstants.WorldCurvature;
			_capturedWorldId = WorldSetting.Current?.Id;
		}
		currentCamera.farClipPlane = Mathf.Lerp(_baseFarClip, Mathf.Max(_baseFarClip, 25000f), num);
		OrbitalViewData orbitalViewData = WorldSetting.Current?.Data?.OrbitalView;
		float num3 = ((orbitalViewData != null && orbitalViewData.SurfaceHeight > 0f) ? orbitalViewData.SurfaceHeight : 120f);
		float b = ((orbitalViewData != null && orbitalViewData.Curvature > 0f) ? orbitalViewData.Curvature : 0.5f);
		float atmosphereDepth = orbitalViewData?.AtmosphereDepth ?? 0f;
		float atmosphereFalloff = ((orbitalViewData != null && orbitalViewData.AtmosphereFalloff > 0f) ? orbitalViewData.AtmosphereFalloff : 0.25f);
		float num4 = Mathf.Max(orbitalViewData?.PlanetLift ?? 0f, 0f) * num;
		float num5 = Mathf.Lerp(_baseCurvature, b, num);
		TerrainShaderScript.SetGlobalCurvature(num5);
		VoxelConstants.WorldCurvature = num5;
		float num6 = (float)VoxelConstants.Size * 0.5f * 0.92f;
		Shader.SetGlobalFloat(GlobalSphereFoldStartId, num6);
		Shader.SetGlobalFloat(GlobalSphereFoldId, 8f * num);
		Shader.SetGlobalFloat(GlobalSphereLiftId, num4);
		Vector2 vector = new Vector2(position.x, position.z);
		Vector2 vector2 = Vector2.ClampMagnitude(vector * 0.05f, 50f);
		Vector2 vector3 = (vector - vector2) * num;
		RecenterX = vector3.x;
		RecenterZ = vector3.y;
		Shader.SetGlobalFloat(GlobalSphereRecenterXId, vector3.x);
		Shader.SetGlobalFloat(GlobalSphereRecenterZId, vector3.y);
		LodMeshRenderer.SetSpaceCulling(enabled: true);
		float num7 = Mathf.Max(y - (num3 + num4), 50f);
		float num8 = Mathf.Pow(Mathf.Max(0f, num6 - 144f) * num5, 2f) * 0.001f;
		_sunElevationOffset = Mathf.Atan2(num7 + num8, num6) * num;
		float num9 = Mathf.Max(num7 + num8 - 20f, 50f);
		float num10 = num6 * num6 + num9 * num9;
		float num11 = num10 / num9;
		float sphereRadius = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(num11 * num11 - num10, 1f)), 200f, 200000f);
		Vector3 sphereCenter = new Vector3(position.x, y - num11, position.z);
		UpdateSkybox(y);
		UpdateAtmosphericScatteringState(y);
		UpdateAtmosphereLayer(currentCamera, num, sphereCenter, sphereRadius, atmosphereDepth, atmosphereFalloff);
	}

	private static void Restore(Camera camera)
	{
		_active = false;
		SpaceBlend = 0f;
		_sunElevationOffset = 0f;
		RecenterX = 0f;
		RecenterZ = 0f;
		bool flag = WorldSetting.Current?.Id == _capturedWorldId;
		if (camera != null && flag)
		{
			camera.farClipPlane = _baseFarClip;
		}
		if (flag)
		{
			TerrainShaderScript.SetGlobalCurvature(_baseCurvature);
			VoxelConstants.WorldCurvature = _baseCurvature;
		}
		Shader.SetGlobalFloat(GlobalSphereFoldId, 0f);
		Shader.SetGlobalFloat(GlobalSphereLiftId, 0f);
		Shader.SetGlobalFloat(GlobalSphereRecenterXId, 0f);
		Shader.SetGlobalFloat(GlobalSphereRecenterZId, 0f);
		LodMeshRenderer.SetSpaceCulling(enabled: false);
		RestoreSkybox();
		RestoreAtmosphericScattering();
		if (_atmosphereObject != null)
		{
			_atmosphereObject.SetActive(value: false);
		}
	}

	private static void UpdateSkybox(float cameraHeight)
	{
		if (!_skyboxSwapped && cameraHeight > 1000f)
		{
			if (_starfieldSkybox == null && !_starfieldSkyboxLoadFailed)
			{
				_starfieldSkybox = Resources.Load<Material>("WorldEnvironment/SkyBoxes/Starfield Skybox");
				if (_starfieldSkybox == null)
				{
					_starfieldSkyboxLoadFailed = true;
					Debug.LogWarning("OrbitalViewController: skybox material not found at 'WorldEnvironment/SkyBoxes/Starfield Skybox'; space skybox swap disabled.");
				}
			}
			if (!(_starfieldSkybox == null))
			{
				_worldSkybox = RenderSettings.skybox;
				RenderSettings.skybox = _starfieldSkybox;
				_skyboxSwapped = true;
			}
		}
		else if (_skyboxSwapped && cameraHeight < 900f)
		{
			RestoreSkybox();
		}
	}

	private static void RestoreSkybox()
	{
		if (_skyboxSwapped)
		{
			if (_worldSkybox != null && WorldSetting.Current?.Id == _capturedWorldId)
			{
				RenderSettings.skybox = _worldSkybox;
			}
			_worldSkybox = null;
			_skyboxSwapped = false;
		}
	}

	private static void UpdateAtmosphericScatteringState(float cameraHeight)
	{
		if (!_scatteringDisabled && cameraHeight > 1000f)
		{
			CursorManager.SetAtmosphericScattering(isOn: false);
			_scatteringDisabled = true;
		}
		else if (_scatteringDisabled && cameraHeight < 900f)
		{
			RestoreAtmosphericScattering();
		}
	}

	private static void RestoreAtmosphericScattering()
	{
		if (_scatteringDisabled)
		{
			CursorManager.UpdateAtmosphericScattering();
			_scatteringDisabled = false;
		}
	}

	private static void UpdateAtmosphereLayer(Camera camera, float blend, Vector3 sphereCenter, float sphereRadius, float atmosphereDepth, float atmosphereFalloff)
	{
		SampleWorldAtmosphere();
		bool flag = _atmosphereDensity > 0.001f;
		if (!(_atmosphereObject == null) || (flag && CreateAtmosphereLayer()))
		{
			if (_atmosphereObject.activeSelf != flag)
			{
				_atmosphereObject.SetActive(flag);
			}
			if (flag)
			{
				_atmosphereObject.transform.position = camera.transform.position;
				AutoAtmosphereDepth = Mathf.Max(sphereRadius * Mathf.Lerp(0.015f, 0.04f, _atmosphereDensity), 40f);
				float num = ((atmosphereDepth > 0f) ? atmosphereDepth : AutoAtmosphereDepth);
				_atmosphereMaterial.SetColor(AtmosphereColorId, _atmosphereColour);
				_atmosphereMaterial.SetFloat(AtmosphereDensityId, blend);
				_atmosphereMaterial.SetFloat(AtmosphereFalloffId, atmosphereFalloff);
				_atmosphereMaterial.SetFloat(AtmosphereScatterStrengthId, Mathf.Lerp(1f, 4f, _atmosphereDensity));
				_atmosphereMaterial.SetFloat(AtmosphereSunIntensityId, 10f * (1f - OrbitalSimulation.EclipseRatio));
				_atmosphereMaterial.SetVector(AtmosphereSunDirectionId, OrbitalSimulation.WorldSunVector);
				_atmosphereMaterial.SetVector(AtmospherePlanetCenterId, sphereCenter);
				_atmosphereMaterial.SetFloat(AtmospherePlanetRadiusId, sphereRadius);
				_atmosphereMaterial.SetFloat(AtmosphereRadiusId, sphereRadius + num);
			}
		}
	}

	private static bool CreateAtmosphereLayer()
	{
		Shader shader = Shader.Find("Custom/OrbitalPlanetAtmosphere");
		if (shader == null)
		{
			Debug.LogWarning("OrbitalViewController: shader 'Custom/OrbitalPlanetAtmosphere' not found; orbital atmosphere disabled.");
			_atmosphereDensity = 0f;
			return false;
		}
		_atmosphereObject = new GameObject("~OrbitalAtmosphere");
		_atmosphereObject.AddComponent<MeshFilter>().sharedMesh = BuildAtmosphereDome();
		MeshRenderer meshRenderer = _atmosphereObject.AddComponent<MeshRenderer>();
		_atmosphereMaterial = new Material(shader);
		meshRenderer.sharedMaterial = _atmosphereMaterial;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;
		return true;
	}

	private static Mesh BuildAtmosphereDome()
	{
		Vector3[] array = new Vector3[325];
		for (int i = 0; i <= 12; i++)
		{
			float f = MathF.PI * (float)i / 12f;
			for (int j = 0; j <= 24; j++)
			{
				float f2 = MathF.PI * 2f * (float)j / 24f;
				array[i * 25 + j] = new Vector3(Mathf.Sin(f) * Mathf.Cos(f2), Mathf.Cos(f), Mathf.Sin(f) * Mathf.Sin(f2)) * 100f;
			}
		}
		int[] array2 = new int[1728];
		int num = 0;
		for (int k = 0; k < 12; k++)
		{
			for (int l = 0; l < 24; l++)
			{
				int num2 = k * 25 + l;
				int num3 = num2 + 24 + 1;
				array2[num++] = num2;
				array2[num++] = num2 + 1;
				array2[num++] = num3;
				array2[num++] = num2 + 1;
				array2[num++] = num3 + 1;
				array2[num++] = num3;
			}
		}
		return new Mesh
		{
			name = "OrbitalAtmosphereDome",
			vertices = array,
			triangles = array2,
			bounds = new Bounds(Vector3.zero, Vector3.one * 200f)
		};
	}

	private static void SampleWorldAtmosphere()
	{
		_atmosphereResampleTimer -= Time.deltaTime;
		if (_atmosphereResampleTimer > 0f)
		{
			return;
		}
		_atmosphereResampleTimer = 5f;
		Atmosphere atmosphere = PlanetaryAtmosphereSimulation.ReadOnlyGlobal(new WorldGrid(Vector3.zero));
		float num = PlanetaryAtmosphereSimulation.GlobalPressure.ToFloat();
		if (atmosphere == null || num <= 0f)
		{
			_atmosphereDensity = 0f;
			return;
		}
		_atmosphereDensity = Mathf.Clamp01(Mathf.Pow(num / 100f, 0.4f));
		GasMixture gasMixture = atmosphere.GasMixture;
		float num2 = gasMixture.Oxygen.Quantity.ToFloat() + gasMixture.Nitrogen.Quantity.ToFloat();
		float num3 = gasMixture.CarbonDioxide.Quantity.ToFloat();
		float num4 = gasMixture.Methane.Quantity.ToFloat();
		float num5 = gasMixture.Pollutant.Quantity.ToFloat() + gasMixture.NitrousOxide.Quantity.ToFloat() + gasMixture.Ozone.Quantity.ToFloat() + gasMixture.HydrochloricAcid.Quantity.ToFloat() + gasMixture.Silanol.Quantity.ToFloat() + gasMixture.Hydrazine.Quantity.ToFloat();
		float num6 = gasMixture.Hydrogen.Quantity.ToFloat() + gasMixture.Helium.Quantity.ToFloat();
		float num7 = gasMixture.Steam.Quantity.ToFloat();
		float num8 = num2 + num3 + num4 + num5 + num6 + num7;
		if (num8 <= 0f)
		{
			_atmosphereDensity = 0f;
			return;
		}
		Color color = (num2 * new Color(0.4f, 0.62f, 1f) + num3 * new Color(0.85f, 0.66f, 0.45f) + num4 * new Color(0.45f, 0.8f, 0.85f) + num5 * new Color(0.7f, 0.85f, 0.4f) + num6 * new Color(0.55f, 0.6f, 1f) + num7 * new Color(0.65f, 0.78f, 1f)) / num8;
		color.a = 1f;
		DerivedAtmosphereColour = color;
		Color color2 = WorldSetting.Current?.Data?.OrbitalView?.AtmosphereColor ?? Color.clear;
		if (color2.a > 0f)
		{
			color = color2;
		}
		color.a = 1f;
		_atmosphereColour = color;
	}
}
