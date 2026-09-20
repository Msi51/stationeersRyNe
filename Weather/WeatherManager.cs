using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Assets.Scripts.Weather;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using StormVolumes;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Weather;

public class WeatherManager : ManagerBase
{
	[FormerlySerializedAs("WeatherEvents")]
	public List<WeatherEventParticlePreset> WeatherParticleEffects = new List<WeatherEventParticlePreset>();

	public static WeatherManager Instance;

	public static RocketRandom MainRandom;

	public static RocketRandom DamagingRandom;

	[SerializeField]
	private StormCardMeshController stormCardMeshController;

	[SerializeField]
	private StormPostEffect stormPostEffect;

	private static bool _isWeatherEventRunning;

	public static int DaysSinceLastWeatherEvent;

	public static float WeatherStartTime;

	public static float WeatherEventLength;

	public static int LastEventCoolDown;

	public static Vector3 StormDirectionVector = Vector3.forward;

	public static readonly int RainWeatherEvent = Animator.StringToHash("Rain");

	public static readonly int SnowWeatherEvent = Animator.StringToHash("Snow");

	public const float STORM_MAX_HEIGHT = 500f;

	public static bool RegenStormCurtain;

	private static float _offset = 1f;

	private const int CAPACITY = 65536;

	private static readonly List<Vector3> Verts = new List<Vector3>(65536);

	private static readonly List<int> Tris = new List<int>(65536);

	private static readonly List<Vector2> Uvs = new List<Vector2>(98304);

