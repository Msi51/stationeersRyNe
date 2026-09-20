using System;
using System.Collections.Generic;
using Assets.Features.AtmosphericScattering.Code;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ch.sycoforge.Flares;
using ImGuiNET;
using TerrainSystem;
using UI.ImGuiUi;
using UnityEngine;
using Weather;

public class OrbitalSimulation
{
	public const float CELESTIAL_SPHERE_RADIUS = 200f;

	public const double KM_TO_AU = 6.6845871222684464E-09;

	public const double AU_TO_KM = 149597870.7;

	public const double AU_TO_LY = 1.5812501680078302E-05;

	public const double DEGREES_IN_CIRCLE = 360.0;

	private const int SOLAR_SKYBOX_RADIUS = 190;

	public RotatingCelestialBody PlayerBody;

	private static float _timeOfDay;

	private static float _debugScaleFactor = 1f;

	private const float REPOSITION_SPEED = 10f;

	public const float DEFAULT_SKYBOX_SCALE = 10000f;

	public static float SkyboxScale = 1f;

	private static RaycastHit[] _raycastHits = new RaycastHit[1];

	private const float TIME_SEEK_STEP = 0.1f;

	private double _offsetTime;

	public double TotalRealTimeSeconds;

	public double SimulationTimeSeconds;

	private static float _eclipseTimeout = 0f;

	private const float ECLIPSE_TIME = 4f;

	private static bool _isEclipseRaycast;

	private const float ECLIPSE_TIME_OUT_THRESHOLD = 0.5f;

	public static bool OrbitalDebugger;

	public const int DEFAULT_DAY_SECONDS = 1200;

	private static List<CelestialPrefab> _prefabs = new List<CelestialPrefab>();

	public static Light PlanetarySun;

	public static Light WorldSun;

	public static Transform WorldSunTransform;

	public static Vector3 WorldSunVector;

	public static EasyFlares WorldSunFlare;

	public static Vector3 WorldSunOffset;

	public const float SUN_REMAIN_ANGLE = 10f;

	public static float SolarIntensity;

	public float SolarConstant = 1367f;

	public const float DEFAULT_SOLAR_CONSTANT = 1367f;

	private static float _solarIrradiance;

	private List<Celestial> _bodies = new List<Celestial>();

	private Dictionary<int, Celestial> _bodiesLookup = new Dictionary<int, Celestial>();

	private double _timeScale;

	private static float _latitude;

	private static float _longitude;

	private static float _bodyScale = 1f;

	public static OrbitalSimulation System { get; } = new OrbitalSimulation("Sol", Localization.GetInterface("CelestialBodySol"));

	public Celestial PrimaryBody { get; }

	public static float TimeOfDay => _timeOfDay;

	public static bool IsEclipse => EclipseRatio > 0f;

	public static float EclipseRatio { get; private set; }

	public static float DebugScale => _debugScaleFactor;

