using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Objects.Rockets;
using UnityEngine;
using Weather;

namespace Sound;

public class EnvironmentalAudioHandler : ManagerBase
{
	[Serializable]
	public class ShellData
	{
		public int MaxCount;

		public float CoolDownTime;

		public float CullDistance;

		public List<EmitterDatum> EmitterData = new List<EmitterDatum>();

		public float VolumeMultiplier = 1f;

		public bool PlayShell = true;
	}

	public struct EmitterDatum(Vector3 position, float coolDownTime)
	{
		public Vector3 Position = position;

		public float CoolDownTime = coolDownTime;
	}

	public static EnvironmentalAudioHandler Instance;

	public AudioReverbZone PlayerReverbZone;

	private Room _currentRoom;

	private bool _hallway;

	private bool _underTerrain;

	private bool _box;

	private bool _hasAtmosphere;

	private bool _enclosed;

	public ReverbSetting[] ReverbSettings;

	public readonly Dictionary<int, ReverbSetting> SettingLookup = new Dictionary<int, ReverbSetting>();

	private readonly ReverbSetting _currentSetting = new ReverbSetting();

	private readonly ReverbSetting _targetSetting = new ReverbSetting();

	public static readonly int RoomHugeHash = Animator.StringToHash("RoomHuge");

	public static readonly int RoomSmallHash = Animator.StringToHash("RoomSmall");

	public static readonly int HallWaySmallHash = Animator.StringToHash("HallWaySmall");

	public static readonly int HallWayLargeHash = Animator.StringToHash("HallWayLarge");

	public static readonly int OutsideHash = Animator.StringToHash("Outside");

	public static readonly int UnderGroundHash = Animator.StringToHash("UnderGround");

	private static readonly float BlendSpeed = 5f;

	private static readonly float MinimumAtmosphere = 0.01f;

	private static readonly float RoomSizeMin = 15f;

	private static readonly float RoomSizeMax = 90f + RoomSizeMin;

	private static readonly float RoomSizeExponent = 1.4f;

	private static readonly float RoomSizeExponentMax = Mathf.Pow(RoomSizeMax - RoomSizeMin, RoomSizeExponent);

	private static readonly int RayCastDistance = 25;

	private static readonly int HallMaxWidth = 5;

	private static readonly int HallMinLength = 10;

	private static readonly int ReverbVolumeMin = -10000;

	private static readonly float TickInterval = 0.1f;

	private static readonly Vector3 AmbienceOffset = new Vector3(0f, 1f, 0f);

	[SerializeField]
	private StaticAudioSource _ambienceAudio0;

	[SerializeField]
	private StaticAudioSource _ambienceAudio1;

	private bool _ambience0IsCurrent;

	private float _ambienceVolumeMultiplier;

	private static readonly float StandardVolumeMultiplier = 1f;

	private static readonly float MarsUndergroundVolumeMultiplier = 1.8f;

	private static float DayEndTime = 0.42f;

	private static float NightStartTime = 0.52f;

	private static float SunSetTime = 0.5f;

	public WorldManager.WorldType WorldType;

	private const float MAX_WIND_AUDIO_STRENGTH = 15f;

	public static readonly int[] HitDistances = new int[6];

	private static readonly Vector3[] RaycastDirections = new Vector3[6]
	{
		Vector3.right,
		Vector3.left,
		Vector3.forward,
		Vector3.back,
		Vector3.up,
		Vector3.down
	};

	private static readonly Vector3 CeilingCheckHeight = Vector3.up * 2f;

	public static readonly Vector3[] RayCastCeilingChecks = new Vector3[4]
	{
		Vector3.right + CeilingCheckHeight,
		Vector3.left + CeilingCheckHeight,
		Vector3.forward + CeilingCheckHeight,
		Vector3.back + CeilingCheckHeight
	};

	private static readonly int MaxOpenDirections = 2;