	private static readonly Action<Atmosphere> StormCurtainAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere.Room != null || atmosphere.HasPartialFrame)
		{
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Left) == Atmosphere.OutsideFaceFlags.Left)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.West.ToVector3() * _offset, Vector3.left);
			}
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Right) == Atmosphere.OutsideFaceFlags.Right)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.East.ToVector3() * _offset, Vector3.right);
			}
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Up) == Atmosphere.OutsideFaceFlags.Up)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.Up.ToVector3() * _offset, Vector3.up);
			}
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Down) == Atmosphere.OutsideFaceFlags.Down)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.Down.ToVector3() * _offset, Vector3.down);
			}
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Front) == Atmosphere.OutsideFaceFlags.Front)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.North.ToVector3() * _offset, Vector3.forward);
			}
			if ((atmosphere.FaceFlags & Atmosphere.OutsideFaceFlags.Back) == Atmosphere.OutsideFaceFlags.Back)
			{
				AddQuad(atmosphere.WorldPosition + Grid3.Face.South.ToVector3() * _offset, Vector3.back);
			}
		}
	};

	private readonly int _defaultCooldown = 3;

	public const int FIRST_STORM_DELAY = 7;

	private static readonly Action<DynamicThing> CheckWeatherDamageAction = delegate(DynamicThing thing)
	{
		if ((object)thing != null && ((IWeatherDamagable)thing).CanBeWeathered() && CurrentEventAffects(thing.RootParent.Position.y))
		{
			((IWeatherDamagable)thing).DoWeatherDamage((float)CurrentWeatherEvent.WeatherDamageMultiplier);
		}
	};

	private static float _defaultFlareOpacity;

	private static float _defaultSunIntensity;

	private static readonly float _intensityAdjustSpeed = 2f;

	private static readonly float _maxSunIntensity = 0.5f;

	[Tooltip("How rapidly the flare flickers when a storm is running")]
	public float flareFlickerFrequency = 10f;

	[Tooltip("How much the flare flickers by when a storm is running")]
	public float flareFlickerIntensity = 1f;

	[Tooltip("Base value from which the flare will flicker. Higher values makes the flare darker.")]
	public float flareFlickerNoiseOffset = 0.5f;

	private readonly OpenSimplexNoise _sunFlickerSimplexNoise = new OpenSimplexNoise();

	public float weatherFlareIntensityOffset;

	private static CancellationTokenWrapper _stormEffectCancellation = new CancellationTokenWrapper();

	public const float FOG_DISTANCE_INDOOR_OFFSET = 15f;

	private static Action StormEffectsFinishedFading = delegate
	{
		Instance.stormCardMeshController.EnableEffects(isEnabled: false);
		Instance.stormPostEffect.enabled = false;
	};

	public static bool DrawStormDebug;

	public static bool IsWeatherEventRunning
	{
		get
		{
			return _isWeatherEventRunning;
		}
		private set
		{
			if (value != _isWeatherEventRunning)
			{
				if (value)
				{
					EnableEffects();
				}
				else
				{
					DisableEffects();
				}
			}
			_isWeatherEventRunning = value;
		}
	}

	public static WeatherEvent CurrentWeatherEvent { get; private set; }

	public static bool IsWeatherEventScheduled => CurrentWeatherEvent != null;

	public static bool WorldHasWeather => WorldSetting.Current.WeatherEvents.Count > 0;

	public static WeatherState WeatherState { get; private set; }

	public static bool IsAboveStormHeight(float height)
	{
		return height > 500f;
	}

	public static bool CurrentEventAffects(float height)
	{
		if (CurrentWeatherEvent == null || !IsWeatherEventRunning)
		{
			return false;
		}
		if (CurrentWeatherEvent.ActiveInOrbit)
		{
			return true;
		}
		return !IsAboveStormHeight(height);
	}

	public static float GetSolarRatioAt(float height)
	{
		if (CurrentWeatherEvent?.SolarRatio == null || !IsWeatherEventRunning)
		{
			return 1f;
		}
		if (!CurrentEventAffects(height))
		{
			return 1f;
		}
		return CurrentWeatherEvent.SolarRatio;
	}

	public static WeatherEvent GetNextWeatherEvent()
	{
		if (WorldSetting.Current == null || WorldSetting.Current.WeatherEvents.Count == 0)
		{
			return null;
		}
		return WorldSetting.Current.WeatherEvents.Pick();
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (!Instance)
		{
			Instance = this;
			foreach (WeatherEventParticlePreset weatherParticleEffect in WeatherParticleEffects)
			{
				weatherParticleEffect.Initialize();
			}
			InitRandoms();
		}
		else
		{
			ConsoleWindow.PrintError("Script WeatherManager: already exists within the scene");
		}
	}

	private void OnDestroy()
	{
	}

	private static void SetCurrentWeatherState()
	{
		if (CurrentWeatherEvent?.IdHash == RainWeatherEvent)
		{
			WeatherState = (IsWeatherEventRunning ? WeatherState.Rain : WeatherState.RainScheduled);
		}
		else if (CurrentWeatherEvent?.IdHash == SnowWeatherEvent)
		{
			WeatherState = (IsWeatherEventRunning ? WeatherState.Snow : WeatherState.SnowScheduled);
		}
		else if (CurrentWeatherEvent != null)
		{
			WeatherState = ((!IsWeatherEventRunning) ? WeatherState.StormScheduled : WeatherState.Storm);
		}
		else
		{
			WeatherState = WeatherState.None;
		}
	}

	public static void ScheduleWeatherEvent(WeatherEvent weatherEvent)
	{
		CurrentWeatherEvent = weatherEvent;
		WeatherStartTime = CurrentWeatherEvent.GetRandomWeatherStartTime(MainRandom.random);
		WeatherEventLength = CurrentWeatherEvent.GetRandomWeatherDuration(MainRandom.random);
		WeatherStation.SetWeatherScheduled(scheduled: true);
		SetCurrentWeatherState();
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.GameState == GameState.Running)
		{
			if (GameManager.RunSimulation)
			{
				if (CanScheduleWeatherEvent())
				{
					ScheduleWeatherEvent(GetNextWeatherEvent());
				}
				if (IsWeatherEventScheduled && CanWeatherEventBecomeActive() && !IsWeatherEventRunning)
				{
					StartWeatherEventServer();
				}
			}
			bool flag = InventoryManager.ParentHuman?.RootParent == null || IsAboveStormHeight(InventoryManager.ParentHuman.RootParent.Position.y);
			if (!GameManager.IsBatchMode && CurrentWeatherEvent != null && IsWeatherEventRunning)
			{
				if (CurrentWeatherEvent.Fog != null)
				{
					Color.RGBToHSV(CurrentWeatherEvent.Fog.FogColor, out var H, out var S, out var V);
					V = Mathf.Lerp(0f, V, OrbitalSimulation.SolarIntensity);
					RenderSettings.fogColor = Color.HSVToRGB(H, S, V);
				}
				weatherFlareIntensityOffset = flareFlickerNoiseOffset + _sunFlickerSimplexNoise.Evaluate(0f, Time.time * flareFlickerFrequency) * flareFlickerIntensity;
				if ((CurrentWeatherEvent.ActiveInOrbit || !flag) && CurrentWeatherEvent.StormEffect != null)
				{
					if (RegenStormCurtain)
					{
						RegenStormCurtain = false;
						RegenerateStormCurtain();
					}
					stormCardMeshController.EnableEffects(isEnabled: true);
					Atmosphere atmosphere = AtmosphericsManager.Find(new WorldGrid(CameraController.CameraPosition));
					stormPostEffect.enabled = atmosphere?.Room == null;
				}
				else
				{
					stormCardMeshController.EnableEffects(isEnabled: false);
					stormPostEffect.enabled = false;
				}
			}
			if (IsWeatherEventRunning && HasStormTimeElapsed())
			{
				if (!flag && !WorldManager.IsCreative() && CurrentWeatherEvent?.Id == "VulcanAshStorm")
				{
					Achievements.AchieveSootHappens();
				}
				StopCurrentWeatherEvent();
			}
		}
		weatherFlareIntensityOffset = 0f;
	}

	public void RegenerateStormCurtain()
	{
		Verts.Clear();
		Tris.Clear();
		Uvs.Clear();
		AtmosphericsManager.AllAtmospheres.ForEach(StormCurtainAction);
		Mesh mesh = new Mesh();
		mesh.SetVertices(Verts);
		mesh.SetTriangles(Tris, 0);
		mesh.SetUVs(0, Uvs);
		mesh.RecalculateNormals();
		stormCardMeshController.SetMesh(mesh);
	}

	private static void AddQuad(Vector3 center, Vector3 normal)
	{
		Vector3 vector = Vector3.Cross(normal, Vector3.up);
		if (vector.sqrMagnitude < 0.001f)
		{
			vector = Vector3.Cross(normal, Vector3.right);
		}
		vector.Normalize();
		Vector3 normalized = Vector3.Cross(vector, normal).normalized;
		float num = 2f * 0.5f;
		Vector3 item = center - vector * num - normalized * num;
		Vector3 item2 = center + vector * num - normalized * num;
		Vector3 item3 = center + vector * num + normalized * num;
		Vector3 item4 = center - vector * num + normalized * num;
		int count = Verts.Count;
		Verts.Add(item);
		Verts.Add(item2);
		Verts.Add(item3);
		Verts.Add(item4);
		Uvs.Add(new Vector2(0f, 0f));
		Uvs.Add(new Vector2(1f, 0f));
		Uvs.Add(new Vector2(1f, 1f));
		Uvs.Add(new Vector2(0f, 1f));
		Tris.Add(count);
		Tris.Add(count + 1);
		Tris.Add(count + 2);
		Tris.Add(count);
		Tris.Add(count + 2);
		Tris.Add(count + 3);
	}

	public void StartWeatherEventServer()
	{
		if (GameManager.RunSimulation)
		{
			StormDirectionVector = GetStormDirection();
			IsWeatherEventRunning = true;
			WeatherStation.SetWeatherScheduled(scheduled: false);
			WeatherStation.SetWeatherActive(active: true);
			SetCurrentWeatherState();
		}
	}

	public void StopWeatherEventServer()
	{
		if (GameManager.RunSimulation)
		{
			LastEventCoolDown = CurrentWeatherEvent.GetCoolDown(MainRandom.random);
			DaysSinceLastWeatherEvent = 0;
			WeatherStartTime = 0f;
			WeatherEventLength = 0f;
			IsWeatherEventRunning = false;
			WeatherStation.SetWeatherActive(active: false);
			CurrentWeatherEvent = null;
			SetCurrentWeatherState();
		}
	}

	public static void ImmediatelyActivateWeatherEvent(WeatherEvent weatherEvent)
	{
		CurrentWeatherEvent = weatherEvent;
		WeatherStartTime = GameManager.GameTime;
		WeatherEventLength = CurrentWeatherEvent.GetRandomWeatherDuration(MainRandom.random);
		Instance.StartWeatherEventServer();
	}

	public static void ImmediatelyActivateWeatherEvent(string stormId = null)
	{
		WeatherEvent weatherEvent;
		if (!string.IsNullOrEmpty(stormId))
		{
			weatherEvent = DataCollection.Get<WeatherEvent>(stormId);
			if (weatherEvent == null)
			{
				weatherEvent = GetNextWeatherEvent();
			}
		}
		else
		{
			weatherEvent = GetNextWeatherEvent();
		}
		ImmediatelyActivateWeatherEvent(weatherEvent);
	}

	public static void StopCurrentWeatherEvent()
	{
		Instance.StopWeatherEventServer();
	}

	public static WeatherEventParticlePreset Find(string id)
	{
		return Find(Animator.StringToHash(id));
	}

	public static WeatherEventParticlePreset Find(int hash)
	{
		foreach (WeatherEventParticlePreset weatherParticleEffect in Instance.WeatherParticleEffects)
		{
			if (weatherParticleEffect.Hash == hash)
			{
				return weatherParticleEffect;
			}
		}
		return null;
	}

	public static void OnNextDay()
	{
		DaysSinceLastWeatherEvent++;
	}

	public static bool CanScheduleWeatherEvent()
	{
		if (WorldHasWeather && !IsWeatherEventScheduled && !IsWeatherEventRunning)
		{
			return HasCooldownPastInDays();
		}
		return false;
	}

	public static bool HasStormTimeElapsed()
	{
		return GameManager.GameTime > WeatherStartTime + WeatherEventLength;
	}

	public static bool CanWeatherEventBecomeActive()
	{
		return GameManager.GameTime >= WeatherStartTime;
	}

	public static float GetSecondsWhenNextWeatherEventIsActive()
	{
		if (!IsWeatherEventScheduled)
		{
			return 0f;
		}
		return WeatherStartTime - GameManager.GameTime;
	}

	private static bool HasCooldownPastInDays()
	{
		if (HasWorldStartCooldownPastInDays())
		{
			return DaysSinceLastWeatherEvent > LastEventCoolDown;
		}
		return false;
	}

	private static bool HasWorldStartCooldownPastInDays()
	{
		return (float)WorldManager.DaysPast > 7f * (float)DifficultySetting.Current.StartingWeatherMultiplier;
	}

	private void InitRandoms()
	{
		MainRandom = new RocketRandom(WorldManager.Seed);
		DamagingRandom = new RocketRandom(MainRandom.random.Next());
	}

	public static void ClearAll()
	{
		if (IsWeatherEventRunning)
		{
			ToggleStars(toggle: true);
		}
		_stormEffectCancellation.Cancel();
		IsWeatherEventRunning = false;
		CurrentWeatherEvent = null;
		DaysSinceLastWeatherEvent = 0;
		WeatherStartTime = 0f;
		WeatherEventLength = 0f;
		LastEventCoolDown = 0;
		if ((bool)Instance)
		{
			Instance.stormCardMeshController.ClearMesh();
			Instance.stormPostEffect.enabled = false;
			Instance.stormCardMeshController.EnableEffects(isEnabled: false);
		}
	}

	public static void DamageDynamicItems()
	{
		if (IsWeatherEventRunning && CurrentWeatherEvent?.WeatherDamageMultiplier != null && !((float)CurrentWeatherEvent.WeatherDamageMultiplier <= 0f))
		{
			OcclusionManager.AllDynamicThings.ForEach(CheckWeatherDamageAction);
		}
	}

	private static void ToggleStars(bool toggle)
	{
		if (GameManager.IsBatchMode || (object)SkyBoxController.Instance == null)
		{
			return;
		}
		foreach (StarfieldSkybox starfield in SkyBoxController.Instance.Starfields)
		{
			starfield.SetVisible(toggle);
		}
	}

	public static Vector3 GetStormDirection()
	{
		float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
		float x = Mathf.Cos(f);
		float z = Mathf.Sin(f);
		return new Vector3(x, 0f, z).normalized;
	}

	private static void EnableEffects()
	{
		if (!GameManager.IsBatchMode)
		{
			UpdateFogDistance();
			Instance.stormCardMeshController.ApplySetting(CurrentWeatherEvent, StormDirectionVector);
			Instance.stormPostEffect.ApplySetting(CurrentWeatherEvent, StormDirectionVector);
			if (CurrentWeatherEvent.StormEffect != null)
			{
				_stormEffectCancellation.CancelAndInitialize();
				FadeStormEffect(0f, CurrentWeatherEvent.StormEffect.RayMarchData.DensityMultiplier, 0f, CurrentWeatherEvent.StormEffect.EmissiveMult, _stormEffectCancellation.Token).Forget();
			}
			if (CurrentWeatherEvent.DirectionalLight != null)
			{
				CameraController.Instance.IsSolarStormEffectActive = true;
			}
		}
	}

	private static async UniTaskVoid FadeStormEffect(float fromDensity, float toDensity, float fromEmissive, float toEmissive, CancellationToken cancellationToken, Action finished = null)
	{
		float t = 0f;
		for (float duration = 10f; t < duration; t += Time.deltaTime)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			float stormDensityMult = Mathf.Lerp(fromDensity, toDensity, t / duration);
			float stormEmissiveMult = Mathf.Lerp(fromEmissive, toEmissive, t / duration);
			SetStormDensityMult(stormDensityMult);
			SetStormEmissiveMult(stormEmissiveMult);
			await UniTask.WaitForEndOfFrame();
		}
		SetStormDensityMult(toDensity);
		SetStormEmissiveMult(toEmissive);
		finished?.Invoke();
	}

	private static void SetStormDensityMult(float value)
	{
		Instance.stormCardMeshController.SetDensityMult(value);
		Instance.stormPostEffect.SetDensityMult(value);
	}

	private static void SetStormEmissiveMult(float value)
	{
		Instance.stormCardMeshController.SetEmissiveMult(value);
		Instance.stormPostEffect.SetEmissiveMult(value);
	}

	public static void UpdateFogDistance()
	{
		if (CurrentWeatherEvent.Fog != null && (bool)InventoryManager.Parent)
		{
			RenderSettings.fogStartDistance = ((InventoryManager.Parent.Room != null) ? (15f + CurrentWeatherEvent.Fog.StartDistance) : CurrentWeatherEvent.Fog.StartDistance);
			RenderSettings.fogEndDistance = ((InventoryManager.Parent.Room != null) ? (15f + CurrentWeatherEvent.Fog.EndDistance) : CurrentWeatherEvent.Fog.EndDistance);
		}
	}

	private static void DisableEffects()
	{
		if (!GameManager.IsBatchMode)
		{
			_stormEffectCancellation.CancelAndInitialize();
			if (CurrentWeatherEvent != null && IsWeatherEventRunning && CurrentWeatherEvent.StormEffect != null)
			{
				FadeStormEffect(CurrentWeatherEvent.StormEffect.RayMarchData.DensityMultiplier, 0f, CurrentWeatherEvent.StormEffect.EmissiveMult, 0f, _stormEffectCancellation.Token, StormEffectsFinishedFading).Forget();
			}
			else
			{
				Instance.stormCardMeshController.EnableEffects(isEnabled: false);
				Instance.stormPostEffect.enabled = false;
			}
			CameraController.Instance.IsSolarStormEffectActive = false;
		}
	}

	public WeatherManagerSavedData CreateSaveData()
	{
		if (!WorldHasWeather)
		{
			return null;
		}
		return new WeatherManagerSavedData
		{
			CurrentWeatherEventId = (CurrentWeatherEvent?.Id ?? string.Empty),
			DaysSinceLastWeatherEvent = DaysSinceLastWeatherEvent,
			IsWeatherEventRunning = IsWeatherEventRunning,
			IsWeatherEventScheduled = IsWeatherEventScheduled,
			WeatherEventLength = WeatherEventLength,
			WeatherStartTimeOffset = WeatherStartTime - GameManager.GameTime,
			LastEventCoolDown = LastEventCoolDown
		};
	}

	public void LoadSaveData(WeatherManagerSavedData savedData)
	{
		if (savedData != null)
		{
			CurrentWeatherEvent = ((!string.IsNullOrEmpty(savedData.CurrentWeatherEventId)) ? DataCollection.Get<WeatherEvent>(savedData.CurrentWeatherEventId) : null);
			DaysSinceLastWeatherEvent = savedData.DaysSinceLastWeatherEvent;
			WeatherEventLength = savedData.WeatherEventLength;
			LastEventCoolDown = savedData.LastEventCoolDown;
			WeatherStartTime = savedData.WeatherStartTimeOffset + GameManager.GameTime;
			if (savedData.IsWeatherEventRunning && CurrentWeatherEvent != null)
			{
				StartWeatherEventServer();
			}
			if (CurrentWeatherEvent == null)
			{
				IsWeatherEventRunning = false;
				WeatherEventLength = 0f;
				WeatherStartTime = 0f;
			}
		}
	}

	public static void DeserialiseDeltaState(RocketBinaryReader reader)
	{
		bool num = reader.ReadBoolean();
		int num2 = reader.ReadInt32();
		StormDirectionVector = reader.ReadVector3Half();
		WeatherEvent currentWeatherEvent = null;
		if (num2 != 0)
		{
			currentWeatherEvent = DataCollection.Get<WeatherEvent>(num2);
		}
		if (num)
		{
			CurrentWeatherEvent = currentWeatherEvent;
			IsWeatherEventRunning = true;
		}
		else
		{
			IsWeatherEventRunning = false;
			CurrentWeatherEvent = currentWeatherEvent;
		}
	}

	public static void SerialiseDeltaState(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(IsWeatherEventRunning);
		writer.WriteInt32(CurrentWeatherEvent?.IdHash ?? 0);
		writer.WriteVector3Half(StormDirectionVector);
	}

	public static void DrawDebug()
	{
		if (!DrawStormDebug)
		{
			return;
		}
		ImGui.Begin("WeatherInfo", (ImGuiWindowFlags)12687);
		Vector2 windowSize = new Vector2(500f, 800f);
		ImGui.SetWindowPos(new Vector2((float)Screen.width - windowSize.x, 0f), ImGuiCond.Always);
		ImGui.Columns(1);
		ImGui.NewLine();
		if (CurrentWeatherEvent == null)
		{
			ImGui.Text("No Weather Scheduled");
			ImGui.NewLine();
			if (!HasWorldStartCooldownPastInDays())
			{
				ImGui.Text("New World Cooldown active. " + StringManager.Get(7 - WorldManager.DaysPast) + " days remaining");
			}
			else if (!HasCooldownPastInDays())
			{
				ImGui.Text("Weather Cooldown. " + StringManager.Get(LastEventCoolDown - DaysSinceLastWeatherEvent) + " days remaining");
			}
		}
		else if (IsWeatherEventRunning)
		{
			ImGui.Text($"{CurrentWeatherEvent.Name} Is Active");
			ImGui.NewLine();
			ImGui.Text($"Direction: {StormDirectionVector}");
			ImGui.NewLine();
			ImGui.Text("Time Remaining: " + StringManager.Get((int)(WeatherEventLength + WeatherStartTime - GameManager.GameTime)) + "s");
			ImGui.NewLine();
			ImGui.Text("Game Time " + StringManager.Get((int)GameManager.GameTime) + "s");
			ImGui.NewLine();
			ImGui.Text("Start Time: " + StringManager.Get((int)WeatherStartTime) + "s");
			ImGui.NewLine();
			ImGui.Text("Length: " + StringManager.Get((int)WeatherEventLength) + "s");
		}
		else if (IsWeatherEventScheduled)
		{
			ImGui.Text($"{CurrentWeatherEvent.Name} Is Scheduled");
			ImGui.NewLine();
			ImGui.Text("Time until event: " + StringManager.Get((int)(WeatherStartTime - GameManager.GameTime)) + "s");
		}
		ImGui.SetWindowSize(windowSize);
		ImGui.End();
	}
}