	public static float DebugSystemScale
	{
		get
		{
			float num = 200f * _debugScaleFactor * 10f;
			if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.LocalMap)
			{
				num *= 1000f;
			}
			return num;
		}
	}

	public double TimeScale
	{
		get
		{
			return _timeScale;
		}
		private set
		{
			_timeScale = value;
			OnTimeScaleChanged();
		}
	}

	public static float BodyScale
	{
		get
		{
			return _bodyScale;
		}
		private set
		{
			_bodyScale = Mathf.Max(0.01f, value);
		}
	}

	public static bool IsValid
	{
		get
		{
			if (System?.PlayerBody != null)
			{
				return System.PrimaryBody != null;
			}
			return false;
		}
	}

	public int DayLengthSeconds { get; set; } = 1200;

	public static float EarthSolarRatio => SolarIrradiance / 1367f;

	public static float SolarIrradiance
	{
		get
		{
			return _solarIrradiance;
		}
		private set
		{
			if (!float.IsNaN(value) && !float.IsNegativeInfinity(value) && !float.IsPositiveInfinity(value))
			{
				_solarIrradiance = value;
			}
		}
	}

	public static ushort NetworkUpdateFlags { get; set; }

	public static float Latitude
	{
		get
		{
			return _latitude;
		}
		private set
		{
			_latitude = Mathf.Clamp(value, -90f, 90f);
		}
	}

	public static float Longitude
	{
		get
		{
			return _longitude;
		}
		private set
		{
			_longitude = Mathf.Clamp(value, -180f, 180f);
		}
	}

	private OrbitalSimulation(string primaryBodyId, string primaryBodyName)
	{
		PrimaryBody = new Celestial(primaryBodyId, primaryBodyName, this);
	}

	public static double GetTimeScale()
	{
		return System?.TimeScale ?? 0.0;
	}

	public static double CalculateTimeScale(OrbitalSimulation simulation)
	{
		double num = Math.Abs(simulation.PlayerBody.DegreesPerPlanetYear / 360.0);
		num /= 360.0 / simulation.PlayerBody.RotationalDegreesForSiderealDay();
		if (simulation.PlayerBody.TidallyLocked)
		{
			num = 1.0;
		}
		double num2 = num * ((double)simulation.DayLengthSeconds / 60.0) * 60.0;
		return 360.0 / num2;
	}

	private void OnTimeScaleChanged()
	{
		foreach (CelestialPrefab prefab in _prefabs)
		{
			prefab.OnTimeScaleChanged(TimeScale);
		}
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NetworkUpdateFlags |= 1;
		}
	}

	public static void SetDayTime(float clampedSunTime)
	{
		if (!IsValid)
		{
			return;
		}
		if (clampedSunTime > _timeOfDay)
		{
			while (_timeOfDay < clampedSunTime)
			{
				System.UpdateAllBodies(0.10000000149011612);
			}
			HandleUpdate();
		}
		else if (clampedSunTime < _timeOfDay)
		{
			while (_timeOfDay > clampedSunTime)
			{
				System.UpdateAllBodies(-0.10000000149011612);
			}
			while (_timeOfDay < clampedSunTime)
			{
				System.UpdateAllBodies(0.10000000149011612);
			}
			HandleUpdate();
		}
	}

	public static void SimulateTime(double realTimeDelta)
	{
		if (IsValid)
		{
			System.UpdateAllBodies(realTimeDelta);
			HandleUpdate();
		}
	}

	public static void SetRealTime(double realTime, bool publish = false)
	{
		if (IsValid)
		{
			realTime += System.GetAdjustedOffsetTime();
			SetSimulationTime(realTime * System.TimeScale, publish);
		}
	}

	public static void SetSimulationTime(double simulationTime, bool publish = false)
	{
		if (IsValid)
		{
			System.SetAllBodies(simulationTime);
			HandleUpdate(publish);
		}
	}

	private void UpdateAllBodies(double deltaRealTime)
	{
		TotalRealTimeSeconds += deltaRealTime;
		double num = deltaRealTime * TimeScale;
		SetAllBodies(SimulationTimeSeconds + num);
	}

	private double GetAdjustedOffsetTime()
	{
		return _offsetTime * ((double)DayLengthSeconds / 1200.0);
	}

	private void SetAllBodies(double simulationTime)
	{
		if (GameManager.IsRunning && OrbitalDebugger)
		{
			HandleDebugInput();
		}
		SimulationTimeSeconds = simulationTime;
		PrimaryBody.Set(simulationTime);
		PrimaryBody.SetPlayerVectorTo(PlayerBody);
		WorldSunVector = PrimaryBody.WorldVector.normalized;
		foreach (Celestial body in _bodies)
		{
			if (body != PlayerBody)
			{
				body.SetPlayerVectorTo(PlayerBody);
			}
		}
	}

	private void HandleDebugInput()
	{
		if (ConsoleWindow.IsOpen)
		{
			return;
		}
		float num = (Input.GetKey(KeyCode.LeftShift) ? 10f : 1f);
		if (Input.GetKey(KeyCode.LeftBracket))
		{
			_debugScaleFactor += Time.deltaTime * num * 0.1f;
			_debugScaleFactor = Mathf.Clamp(_debugScaleFactor, 0.01f, 1f);
		}
		else if (Input.GetKey(KeyCode.RightBracket))
		{
			_debugScaleFactor -= Time.deltaTime * num * 0.1f;
			_debugScaleFactor = Mathf.Clamp(_debugScaleFactor, 0.01f, 1f);
		}
		else if (Input.GetKey(KeyCode.Equals))
		{
			TimeScale += Time.deltaTime * num * 0.5f;
			TimeScale = Math.Clamp(TimeScale, 0.0, 100.0);
		}
		else if (Input.GetKey(KeyCode.Minus))
		{
			TimeScale -= Time.deltaTime * num * 0.5f;
			TimeScale = Math.Clamp(TimeScale, 0.0, 100.0);
		}
		else if (Input.GetKeyDown(KeyCode.Semicolon))
		{
			TimeScale = 1.0;
		}
		else if (Input.GetKey(KeyCode.UpArrow))
		{
			Latitude += Time.deltaTime * 10f * num;
		}
		else if (Input.GetKey(KeyCode.DownArrow))
		{
			Latitude -= Time.deltaTime * 10f * num;
		}
		else if (Input.GetKey(KeyCode.LeftArrow))
		{
			Longitude += Time.deltaTime * 10f * num;
		}
		else if (Input.GetKey(KeyCode.RightArrow))
		{
			Longitude -= Time.deltaTime * 10f * num;
		}
		else if (Input.GetKey(KeyCode.Keypad4))
		{
			BodyScale += Time.deltaTime * num;
			RescaleBodies();
		}
		else if (Input.GetKey(KeyCode.Keypad6))
		{
			BodyScale -= Time.deltaTime * num;
			RescaleBodies();
		}
		else if (Input.GetKey(KeyCode.Keypad8))
		{
			SkyboxScale += Time.deltaTime * 0.01f * num;
		}
		else if (Input.GetKey(KeyCode.Keypad2))
		{
			SkyboxScale -= Time.deltaTime * 0.01f * num;
		}
		else if (Input.GetKeyDown(KeyCode.M))
		{
			int num2 = (int)(SkyBoxController.OrbitDrawMode + 1);
			if (num2 > 3)
			{
				num2 = 0;
			}
			SkyBoxController.OrbitDrawMode = (OrbitDrawMode)num2;
		}
		else if (Input.GetKeyDown(KeyCode.O))
		{
			int num3 = (int)(SkyBoxController.OrbitRenderSetting + 1);
			if (num3 > 1)
			{
				num3 = 0;
			}
			SkyBoxController.OrbitRenderSetting = (OrbitRenderSetting)num3;
		}
	}

	private void RescaleBodies()
	{
		foreach (CelestialPrefab prefab in _prefabs)
		{
			prefab.Rescale();
		}
	}

	public static void UpdateEachFrame()
	{
		if (System != null && IsValid)
		{
			System.UpdateAllBodies(Time.deltaTime);
			CameraController.SetCameraPosition();
			HandleUpdate();
			SetSunState(Time.unscaledDeltaTime);
		}
	}

	private static void HandleUpdate(bool publish = true)
	{
		if (System.SetTimeOfDay() && publish)
		{
			WorldManager.PublishDaysPast();
		}
		SolarIrradiance = System.CalculateSolarIrradiance();
		if ((object)WorldSun != null)
		{
			Vector3 position = CameraController.EffectiveCameraPosition + WorldSunOffset;
			WorldSunOffset = WorldSunVector * 190f;
			WorldSunTransform.position = position;
			WorldSunTransform.LookAt(CameraController.EffectiveCameraPosition);
		}
	}

	private static void SetSunState(float unscaledDeltaTime)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		Vector3 effectiveCameraPosition = CameraController.EffectiveCameraPosition;
		Vector3 vector = effectiveCameraPosition + WorldSunOffset;
		bool flag = Physics.RaycastNonAlloc(new Ray(effectiveCameraPosition, WorldSunVector), _raycastHits, vector.magnitude * 0.7f, CursorManager.GetPlanetarySunMask()) > 0;
		if (flag != _isEclipseRaycast)
		{
			_eclipseTimeout = 0f;
			_isEclipseRaycast = flag;
		}
		else
		{
			_eclipseTimeout += unscaledDeltaTime;
		}
		if (_eclipseTimeout > 0.5f)
		{
			if (_isEclipseRaycast)
			{
				EclipseRatio = Mathf.Min(1f, EclipseRatio + unscaledDeltaTime / 4f);
			}
			else
			{
				EclipseRatio = Mathf.Max(0f, EclipseRatio - unscaledDeltaTime / 4f);
			}
		}
		SetSolarIntensity();
	}

	private static void SetSolarIntensity()
	{
		float num = Mathf.Lerp(1f, 0f, EclipseRatio);
		bool flag = WeatherManager.CurrentEventAffects(InventoryManager.ParentPosition.y);
		float num2 = 1f;
		if (flag && WeatherManager.CurrentWeatherEvent.SolarRatio != null)
		{
			num2 = WeatherManager.CurrentWeatherEvent.SolarRatio;
			if (WeatherManager.CurrentWeatherEvent.DirectionalLight != null)
			{
				num2 = Mathf.Max(num2, WeatherManager.CurrentWeatherEvent.DirectionalLight.GetIntensity());
			}
		}
		if (WorldManager.HasGravityAtHeight(InventoryManager.ParentPosition.y))
		{
			SolarIntensity = Mathf.Clamp(OrbitalViewController.AdjustSunElevation(WorldSunVector.y) / 0.01f, 0f, WorldSetting.Current.MaxSunIntensity) * num * num2;
		}
		else
		{
			SolarIntensity = WorldSetting.Current.MaxSunIntensity * num * num2;
		}
		WorldSun.intensity = SolarIntensity;
		WorldSun.color = ((flag && WeatherManager.CurrentWeatherEvent.DirectionalLight != null) ? ((Color)WeatherManager.CurrentWeatherEvent.DirectionalLight.Color) : WorldManager.Instance.WorldSun.Color);
		if (CursorManager.DefaultEasyFlareEnabled && (bool)WorldSunFlare)
		{
			if (VoxelTerrain.TerrainIsBlockingSun)
			{
				WorldSunFlare.Opacity = 0f;
			}
			else
			{
				WorldSunFlare.Opacity = Mathf.Clamp01(SolarIntensity - WeatherManager.Instance.weatherFlareIntensityOffset);
			}
		}
	}

	private bool SetTimeOfDay()
	{
		bool num = PlayerBody.DayCount != WorldManager.DaysPast;
		if (num)
		{
			WorldManager.DaysPast = PlayerBody.DayCount;
		}
		_timeOfDay = GetTimeOfDay();
		return num;
	}

	private static float GetTimeOfDay()
	{
		return (float)((double)Mathf.Abs((float)(GetPlayerBody().AccumulatedAngle / 360.0)) % 1.0);
	}

	public static float GetWorldAtmosphereCurveTime()
	{
		float value = Vector3.Angle(Vector3.up, WorldSunVector);
		return RocketMath.MapToScale(0f, 180f, 0.25f, 0.75f, value);
	}

	public float CalculateSolarIrradiance()
	{
		return CalculateSolarIrradiance(GetPrimaryBody()?.DistanceToPlayer ?? double.PositiveInfinity);
	}

	private float CalculateSolarIrradiance(double distanceInAu)
	{
		return (float)((double)SolarConstant / (distanceInAu * distanceInAu));
	}

	private ValueRange CalculateSolarIrradiance(DistanceRange distanceInAu)
	{
		return new ValueRange((float)((double)SolarConstant / (distanceInAu.Maximum * distanceInAu.Maximum)), (float)((double)SolarConstant / (distanceInAu.Minimum * distanceInAu.Minimum)));
	}

	public static void Draw()
	{
		if ((object)WorldSunTransform == null || System?.PlayerBody == null)
		{
			return;
		}
		if (SPUCelestialScanner.ShowVisualization)
		{
			OrbitDrawMode orbitDrawMode = SkyBoxController.OrbitDrawMode;
			SkyBoxController.OrbitDrawMode = OrbitDrawMode.InWorld;
			System.PrimaryBody.DrawDebug(System.PlayerBody, System.PrimaryBody);
			SkyBoxController.OrbitDrawMode = orbitDrawMode;
			SPUCelestialScanner.ShowVisualization = false;
		}
		if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.None && !OrbitalDebugger)
		{
			return;
		}
		Celestial referenceBody = System.PrimaryBody;
		if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.LocalMap)
		{
			referenceBody = ((System.PlayerBody.OrbitingBody != System.PrimaryBody) ? System.PlayerBody.OrbitingBody : System.PlayerBody);
		}
		if (SkyBoxController.OrbitDrawMode != OrbitDrawMode.None)
		{
			System.PrimaryBody.DrawDebug(System.PlayerBody, referenceBody);
		}
		if (OrbitalDebugger)
		{
			if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.InWorld)
			{
				DrawScreenRotation(System.PlayerBody);
			}
			ImGui.Begin("debug", (ImGuiWindowFlags)799685);
			ImGui.SetWindowPos(new Vector2(10f, 10f), ImGuiCond.Always);
			ImguiHelper.DrawText($"IsEclipse: {IsEclipse}");
			ImguiHelper.DrawText($"IsRetrogradeRotation: {System.PlayerBody.IsRetrogradeRotation}");
			ImguiHelper.DrawText($"Latitude: {Latitude:F1}");
			ImguiHelper.DrawText($"Longitude: {Longitude:F1}");
			ImguiHelper.DrawText($"TimeOfDay: {TimeOfDay:F2}");
			ImguiHelper.DrawText($"AtmosphereTimeOfDay: {GetWorldAtmosphereCurveTime():F2}");
			ImguiHelper.DrawText($"Timescale: {System.TimeScale:F6} (standard: {CalculateTimeScale(System):F6})");
			ImguiHelper.DrawText($"DistanceFromSun: {GetPrimaryBody().DistanceToPlayer:F3} AU");
			ImguiHelper.DrawText($"SolarIrradiance: {SolarIrradiance:F3} W/m2");
			ImguiHelper.DrawText($"RotationSpeed: {GetPlayerBody()._baseRotationSpeed:F3}");
			ImguiHelper.DrawText($"SkyboxScale: {SkyboxScale:F4}");
			ImguiHelper.DrawText($"BodyScale: {BodyScale:F1}");
			ImguiHelper.DrawText($"SiderealDay: {System.PlayerBody.GetGameSiderealDayLength()}");
			ImguiHelper.DrawText($"SiderealYear: {System.PlayerBody.GetSiderealYearLength()}");
			ImguiHelper.DrawText($"NewTimeScale: {System.PlayerBody.UnscaledDeltaTime}");
			ImguiHelper.DrawText($"DaysPast: {WorldManager.DaysPast}");
			ImguiHelper.DrawText($"CurrentAngle: {System.PlayerBody.CurrentAngle:F1}°");
			ImguiHelper.DrawText($"SolarProgressionPerDay: {360.0 / System.PlayerBody.Orbit.Period:F1}°");
			ImguiHelper.DrawText($"AccumulatedAngle: {System.PlayerBody.AccumulatedAngle:F1}°");
			ImguiHelper.DrawText($"TrueAnomaly: {System.PlayerBody._trueAnomaly:F1}°");
			ImguiHelper.DrawText($"WrappedTrueAnomaly: {System.PlayerBody.GetWrappedTrueAnomaly():F1}°");
			ImguiHelper.DrawText($"TotalRealTimeSeconds: {TimeLength.FromSeconds(System.TotalRealTimeSeconds)}");
			ImguiHelper.DrawText($"SimulationTimeSeconds: {TimeLength.FromSeconds(System.SimulationTimeSeconds)}");
			ImguiHelper.DrawText("OffsetTime: " + TimeLength.FromSeconds(System._offsetTime).ToNearestString());
			ImguiHelper.DrawText($"AdjustdOffsetTime: {TimeLength.FromSeconds(System.GetAdjustedOffsetTime())}");
			ImguiHelper.DrawText($"DayAngle: {System.PlayerBody.DayAngle:F1}°");
			ImguiHelper.DrawText($"DayCount: {System.PlayerBody.DayCount}");
			ImGui.End();
		}
	}

	public static int GetDayLengthSeconds()
	{
		return System?.DayLengthSeconds ?? 1200;
	}

	private static void DrawScreenRotation(RotatingCelestialBody body)
	{
		Vector2? vector = null;
		Vector2? vector2 = null;
		Quaternion quaternion = Quaternion.AngleAxis(body.EclipticInclination, Vector3.forward);
		for (int i = 0; i <= 24; i++)
		{
			Quaternion quaternion2 = body.Rotation * Quaternion.AngleAxis((float)i * 15f, body.RotationAxis);
			Vector3 vector3 = body.GetScenePosition(body.Position + quaternion2 * (Vector3.forward * 190f)).normalized.ToVector3();
			vector3 = quaternion * vector3;
			if (Vector3.Dot(vector3, CameraController.Instance.MainCameraTransform.forward) < 0f)
			{
				vector2 = null;
				continue;
			}
			vector3 += CameraController.EffectiveCameraPosition;
			Vector2 screenPosition = GetScreenPosition(vector3, CameraController.CurrentCamera);
			if (vector2.HasValue)
			{
				ImGui.GetForegroundDrawList().AddLine(vector2.Value, screenPosition, ImGuiColor.Integer.Blue, 1f);
			}
			Vector2 valueOrDefault = vector.GetValueOrDefault();
			if (!vector.HasValue)
			{
				valueOrDefault = screenPosition;
				vector = valueOrDefault;
			}
			vector2 = screenPosition;
		}
		if (vector2.HasValue)
		{
			ImGui.GetForegroundDrawList().AddLine(vector2.Value, vector.Value, ImGuiColor.Integer.Blue, 1f);
		}
	}

	public static Vector2 GetScreenPosition(Vector3d localPosition, Camera camera)
	{
		return GetScreenPosition(localPosition.ToVector3(), camera);
	}

	public static Vector2 GetScreenPosition(Vector3 localPosition, Camera camera)
	{
		Vector3 vector = camera.WorldToScreenPoint(localPosition);
		if (vector.z < 0f)
		{
			return Vector2.negativeInfinity;
		}
		return new Vector2(vector.x, (float)Screen.height - vector.y);
	}

	private CelestialBody AddCelestialBody(Celestial parentBody, CelestialBodyReference reference, bool simulateOnly = false)
	{
		CelestialBodyTemplate celestialBodyTemplate = CelestialBodyTemplate.Find(reference.Id);
		if (celestialBodyTemplate == null)
		{
			return null;
		}
		CelestialBody celestialBody = new CelestialBody(this, celestialBodyTemplate)
		{
			OrbitingBody = parentBody
		};
		parentBody.Add(celestialBody);
		if (!reference.Hidden && !simulateOnly)
		{
			reference.Create(celestialBody, celestialBodyTemplate);
		}
		return celestialBody;
	}

	private CelestialBody AddRotatableBody(Celestial parentBody, CelestialBodyReference reference, bool simulateOnly = false)
	{
		CelestialBodyTemplate celestialBodyTemplate = CelestialBodyTemplate.Find(reference.Id);
		if (celestialBodyTemplate == null)
		{
			return null;
		}
		RotatingCelestialBody rotatingCelestialBody = new RotatingCelestialBody(this, celestialBodyTemplate, reference.Axis.ToVector3(), reference.Axis.GetSpeed(celestialBodyTemplate))
		{
			TidallyLocked = (reference.Axis is TidalLocking),
			OrbitingBody = parentBody
		};
		parentBody.Add(rotatingCelestialBody);
		if (!reference.Hidden && !simulateOnly)
		{
			reference.Create(rotatingCelestialBody, celestialBodyTemplate);
		}
		return rotatingCelestialBody;
	}

	public static void ClearAll()
	{
		CelestialPrefab.ReturnPools();
		_prefabs.Clear();
		System.Clear();
		EclipseRatio = 0f;
	}

	private void Clear()
	{
		PlayerBody = null;
		PrimaryBody.Clear();
		_bodies.Clear();
		_bodiesLookup.Clear();
	}

	public static void Register(CelestialPrefab celestialPrefab)
	{
		_prefabs.Add(celestialPrefab);
		celestialPrefab.Body = System.Find<Celestial>(celestialPrefab.CelestialId);
		if (celestialPrefab.Body == null)
		{
			ConsoleWindow.PrintError("warning: celestial body " + celestialPrefab.CelestialId + " not found for " + celestialPrefab.name);
		}
		celestialPrefab.Transform.SetParent(null);
	}

	public static void Deregister(CelestialPrefab celestialPrefab)
	{
		celestialPrefab.Body = null;
		_prefabs.Remove(celestialPrefab);
	}

	public static void AssignSun(Light sun)
	{
		WorldSun = sun;
		WorldSunFlare = WorldSun.GetComponent<EasyFlares>();
		WorldSunTransform = WorldSun.transform;
		if (!GameManager.IsBatchMode)
		{
			WorldSunOffset = WorldSunTransform.position;
		}
		WorldSunTransform.LookAt(Vector3.zero);
		WorldSunVector = Vector3.zero - WorldSunTransform.position;
		WorldSun.cullingMask = CursorManager.Instance.SunMask;
		AtmosphericScatteringSun component = WorldSun.GetComponent<AtmosphericScatteringSun>();
		if ((bool)component)
		{
			component.Register();
		}
		if ((bool)PlanetarySun)
		{
			UnityEngine.Object.Destroy(PlanetarySun.gameObject);
		}
		PlanetarySun = UnityEngine.Object.Instantiate(sun, WorldSun.transform);
		PlanetarySun.transform.SetParent(WorldSunTransform);
		PlanetarySun.transform.localPosition = Vector3.zero;
		PlanetarySun.transform.localRotation = Quaternion.identity;
		PlanetarySun.name = "~PlanetarySun";
		Transform[] componentsInChildren = PlanetarySun.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform != PlanetarySun.transform)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		UnityEngine.Object.Destroy(PlanetarySun.GetComponent<AtmosphericScatteringSun>());
		UnityEngine.Object.Destroy(PlanetarySun.GetComponent<EasyFlares>());
		PlanetarySun.cullingMask = CursorManager.Instance.PlanetarySunMask;
		PlanetarySun.intensity = 1f;
	}

	public static void InitializeSimulation()
	{
		WorldSunTransform = WorldSun.transform;
		WorldSunOffset = WorldSunTransform.position;
		WorldSunTransform.LookAt(Vector3.zero);
		WorldSunVector = Vector3.zero - WorldSunOffset;
	}

	public static void Load(WorldSetting worldSetting)
	{
		CelestialPrefab.ReturnPools();
		System.Clear();
		PlayableBodyReference data = worldSetting.PlayableBody ?? PlayableBodyReference.Default;
		CelestialConstantsReference celestialConstantsReference = worldSetting.CelestialConstants ?? CelestialConstantsReference.Default;
		BodyScale = celestialConstantsReference.BodyScale;
		SkyboxScale = celestialConstantsReference.SkyboxScale;
		string id = ((worldSetting.PrimaryBody != null) ? worldSetting.PrimaryBody.Id : "Sol");
		string name = ((worldSetting.PrimaryBody != null) ? ((string)worldSetting.PrimaryBody.Name) : "CelestialBodySol");
		System.PrimaryBody.Rename(id, name);
		SolarIrradiance = System.CalculateSolarIrradiance();
		if (worldSetting.PrimaryBody != null && !float.IsNaN(worldSetting.PrimaryBody.SolarConstant))
		{
			System.SolarConstant = worldSetting.PrimaryBody.SolarConstant;
		}
		else
		{
			System.SolarConstant = 1367f;
		}
		if (worldSetting.CelestialBodies != null)
		{
			System.Add(worldSetting.CelestialBodies, simulateOnly: false);
		}
		System.SetPlayable(data);
		System.Initialize();
		System.TimeScale = CalculateTimeScale(System);
		System._offsetTime = celestialConstantsReference.TimeOffset?.GetTotalSeconds() ?? 0.0;
		double allBodies = System.GetAdjustedOffsetTime() * System.TimeScale;
		System.SetAllBodies(allBodies);
		System.RescaleBodies();
	}

	private void Add(CelestialCollection collection, bool simulateOnly)
	{
		List<CelestialBodyReference> list = new List<CelestialBodyReference>(collection.Bodies);
		list.Sort(delegate(CelestialBodyReference a, CelestialBodyReference b)
		{
			if (string.IsNullOrEmpty(a.Parent) && !string.IsNullOrEmpty(b.Parent))
			{
				return -1;
			}
			if (!string.IsNullOrEmpty(a.Parent) && string.IsNullOrEmpty(b.Parent))
			{
				return 1;
			}
			if (!string.IsNullOrEmpty(a.Parent) && !string.IsNullOrEmpty(b.Parent))
			{
				if (a.Id == b.Parent)
				{
					return -1;
				}
				if (b.Id == a.Parent)
				{
					return 1;
				}
				return string.Compare(a.Id, b.Id);
			}
			return string.Compare(a.Id, b.Id);
		});
		foreach (CelestialBodyReference item in list)
		{
			Add(item, simulateOnly);
		}
	}

	private void Initialize()
	{
		Register(PrimaryBody);
		foreach (Celestial body in _bodies)
		{
			body.Initialize(PlayerBody);
		}
		PrimaryBody.Initialize(PlayerBody);
	}

	private CelestialBody Add(CelestialBodyReference data, bool simulateOnly = false)
	{
		Celestial parentBody = Find<Celestial>(data.Parent) ?? PrimaryBody;
		if (data.Axis != null)
		{
			return AddRotatableBody(parentBody, data, simulateOnly);
		}
		return AddCelestialBody(parentBody, data, simulateOnly);
	}

	private void SetPlayable(PlayableBodyReference data, bool simulateOnly = false)
	{
		if (data == null)
		{
			data = PlayableBodyReference.Default;
		}
		PlayerBody = Find<RotatingCelestialBody>(data.Id) ?? Add(data, simulateOnly);
		Latitude = data.GetLatitude();
		Longitude = data.GetLongitude();
		if (!simulateOnly)
		{
			SkyBoxController.SetPlanetaryAxis(PlayerBody.RotationAxis);
			PlayerBody?.Prefab?.SetupAsPlayerBody();
		}
	}

	private RotatingCelestialBody Add(PlayableBodyReference data, bool simulateOnly)
	{
		CelestialBodyReference data2 = new CelestialBodyReference
		{
			Id = data.Id,
			Axis = new FixedRotation
			{
				x = 0f,
				y = 1f,
				z = 0f,
				Speed = 15.0
			}
		};
		return Add(data2, simulateOnly) as RotatingCelestialBody;
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		if (System == null)
		{
			throw new NullReferenceException("cannot sync when system is null");
		}
		writer.WriteDouble(System.TimeScale);
		writer.WriteDouble(System.SimulationTimeSeconds);
	}

	public static void DeserializeOnJoin(RocketBinaryReader reader)
	{
		if (System == null)
		{
			throw new NullReferenceException("cannot sync when system is null");
		}
		System.TimeScale = reader.ReadDouble();
		double allBodies = reader.ReadDouble();
		System.SetAllBodies(allBodies);
		HandleUpdate();
	}

	public T Find<T>(string name) where T : Celestial
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		return Find<T>(Animator.StringToHash(name));
	}

	private bool Exists(int hash)
	{
		return _bodiesLookup.ContainsKey(hash);
	}

	public T Find<T>(int hash) where T : Celestial
	{
		Celestial value = null;
		if (hash == PrimaryBody.Hash)
		{
			value = PrimaryBody as T;
		}
		if (value == null)
		{
			_bodiesLookup.TryGetValue(hash, out value);
		}
		return (value as T) ?? null;
	}

	public void Register(Celestial celestial)
	{
		if (!Exists(celestial.Hash))
		{
			_bodies.Add(celestial);
			_bodiesLookup.Add(celestial.Hash, celestial);
		}
	}

	public static RotatingCelestialBody GetPlayerBody()
	{
		return System.PlayerBody;
	}

	public static Celestial GetPrimaryBody()
	{
		return System.PrimaryBody;
	}

	public Quaternion GetSceneRotation()
	{
		if (PlayerBody == null)
		{
			return Quaternion.identity;
		}
		RotatingCelestialBody playerBody = PlayerBody;
		Quaternion quaternion = Quaternion.AngleAxis(playerBody.CurrentAngle + Longitude, playerBody.RotationAxis);
		Quaternion quaternion2 = Quaternion.AngleAxis(-90f, Vector3.right);
		Quaternion quaternion3 = Quaternion.AngleAxis(Latitude, Vector3.right);
		return quaternion2 * quaternion3 * quaternion;
	}

	public static void OnGameStarted()
	{
		System.RescaleBodies();
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer, bool force = false)
	{
		if (System == null)
		{
			throw new NullReferenceException("cannot sync when system is null");
		}
		ushort networkUpdateFlags = NetworkUpdateFlags;
		networkUpdateFlags |= 2;
		if (force)
		{
			networkUpdateFlags |= 0xFFFF;
		}
		writer.WriteUInt32(networkUpdateFlags);
		if (Thing.IsNetworkUpdateRequired(networkUpdateFlags, 1))
		{
			writer.WriteDouble(System.TimeScale);
		}
		if (Thing.IsNetworkUpdateRequired(networkUpdateFlags, 2))
		{
			writer.WriteDouble(System.TotalRealTimeSeconds);
			writer.WriteDouble(System.SimulationTimeSeconds);
		}
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		if (System == null)
		{
			throw new NullReferenceException("cannot sync when system is null");
		}
		uint toCheck = reader.ReadUInt32();
		if (Thing.IsNetworkUpdateRequired(toCheck, 1))
		{
			System.TimeScale = reader.ReadDouble();
		}
		if (Thing.IsNetworkUpdateRequired(toCheck, 2))
		{
			System.TotalRealTimeSeconds = reader.ReadDouble();
			double allBodies = reader.ReadDouble();
			System.SetAllBodies(allBodies);
			HandleUpdate();
		}
	}

	public static void SerializeSave(XmlSaveLoad.WorldData worldData)
	{
		worldData.CelestialData = new OrbitSimulationSaveData
		{
			SimulationTime = new DoubleReference
			{
				Value = System.SimulationTimeSeconds
			},
			AccumulatedTime = new DoubleReference
			{
				Value = System.TotalRealTimeSeconds
			}
		};
	}

	public static void DeserializeSave(XmlSaveLoad.WorldData worldData)
	{
		if (worldData.CelestialData == null)
		{
			SetRealTime((double)worldData.DaysPast * System.PlayerBody.GetGameSiderealDayLength().ToTotalSeconds());
		}
		else
		{
			worldData.CelestialData?.Deserialize(System);
		}
	}

	public static CelestialReference GetParentBody(WorldSetting worldSetting)
	{
		CelestialBodyReference celestialBodyReference = worldSetting.CelestialBodies?.Find(worldSetting.PlayableBody.Id);
		if (celestialBodyReference == null)
		{
			return worldSetting.PrimaryBody;
		}
		CelestialBodyReference celestialBodyReference2 = worldSetting.CelestialBodies.Find(celestialBodyReference.Parent);
		if (celestialBodyReference2 != null)
		{
			return celestialBodyReference2;
		}
		return worldSetting.PrimaryBody;
	}

	public static OrbitalSimulation GetSimulation(WorldSetting worldSetting, int dayLengthSeconds)
	{
		string primaryBodyId = worldSetting.PrimaryBody?.Id ?? "Sol";
		LocalizedStringReference localizedStringReference = worldSetting.PrimaryBody?.Name;
		string primaryBodyName = ((localizedStringReference != null) ? ((string)localizedStringReference) : (worldSetting.PrimaryBody?.Id ?? Localization.GetInterface("CelestialBodySol")));
		OrbitalSimulation orbitalSimulation = new OrbitalSimulation(primaryBodyId, primaryBodyName);
		if (worldSetting.CelestialBodies != null)
		{
			orbitalSimulation.Add(worldSetting.CelestialBodies, simulateOnly: true);
		}
		orbitalSimulation.DayLengthSeconds = dayLengthSeconds;
		orbitalSimulation.SetPlayable(worldSetting.PlayableBody, simulateOnly: true);
		orbitalSimulation.SolarConstant = worldSetting.PrimaryBody?.SolarConstant ?? 1367f;
		orbitalSimulation.Initialize();
		orbitalSimulation.TimeScale = CalculateTimeScale(orbitalSimulation);
		return orbitalSimulation;
	}

	public ValueRange GetSolarEnergy()
	{
		return CalculateSolarIrradiance(PrimaryBody.RangeToPlayer);
	}

	public float GetSolarEnergyPercentClamped(ValueRange solarRange, float solarIrradiance)
	{
		return RocketMath.MapToScaleClamp(solarRange.Minimum, solarRange.Maximum, 0f, 100f, solarIrradiance);
	}

	public float GetSolarAngle(WorldSetting worldSetting)
	{
		if (worldSetting.PlayableBody == null)
		{
			return 0f;
		}
		float f = Mathf.Acos(Vector3.Dot(PlayerBody.RotationAxis, Vector3.up));
		PlayableBodyReference playableBody = worldSetting.PlayableBody;
		float f2 = ((playableBody != null) ? (playableBody.Latitude * (MathF.PI / 180f)) : 0f);
		return Mathf.Asin(Mathf.Sin(f2) * Mathf.Sin(f) + Mathf.Cos(f2) * Mathf.Cos(f)) * 57.29578f;
	}

	public static void SetTimeScale(float scale)
	{
		if (System != null)
		{
			System.TimeScale = scale;
			ConsoleWindow.PrintAction($"orbital simulation timescale set to {scale}");
		}
	}

	public static void PrintDebug()
	{
		if (System == null)
		{
			ConsoleWindow.PrintError("error no orbital simulation running", suppressStacktrace: true);
			return;
		}
		ConsoleWindow.PrintAction("orbital simulation for '" + (WorldSetting.Current?.Id ?? "???") + "'");
		TreeString treeString = TreeString.Node("Primary Body: " + System.PrimaryBody?.Name);
		TreeString.Variable($"Solar Constant: {System.SolarConstant} W/m2", treeString);
		TreeString.Variable($"Solar Irradiance: {SolarIrradiance} W/m2", treeString);
		TreeString.Variable($"Solar Energy: {System.GetSolarEnergy()}", treeString);
		TreeString.Variable($"Total Real Time: {System.TotalRealTimeSeconds:F0} seconds", treeString);
		TreeString.Variable($"Total Simulation Time: {System.SimulationTimeSeconds:F0} seconds", treeString);
		TreeString.Variable($"Offset Time: {System._offsetTime:F0} seconds", treeString);
		TreeString.Variable($"DaysPast: {WorldManager.DaysPast}", treeString);
		TreeString.Node($"Timescale: {System.TimeScale}", treeString);
		float num = Mathf.Atan2(WorldSunVector.x, WorldSunVector.z) * 57.29578f;
		float num2 = Mathf.Asin(WorldSunVector.y) * 57.29578f;
		TreeString.Node($"Sun Vector Horizontal: {num:F1}° Vertical: {num2:F1}°", treeString);
		if (System.PlayerBody != null)
		{
			TreeString myParent = TreeString.Node("Player Body: " + System.PlayerBody.Name, treeString);
			TreeString.Variable($"Latitude: {Latitude:F1}°", myParent);
			TreeString.Variable($"Longitude: {Longitude:F1}°", myParent);
			TreeString.Variable($"TimeOfDay: {GetTimeOfDay()}", myParent);
			TreeString.Variable($"RotationSpeed: {GetPlayerBody()._baseRotationSpeed:F3}°", myParent);
			TreeString.Variable("SiderealDay: " + System.PlayerBody.GetGameSiderealDayLength().ToNearestString(), myParent);
			TreeString.Variable("SiderealYear: " + System.PlayerBody.GetSiderealYearLength().ToNearestString(), myParent);
			TreeString.Variable($"CurrentRotation: {System.PlayerBody.CurrentAngle}°", myParent);
		}
		treeString.ToConsole();
	}

	public static void PrintList()
	{
		if (System == null)
		{
			ConsoleWindow.PrintError("error no orbital simulation running", suppressStacktrace: true);
			return;
		}
		TreeString treeString = TreeString.Node("celestial database for '" + (WorldSetting.Current?.Id ?? "???") + "'");
		foreach (Celestial body in System._bodies)
		{
			TreeString.Variable(body.Name + " (" + body.GetType().Name + ")", treeString);
		}
		treeString.ToConsole();
	}

	public static int GetCelestialCount()
	{
		if (System == null)
		{
			throw new IndexOutOfRangeException("no celestials exist");
		}
		return System._bodies.Count;
	}

	public static Celestial GetCelestial(int index)
	{
		if (System == null)
		{
			throw new IndexOutOfRangeException("no celestials exist");
		}
		if (index < 0 || index >= System._bodies.Count)
		{
			throw new IndexOutOfRangeException($"index '{index}' out of range");
		}
		return System._bodies[index];
	}

	public static CelestialHit RaycastSky(Vector3 dishForward, float sensitivity)
	{
		if (System == null)
		{
			throw new IndexOutOfRangeException("no celestials exist");
		}
		dishForward = -dishForward.normalized;
		CelestialHit result = new CelestialHit(null, float.MaxValue);
		foreach (Celestial body in System._bodies)
		{
			if (body != System.PlayerBody)
			{
				float num = Vector3.Angle(body.WorldVector, -dishForward);
				if (num <= sensitivity && num < result.Angle)
				{
					result = new CelestialHit(body, num);
				}
			}
		}
		return result;
	}

	public static double MakeOffset()
	{
		if (System == null)
		{
			throw new NullReferenceException("cannot offset time when system is null");
		}
		return System.TotalRealTimeSeconds + System._offsetTime;
	}
}