	private AmbientAudioState _currentAmbientState;

	public ShellData[] ShellDatas = new ShellData[4];

	public AnimationCurve LocalWindCurve;

	private static readonly int RandomNoiseRangeMin = -20;

	private static readonly int RandomNoiseRangeMax = 0;

	private static readonly float MinCoolDownMultiplier = 0.85f;

	private static readonly float MaxCoolDownMultiplier = 1.2f;

	private readonly List<int> _workingList = new List<int>();

	public static bool Enclosed
	{
		get
		{
			if (Instance != null)
			{
				return Instance._enclosed;
			}
			return false;
		}
	}

	private AmbientAudioState CurrentAmbientState
	{
		get
		{
			return _currentAmbientState;
		}
		set
		{
			if (value != _currentAmbientState)
			{
				_currentAmbientState = value;
				if (InventoryManager.Parent == null)
				{
					_currentAmbientState = AmbientAudioState.NoAmbience;
					StopAmbience0();
					StopAmbience1();
				}
				else if (CurrentAmbientState == AmbientAudioState.NoAmbience)
				{
					StopAmbience0();
					StopAmbience1();
				}
				else if (_ambience0IsCurrent)
				{
					_ambienceAudio1.Play(EnumUtil.GetHashesCached(typeof(AmbientAudioState))[(int)CurrentAmbientState], _ambienceVolumeMultiplier);
					StopAmbience0();
				}
				else
				{
					_ambienceAudio0.Play(EnumUtil.GetHashesCached(typeof(AmbientAudioState))[(int)CurrentAmbientState], _ambienceVolumeMultiplier);
					StopAmbience1();
				}
			}
		}
	}

	public bool StormActive => WeatherManager.IsWeatherEventRunning;

