using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Util;
using ThingImport;
using UnityEngine;

public class DifficultySetting : SettingBase
{
	[XmlAttribute("Default")]
	public bool IsDefault;

	public LocalizedStringReference Name;

	public LocalizedStringReference Description;

	public TextureReference PreviewButton;

	public FloatReference RobotBatteryRate = new FloatReference(1f);

	public FloatReference HungerRate = new FloatReference(1f);

	public FloatReference HydrationRate = new FloatReference(1f);

	public BoolReference Sanitation = new BoolReference();

	public FloatReference JetpackRate = new FloatReference(1f);

	public FloatReference BreathingRate = new FloatReference(2f);

	public FloatReference LungDamageRate = new FloatReference(1f);

	public FloatReference OfflineMetabolism = new FloatReference(0.1f);

	public FloatReference MiningYield = new FloatReference(1f);

	public BoolReference EatWhileHelmetClosed = new BoolReference();

	public BoolReference DrinkWhileHelmetClosed = new BoolReference();

	public FloatReference WeatherLanderDamageRate = new FloatReference(1f);

	public FloatReference FoodDecayRate = new FloatReference(1f);

	public FloatReference MoodRate = new FloatReference(1f);

	public FloatReference HygieneRate = new FloatReference(1f);

	public FloatReference StartingWeatherMultiplier = new FloatReference(1f);

	public BoolReference Creative = new BoolReference();

	public FloatReference SpaceMapDistanceMultiplier = new FloatReference(1f);

	public SerializedId RespawnCondition;

	public BoolReference Achievements = new BoolReference(value: true);

	public FloatReference RespawnStressTime = new FloatReference(0f);

	public FloatReference RespawnStressConsumptionSpeed = new FloatReference(1f);

	public FloatReference RespawnStressToolUseSpeed = new FloatReference(1f);

	public FloatReference RespawnStressTradePenalty = new FloatReference(1f);

	public FloatReference GoodHygieneToolSpeedMultiplier = new FloatReference(1f);

	public FloatReference MoodToolSpeedMultiplier = new FloatReference(1f);

	[XmlIgnore]
	public ModAbout Mod;

	[XmlAttribute("Hidden")]
	public bool Hidden;

	public static List<DifficultySetting> AllSettings = new List<DifficultySetting>();

	public static List<DifficultyButtonItem> AllSettingButtons = new List<DifficultyButtonItem>();

	public static DifficultySetting Default = Fallback;

	[XmlIgnore]
	private int _hash;

	private static Dictionary<int, DifficultySetting> _settingLookup = new Dictionary<int, DifficultySetting>();

	private static DifficultySetting _current;

	public static DifficultySetting Fallback => new DifficultySetting
	{
		Id = "Fallback",
		EatWhileHelmetClosed = new BoolReference(),
		DrinkWhileHelmetClosed = new BoolReference(),
		RespawnCondition = new SerializedId("Normal"),
		Hidden = true
	};

	public static DifficultySetting Tutorial => new DifficultySetting
	{
		Id = "Tutorial",
		HungerRate = new FloatReference(0.2f),
		HydrationRate = new FloatReference(0.2f),
		Sanitation = new BoolReference(value: true),
		BreathingRate = new FloatReference(2f),
		LungDamageRate = new FloatReference(1f),
		OfflineMetabolism = new FloatReference(0.1f),
		EatWhileHelmetClosed = new BoolReference(value: true),
		DrinkWhileHelmetClosed = new BoolReference(value: true),
		RespawnCondition = new SerializedId("Normal"),
		Hidden = true
	};

	[XmlIgnore]
	public int Index { get; private set; }

	public static DifficultySetting Current => _current;

	public void Initialize()
	{
		_hash = Animator.StringToHash(Id);
	}

	public int GetHash()
	{
		if (_hash == 0)
		{
			Initialize();
		}
		return _hash;
	}

	public static void OnGameDataLoad()
	{
		Register(Fallback);
		Register(Tutorial);
	}

	public static void ClearButtons()
	{
		for (int num = AllSettingButtons.Count - 1; num >= 0; num--)
		{
			Object.Destroy(AllSettingButtons[num].gameObject);
		}
		AllSettingButtons.Clear();
	}

	public static void Register(DifficultyButtonItem item)
	{
		AllSettingButtons.Add(item);
	}

	public static void Register(DifficultySetting difficultySetting, ModAbout modFile = null)
	{
		difficultySetting.Initialize();
		int hash = difficultySetting.GetHash();
		difficultySetting.Mod = modFile;
		if (!_settingLookup.TryAdd(hash, difficultySetting))
		{
			int index = _settingLookup[hash].Index;
			_settingLookup[hash] = difficultySetting;
			difficultySetting.Index = index;
			AllSettings[index] = difficultySetting;
			ConsoleWindow.Print("difficulty setting '" + difficultySetting.Id + "' updated from '" + (modFile?.Name ?? "Core") + "'");
		}
		else
		{
			difficultySetting.Index = AllSettings.Count;
			AllSettings.Add(difficultySetting);
		}
		if (difficultySetting.IsDefault)
		{
			Default = difficultySetting;
		}
		difficultySetting.PreviewButton?.Load();
	}

	public static DifficultySetting Find(int settingHash)
	{
		_settingLookup.TryGetValue(settingHash, out var value);
		return value;
	}

	public static DifficultySetting Find(string settingName)
	{
		return Find(Animator.StringToHash(settingName));
	}

	public static void SetCurrent(int worldHash)
	{
		SetCurrent(Find(worldHash));
	}

	public static void SetCurrent(string settingName)
	{
		DifficultySetting difficultySetting = Find(settingName);
		if (difficultySetting == null)
		{
			ConsoleWindow.PrintError("error difficulty setting '" + settingName + "' not found");
			SetCurrent(Fallback);
		}
		else
		{
			SetCurrent(difficultySetting);
		}
	}

	public static void SetCurrent(DifficultySetting difficultySetting)
	{
		_current = difficultySetting;
		WorldManager.SetCreativeMode(difficultySetting.Creative);
	}

	public void DebugPrint()
	{
		ConsoleWindow.Print("Id:           " + Id);
		ConsoleWindow.Print($"Description:  {Description}");
		ConsoleWindow.Print($"Creative:     {WorldManager.IsCreative()}");
	}

	public static bool MetabolismApproximately(DifficultySetting setting)
	{
		float @float = (float)setting.HungerRate / (float)Default.HungerRate;
		float float2 = (float)setting.HydrationRate / (float)Default.HydrationRate;
		float float3 = (float)setting.BreathingRate / (float)Default.BreathingRate;
		float float4 = (float)setting.RobotBatteryRate / (float)Default.RobotBatteryRate;
		if (RocketMath.Approximately(@float, float2, 0.001f) && RocketMath.Approximately(float3, float2, 0.001f))
		{
			return RocketMath.Approximately(float4, float2, 0.001f);
		}
		return false;
	}

	public static void SetCurrent()
	{
		SetCurrent(Find("Normal"));
	}
}