	public void Initialize()
	{
		Instance = this;
		ReverbSetting[] reverbSettings = ReverbSettings;
		foreach (ReverbSetting reverbSetting in reverbSettings)
		{
			SettingLookup.Add(reverbSetting.NameHash, reverbSetting);
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (!(PlayerReverbZone == null) && !WorldManager.IsGamePaused && GameManager.GameState == GameState.Running && !(InventoryManager.Parent == null))
		{
			_currentSetting.SetSettingBlend(_currentSetting, _targetSetting, Time.deltaTime * BlendSpeed);
			_currentSetting.Apply(PlayerReverbZone);
		}
	}

	public override void SlowUpdate()
	{
		base.SlowUpdate();
		EnvironmentalAudioTick();
		if (GameManager.GameState != GameState.Running || !StormActive || !WeatherManager.CurrentEventAffects(InventoryManager.ParentPosition.y))
		{
			AtmosphericAudioHandler.Instance.InStormWindModifier = Mathf.Lerp(AtmosphericAudioHandler.Instance.InStormWindModifier, 0f, 0.1f);
			return;
		}
		float num = Mathf.Lerp(0f, 1f, Mathf.Clamp01((float)WeatherManager.CurrentWeatherEvent.WindStrength / 15f));
		AtmosphericAudioHandler.Instance.InStormWindModifier = (LocalWindCurve.Evaluate(GameManager.GameTime % 60f) + (float)UnityEngine.Random.Range(RandomNoiseRangeMin, RandomNoiseRangeMax)) * num;
		if ((bool)InventoryManager.Parent && InventoryManager.Parent.Room == null)
		{
			InventoryManager.ParentHuman.Exertion = 1f;
		}
	}

	private void EnvironmentalAudioTick()
	{
		try
		{
			HandleReverb();
			UpdateAmbienceState();
		}
		catch (Exception)
		{
		}
	}

	public void InitReverbZone(Entity localPlayer)
	{
		foreach (Human allHuman in Human.AllHumans)
		{
			allHuman.ReverbZone.enabled = false;
		}
		Human human = localPlayer as Human;
		if (human != null)
		{
			human.ReverbZone.enabled = true;
			PlayerReverbZone = human.ReverbZone;
			if (!WorldManager.IsGamePaused && GameManager.GameState == GameState.Running && !(InventoryManager.Parent == null))
			{
				HandleReverb();
			}
		}
	}

	public ReverbSetting GetSetting(int settingNameHash)
	{
		SettingLookup.TryGetValue(settingNameHash, out var value);
		return value;
	}

	private void HandleReverb()
	{
		if (GameManager.GameState != GameState.Running || !InventoryManager.ParentHuman)
		{
			return;
		}
		if (!PlayerReverbZone || PlayerReverbZone != InventoryManager.ParentHuman.ReverbZone)
		{
			InitReverbZone(InventoryManager.ParentHuman);
			return;
		}
		Human parentHuman = InventoryManager.ParentHuman;
		if (!parentHuman)
		{
			return;
		}
		_currentRoom = parentHuman.Room;
		Vector3 position = parentHuman.HeadBone.position;
		for (int i = 0; i < RaycastDirections.Length; i++)
		{
			if (Physics.Raycast(position, RaycastDirections[i], out var hitInfo, RayCastDistance, GameAudioSource.OcclusionLayerMask))
			{
				HitDistances[i] = Mathf.CeilToInt(hitInfo.distance);
				if (RaycastDirections[i] == Vector3.up)
				{
					_underTerrain = hitInfo.collider.gameObject.layer == GameAudioSource.TerrainLayer;
				}
			}
			else
			{
				HitDistances[i] = -1;
				if (RaycastDirections[i] == Vector3.up)
				{
					_underTerrain = false;
				}
			}
		}
		bool flag = HitDistances[0] != -1 && HitDistances[1] != -1 && HitDistances[0] + HitDistances[1] <= HallMaxWidth && (HitDistances[2] == -1 || HitDistances[3] == -1 || HitDistances[2] + HitDistances[3] >= HallMinLength);
		bool flag2 = HitDistances[2] != -1 && HitDistances[3] != -1 && HitDistances[2] + HitDistances[3] <= HallMaxWidth && (HitDistances[0] == -1 || HitDistances[1] == -1 || HitDistances[0] + HitDistances[1] >= HallMinLength);
		_hallway = flag || flag2;
		_box = flag && flag2;
		_enclosed = CalculateEnclosed();
		_hasAtmosphere = true;
		if (_enclosed)
		{
			if (_underTerrain)
			{
				_targetSetting.SetSettingBlend(GetSetting(UnderGroundHash), GetSetting(RoomHugeHash), RoomSizeRatio());
			}
			else if (_hallway && !_box)
			{
				_targetSetting.SetSettingBlend(GetSetting(HallWaySmallHash), GetSetting(HallWayLargeHash), RoomSizeRatio());
			}
			else
			{
				_targetSetting.SetSettingBlend(GetSetting(RoomSmallHash), GetSetting(RoomHugeHash), RoomSizeRatio());
			}
		}
		else
		{
			_targetSetting.Set(GetSetting(OutsideHash));
		}
		if (InventoryManager.Parent.WorldAtmosphere == null || InventoryManager.Parent.WorldAtmosphere.RatioOneAtmosphereClamped() < MinimumAtmosphere)
		{
			_targetSetting.Room = ReverbVolumeMin;
			_hasAtmosphere = false;
		}
	}

	private bool CalculateEnclosed()
	{
		if (_currentRoom != null)
		{
			return true;
		}
		int num = 0;
		int[] hitDistances = HitDistances;
		for (int i = 0; i < hitDistances.Length; i++)
		{
			if (hitDistances[i] == -1)
			{
				num++;
			}
		}
		if (num > MaxOpenDirections)
		{
			return false;
		}
		Vector3 position = InventoryManager.ParentHuman.HeadBone.position;
		int num2 = 0;
		Vector3[] rayCastCeilingChecks = RayCastCeilingChecks;
		foreach (Vector3 direction in rayCastCeilingChecks)
		{
			if (Physics.Raycast(position, direction, out var hitInfo, RayCastDistance, GameAudioSource.OcclusionLayerMask))
			{
				if (hitInfo.collider.gameObject.layer == GameAudioSource.TerrainLayer)
				{
					num2++;
				}
				if (num2 >= 2)
				{
					_underTerrain = true;
				}
				continue;
			}
			return false;
		}
		return true;
	}

	private float RoomSizeRatio()
	{
		float num = 0f;
		for (int i = 0; i < HitDistances.Length; i++)
		{
			int num2 = HitDistances[i];
			if (num2 != -1)
			{
				if (i == 4)
				{
					num2 *= 2;
				}
				num += (float)num2;
			}
		}
		num = Mathf.Clamp(num, RoomSizeMin, RoomSizeMax) - RoomSizeMin;
		num = Mathf.Pow(num, RoomSizeExponent);
		num = Mathf.Clamp(num, 0f, RoomSizeExponentMax);
		return num / RoomSizeExponentMax;
	}

	private void StopAmbience0()
	{
		_ambience0IsCurrent = false;
		if (_ambienceAudio0 != null)
		{
			_ambienceAudio0.Stop();
		}
	}

	private void StopAmbience1()
	{
		_ambience0IsCurrent = true;
		if (_ambienceAudio1 != null)
		{
			_ambienceAudio1.Stop();
		}
	}

	public void UpdateAmbienceState()
	{
		if (GameManager.GameState != GameState.Running)
		{
			WorldType = WorldManager.WorldType.Undefined;
			CurrentAmbientState = AmbientAudioState.NoAmbience;
			return;
		}
		if (WorldType == WorldManager.WorldType.Undefined)
		{
			string wName = (string.IsNullOrEmpty(WorldSetting.Current.AmbienceSound) ? WorldManager.CurrentWorldId : WorldSetting.Current.AmbienceSound);
			WorldType = WorldManager.ConvertStringToWorldType(wName);
			UpdateTransitionTimes();
		}
		if (!InventoryManager.Parent || !_hasAtmosphere)
		{
			CurrentAmbientState = AmbientAudioState.NoAmbience;
			return;
		}
		if (CameraController.IsUnderWater)
		{
			CurrentAmbientState = AmbientAudioState.UnderWater;
			return;
		}
		_ambienceVolumeMultiplier = StandardVolumeMultiplier;
		if (_enclosed)
		{
			if (WorldType == WorldManager.WorldType.Mars && _underTerrain)
			{
				_ambienceVolumeMultiplier = MarsUndergroundVolumeMultiplier;
			}
			CurrentAmbientState = ((!_underTerrain) ? AmbientAudioState.Inside : AmbientAudioState.UnderGround);
			return;
		}
		if (InventoryManager.ParentPosition.y > 1000f)
		{
			CurrentAmbientState = AmbientAudioState.NoAmbience;
			return;
		}
		if (InventoryManager.Parent?.ParentSlot?.Parent is IRocketInternals rocketInternals && rocketInternals.RocketNetwork?.Rocket != null && rocketInternals.RocketNetwork.Rocket.GetAltitude() > 1000f)
		{
			CurrentAmbientState = AmbientAudioState.NoAmbience;
			return;
		}
		switch (WorldType)
		{
		case WorldManager.WorldType.Mars:
			switch (TimeOfDay())
			{
			case AmbientAudioTime.Day:
				CurrentAmbientState = AmbientAudioState.MarsDay;
				break;
			case AmbientAudioTime.Night:
				CurrentAmbientState = AmbientAudioState.MarsNight;
				break;
			case AmbientAudioTime.Transition:
				CurrentAmbientState = AmbientAudioState.MarsTransition;
				break;
			}
			break;
		case WorldManager.WorldType.Loulan:
			switch (TimeOfDay())
			{
			case AmbientAudioTime.Day:
				CurrentAmbientState = AmbientAudioState.LoulanDay;
				break;
			case AmbientAudioTime.Night:
				CurrentAmbientState = AmbientAudioState.LoulanNight;
				break;
			case AmbientAudioTime.Transition:
				CurrentAmbientState = AmbientAudioState.LoulanTransition;
				break;
			}
			break;
		case WorldManager.WorldType.Europa:
			switch (TimeOfDay())
			{
			case AmbientAudioTime.Day:
				CurrentAmbientState = AmbientAudioState.EuropaDay;
				break;
			case AmbientAudioTime.Night:
				CurrentAmbientState = AmbientAudioState.EuropaNight;
				break;
			case AmbientAudioTime.Transition:
				CurrentAmbientState = AmbientAudioState.EuropaTransition;
				break;
			}
			break;
		case WorldManager.WorldType.Vulcan:
			switch (TimeOfDay())
			{
			case AmbientAudioTime.Day:
				CurrentAmbientState = AmbientAudioState.VulcanDay;
				break;
			case AmbientAudioTime.Night:
				CurrentAmbientState = AmbientAudioState.VulcanNight;
				break;
			case AmbientAudioTime.Transition:
				CurrentAmbientState = AmbientAudioState.VulcanTransition;
				break;
			}
			break;
		case WorldManager.WorldType.Venus:
			switch (TimeOfDay())
			{
			case AmbientAudioTime.Day:
				CurrentAmbientState = AmbientAudioState.LoulanDay;
				break;
			case AmbientAudioTime.Night:
				CurrentAmbientState = AmbientAudioState.LoulanNight;
				break;
			case AmbientAudioTime.Transition:
				CurrentAmbientState = AmbientAudioState.LoulanTransition;
				break;
			}
			break;
		default:
			CurrentAmbientState = AmbientAudioState.NoAmbience;
			break;
		}
	}

	private AmbientAudioTime TimeOfDay()
	{
		float worldAtmosphereCurveTime = OrbitalSimulation.GetWorldAtmosphereCurveTime();
		if (worldAtmosphereCurveTime > DayEndTime && worldAtmosphereCurveTime < NightStartTime)
		{
			return AmbientAudioTime.Transition;
		}
		if (worldAtmosphereCurveTime < SunSetTime)
		{
			return AmbientAudioTime.Day;
		}
		return AmbientAudioTime.Night;
	}

	private void UpdateTransitionTimes()
	{
		if (WorldType == WorldManager.WorldType.Vulcan)
		{
			DayEndTime = 0.45f;
			NightStartTime = 0.5f;
		}
		else
		{
			DayEndTime = 0.42f;
			NightStartTime = 0.52f;
		}
	}

	private void ManageEmitPositionLists()
	{
		ShellData[] shellDatas = ShellDatas;
		foreach (ShellData shellData in shellDatas)
		{
			for (int num = shellData.EmitterData.Count - 1; num >= 0; num--)
			{
				EmitterDatum emitterDatum = shellData.EmitterData[num];
				if (GameManager.GameTime > emitterDatum.CoolDownTime)
				{
					shellData.EmitterData.RemoveAt(num);
				}
				else if (Vector3.SqrMagnitude(emitterDatum.Position - InventoryManager.ParentHuman.Position) > shellData.CullDistance * shellData.CullDistance)
				{
					shellData.EmitterData.RemoveAt(num);
				}
			}
		}
	}

	private void SpawnEmittersInShells()
	{
	}

	private static bool HasEmitterAtLocation(Vector3 worldPosition, ShellData shellData)
	{
		foreach (EmitterDatum emitterDatum in shellData.EmitterData)
		{
			if (RocketMath.Approximately(emitterDatum.Position, worldPosition))
			{
				return true;
			}
		}
		return false;
	}
}
