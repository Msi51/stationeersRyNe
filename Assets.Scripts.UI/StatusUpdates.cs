using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class StatusUpdates : ManagerBase
{
	public class AudioEventInstance
	{
		public AudioSource Source;

		public AudioClip Clip;

		public override bool Equals(object obj)
		{
			if (obj is AudioEventInstance audioEventInstance)
			{
				return audioEventInstance.Clip == Clip;
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (!Clip)
			{
				return 0;
			}
			return Clip.GetHashCode();
		}
	}

	[Header("Status Voice")]
	public AudioSource Audio;

	public AudioClip VoiceHydrationCaution;

	public AudioClip VoiceHydrationCritical;

	[Header("Status UI")]
	public Image StatusClimbing;

	public Sprite IconBlank;

	public LayoutGroup SettingsGroup;

	public AlertItem AlertItemPrefab;

	public ControlGroupItem AlertGroupPrefab;

	private Human _human;

	private ISuit _suit;

	private BatteryCell _robotBattery;

	private GasMask _helmet;

	private IWearableLight _light;

	private DynamicThing _activeHandThing;

	private float _gForce;

	private Atmosphere _atmosphere;

	public static Entity Parent;

	public static MovementController MovementController;

	public static bool firstTime = true;

	[Header("Status: Damage")]
	public Image IndicatorHead;

	public Image IndicatorBodyUpper;

	public Image IndicatorBodyLower;

	public Image IndicatorArmLeft;

	public Image IndicatorArmRight;

	public Image IndicatorLegLeft;

	public Image IndicatorLegRight;

	public Gradient DamageGradient = new Gradient();

	[Header("Status: Suit")]
	public StatusUpdate AirTankWarning = new StatusUpdate();

	public StatusUpdate AirTankCritical = new StatusUpdate();

	public StatusUpdate FilterWarning = new StatusUpdate();

	public StatusUpdate FilterCritical = new StatusUpdate();

	public StatusUpdate CoolantWarning = new StatusUpdate();

	public StatusUpdate CoolantCritical = new StatusUpdate();

	public StatusUpdate WasteWarning = new StatusUpdate();

	public StatusUpdate WasteCritical = new StatusUpdate();

	public StatusUpdate PowerStateWarning = new StatusUpdate();

	public StatusUpdate PowerStateCritical = new StatusUpdate();

	public TMP_Text TextWaste;

	public TMP_Text TexAirTank;

	[Header("Status: Pressure")]
	public StatusUpdate PressureLowWarning = new StatusUpdate();

	public StatusUpdate PressureLowCritical = new StatusUpdate();

	public StatusUpdate PressureHighWarning = new StatusUpdate();

	public StatusUpdate PressureHighCritical = new StatusUpdate();

	private PressurekPa _pressure;

	[Header("Status: Oxygen")]
	public StatusUpdate OxygenWarning = new StatusUpdate();

	public StatusUpdate OxygenCritical = new StatusUpdate();

	[Header("Status: Temperature")]
	public StatusUpdate TemperatureLowWarning = new StatusUpdate();

	public StatusUpdate TemperatureLowCritical = new StatusUpdate();

	public StatusUpdate TemperatureHighWarning = new StatusUpdate();

	public StatusUpdate TemperatureHighCritical = new StatusUpdate();

	private TemperatureKelvin _temperature;

	[Header("Status: Toxins")]
	public StatusUpdate ToxinsWarning = new StatusUpdate();

	public StatusUpdate ToxinsCritical = new StatusUpdate();

	[Header("Status: Internals")]
	public StatusUpdate LeakWarning = new StatusUpdate();

	public StatusUpdate LeakCritical = new StatusUpdate();

	public StatusUpdate InternalsOnState = new StatusUpdate();

	public StatusUpdate InternalsOffState = new StatusUpdate();

	[Header("Status: Lights")]
	public StatusUpdate LightOnState = new StatusUpdate();

	public StatusUpdate LightOffState = new StatusUpdate();

	[Header("Status: ActiveHand")]
	public StatusUpdate ActiveHandOnState = new StatusUpdate();

	public StatusUpdate ActiveHandOffState = new StatusUpdate();

	[Header("Status: Gravity & Acceleration")]
	public StatusUpdate ZeroGeeState = new StatusUpdate();

	public StatusUpdate AccelerationWarning = new StatusUpdate();

	public StatusUpdate AccelerationCritical = new StatusUpdate();

	[Header("Status: Entity State")]
	public Image StatusState;

	private UniTask _statusStateIndicator;

	public Sprite IconState;

	public List<Sprite> IconStatusUnconscious;

	public Image StatusInSlot;

	public StatusUpdate LeavingMissionArea = new StatusUpdate();

	public StatusUpdate StatusCrewChair = new StatusUpdate();

	[Header("Status: Jetpack")]
	public StatusUpdate JetpackNotice = new StatusUpdate();

	public StatusUpdate JetpackWarning = new StatusUpdate();

	public StatusUpdate JetpackCritical = new StatusUpdate();

	[Header("Status: Hunger")]
	public StatusUpdate NutritionWarning = new StatusUpdate();

	public StatusUpdate NutritionCritical = new StatusUpdate();

	[Header("Status: Thirst")]
	public StatusUpdate HydrationWarning = new StatusUpdate();

	public StatusUpdate HydrationCritical = new StatusUpdate();

	[Header("Status: Sanitation")]
	public StatusUpdate SanitationWarning = new StatusUpdate();

	public StatusUpdate SanitationCritical = new StatusUpdate();

	public StatusUpdate SoiledCritical = new StatusUpdate();

	[Header("Status: Health")]
	public StatusUpdate HealthWarning = new StatusUpdate();

	public StatusUpdate HealthCritical = new StatusUpdate();

	public StatusUpdate StunWarning = new StatusUpdate();

	public StatusUpdate StunCritical = new StatusUpdate();

	public StatusSecondsAnimatedUpdate HealingNotice = new StatusSecondsAnimatedUpdate();

	public StatusSecondsUpdate StimulantNotice = new StatusSecondsUpdate();

	public StatusSecondsUpdate StunNotice = new StatusSecondsUpdate();

	public StatusUpdate LifeSuspendedNotice = new StatusUpdate();

	[Header("Status: Mood & Hygiene")]
	public StatusUpdate MoodWarning = new StatusUpdate();

	public StatusUpdate MoodCritical = new StatusUpdate();

	public StatusUpdate HygieneWarning = new StatusUpdate();

	public StatusUpdate HygieneCritical = new StatusUpdate();

	public StatusUpdate RefreshedNotice = new StatusUpdate();

	[Header("Status: Respawn")]
	public StatusUpdate RespawnWarning = new StatusUpdate();

	private Organ[] _organTypes;

	public static bool ShowStatusUpdates;

	public static string[] StatusUpdateTypes = Enum.GetNames(typeof(StatusUpdateType));

	public static List<StatusUpdate> AllStatusUpdates = new List<StatusUpdate>();

	public static StatusUpdates Instance;

	public static Dictionary<string, AlertItem> AlertItems = new Dictionary<string, AlertItem>();

	public Transform StatusTransform;

	private float _waitStart = 2f;

	public static float DamageIndicatorAlpha = 1f;

	private static bool _isCrewScreenAccessible = false;

	public const float WASTE_CRITICAL_VALUE = 0.95f;

	public const float WASTE_CAUTION_VALUE = 0.75f;

	private static List<AudioEventInstance> _soundEvents = new List<AudioEventInstance>();

	private static UniTask _playingSound;

	private static readonly string[] Percentages = new string[202]
	{
		"0%", "1%", "2%", "3%", "4%", "5%", "6%", "7%", "8%", "9%",
		"10%", "11%", "12%", "13%", "14%", "15%", "16%", "17%", "18%", "19%",
		"20%", "21%", "22%", "23%", "24%", "25%", "26%", "27%", "28%", "29%",
		"30%", "31%", "32%", "33%", "34%", "35%", "36%", "37%", "38%", "39%",
		"40%", "41%", "42%", "43%", "44%", "45%", "46%", "47%", "48%", "49%",
		"50%", "51%", "52%", "53%", "54%", "55%", "56%", "57%", "58%", "59%",
		"60%", "61%", "62%", "63%", "64%", "65%", "66%", "67%", "68%", "69%",
		"70%", "71%", "72%", "73%", "74%", "75%", "76%", "77%", "78%", "79%",
		"80%", "81%", "82%", "83%", "84%", "85%", "86%", "87%", "88%", "89%",
		"90%", "91%", "92%", "93%", "94%", "95%", "96%", "97%", "98%", "99%",
		"100%", "100%", "101%", "102%", "103%", "104%", "105%", "106%", "107%", "108%",
		"109%", "110%", "111%", "112%", "113%", "114%", "115%", "116%", "117%", "118%",
		"119%", "120%", "121%", "122%", "123%", "124%", "125%", "126%", "127%", "128%",
		"129%", "130%", "131%", "132%", "133%", "134%", "135%", "136%", "137%", "138%",
		"139%", "140%", "141%", "142%", "143%", "144%", "145%", "146%", "147%", "148%",
		"149%", "150%", "151%", "152%", "153%", "154%", "155%", "156%", "157%", "158%",
		"159%", "160%", "161%", "162%", "163%", "164%", "165%", "166%", "167%", "168%",
		"169%", "170%", "171%", "172%", "173%", "174%", "175%", "176%", "177%", "178%",
		"179%", "180%", "181%", "182%", "183%", "184%", "185%", "186%", "187%", "188%",
		"189%", "190%", "191%", "192%", "193%", "194%", "195%", "196%", "197%", "198%",
		"199%", "200%"
	};

	private static float _cooldown = 0.8f;

	private static StringBuilder _sb = new StringBuilder();

	public const string TOOL_TIP_GREY = "#B4B4B4";

	public bool IsClimbing
	{
		get
		{
			if (MovementController != null)
			{
				return MovementController.ControlMode == MovementController.Mode.Grab;
			}
			return false;
		}
	}

	private static MoleQuantity AirTankMolesCritical => Human.MolesPerMinute * 5.0;

	private static MoleQuantity AirTankMolesCaution => Human.MolesPerMinute * 30.0;

	public bool IsInSlot
	{
		get
		{
			if ((bool)Parent && Parent.ParentSlot != null)
			{
				return Parent.ParentSlot.Parent != null;
			}
			return false;
		}
	}

	public static Color GetDamageColor(float damage)
	{
		if (float.IsNaN(damage))
		{
			return Color.red;
		}
		return Instance.DamageGradient.Evaluate(damage);
	}

	public static void OnLanguageChanged()
	{
		foreach (KeyValuePair<string, AlertItem> alertItem in AlertItems)
		{
			alertItem.Value.Name.text = alertItem.Value.LinkedUpdate.GetDisplayName();
		}
	}

	public static void Load(List<VoiceNotificationData> list)
	{
		foreach (VoiceNotificationData voiceNotificationData in list)
		{
			StatusUpdate statusUpdate = AllStatusUpdates.Find((StatusUpdate su) => su.Key == voiceNotificationData.Notification);
			if (statusUpdate != null && statusUpdate.Display != null)
			{
				statusUpdate.IsEnabled = voiceNotificationData.IsEnabled;
			}
		}
	}

	public static bool Save(out List<VoiceNotificationData> data)
	{
		data = new List<VoiceNotificationData>();
		if (AllStatusUpdates.Count <= 0)
		{
			return false;
		}
		foreach (StatusUpdate allStatusUpdate in AllStatusUpdates)
		{
			data.Add(new VoiceNotificationData
			{
				Notification = allStatusUpdate.Type.ToString() + allStatusUpdate.DisplayName,
				IsEnabled = allStatusUpdate.IsEnabled
			});
		}
		return true;
	}

	public void Initialize()
	{
		PowerStateCritical.Register(() => DefaultToolTip(PowerStateCritical));
		PressureHighCritical.Register(() => DefaultToolTip(PressureHighCritical));
		PressureLowCritical.Register(() => DefaultToolTip(PressureLowCritical));
		OxygenCritical.Register(OxygenTooltip);
		ToxinsCritical.Register(() => DefaultToolTip(ToxinsCritical));
		WasteCritical.Register(() => DefaultToolTip(WasteCritical));
		CoolantCritical.Register(() => DefaultToolTip(CoolantCritical));
		TemperatureHighCritical.Register(() => DefaultToolTip(TemperatureHighCritical));
		TemperatureLowCritical.Register(() => DefaultToolTip(TemperatureLowCritical));
		LeakCritical.Register(() => DefaultToolTip(LeakCritical));
		NutritionCritical.Register(NutritionTooltip);
		JetpackCritical.Register(() => DefaultToolTip(JetpackCritical));
		HealthCritical.Register(() => DefaultToolTip(HealthCritical));
		SanitationCritical.Register(SanitationTooltip);
		SoiledCritical.Register(SoiledTooltip);
		FilterCritical.Register(() => DefaultToolTip(FilterCritical));
		AirTankCritical.Register(() => DefaultToolTip(AirTankCritical));
		StunCritical.Register(StunTooltip);
		HydrationCritical.Register(HydrationTooltip);
		MoodCritical.Register(MoodToolTip);
		HygieneCritical.Register(HygieneToolTip);
		StatusCrewChair.Register(CrewChairTooltip);
		PowerStateWarning.Register(() => DefaultToolTip(PowerStateWarning));
		PressureHighWarning.Register(() => DefaultToolTip(PressureHighWarning));
		PressureLowWarning.Register(() => DefaultToolTip(PressureLowWarning));
		OxygenWarning.Register(OxygenTooltip);
		ToxinsWarning.Register(() => DefaultToolTip(ToxinsWarning));
		LightOnState.Register(() => DefaultToolTip(LightOnState));
		LightOffState.Register(() => DefaultToolTip(LightOffState));
		ActiveHandOnState.Register(() => DefaultToolTip(ActiveHandOnState));
		ActiveHandOffState.Register(() => DefaultToolTip(ActiveHandOffState));
		WasteWarning.Register(() => DefaultToolTip(WasteWarning));
		CoolantWarning.Register(() => DefaultToolTip(CoolantWarning));
		TemperatureHighWarning.Register(() => DefaultToolTip(TemperatureHighWarning));
		TemperatureLowWarning.Register(() => DefaultToolTip(TemperatureLowWarning));
		LeakWarning.Register(() => DefaultToolTip(LeakWarning));
		InternalsOnState.Register(() => DefaultToolTip(InternalsOnState));
		InternalsOffState.Register(() => DefaultToolTip(InternalsOffState));
		NutritionWarning.Register(NutritionTooltip);
		HydrationWarning.Register(HydrationTooltip);
		SanitationWarning.Register(SanitationTooltip);
		JetpackWarning.Register(() => DefaultToolTip(JetpackWarning));
		JetpackNotice.Register(() => DefaultToolTip(JetpackNotice));
		HealthWarning.Register(() => DefaultToolTip(HealthWarning));
		FilterWarning.Register(() => DefaultToolTip(FilterWarning));
		AirTankWarning.Register(() => DefaultToolTip(AirTankWarning));
		ZeroGeeState.Register(() => DefaultToolTip(ZeroGeeState));
		AccelerationWarning.Register(AccelerationTooltip);
		AccelerationCritical.Register(AccelerationTooltip);
		StunWarning.Register(StunTooltip);
		MoodWarning.Register(MoodToolTip);
		HygieneWarning.Register(HygieneToolTip);
		LeavingMissionArea.Register(() => DefaultToolTip(LeavingMissionArea));
		RespawnWarning.Register(RespawnStressTooltip);
		RefreshedNotice.Register(RefreshedTooltip);
		HealingNotice.Register(MedicalHealingTooltip);
		StimulantNotice.Register(MedicalStimTooltip);
		StunNotice.Register(MedicalStunTooltip);
		LifeSuspendedNotice.Register(LifeSuspendedTooltip);
		InternalsOnState.UsesDedicatedDisplay = true;
		InternalsOffState.UsesDedicatedDisplay = true;
		LightOnState.UsesDedicatedDisplay = true;
		LightOffState.UsesDedicatedDisplay = true;
		JetpackNotice.UsesDedicatedDisplay = true;
		if (AlertItems.Count == 0)
		{
			List<ControlGroupItem> list = new List<ControlGroupItem>();
			int num = StatusUpdateTypes.Length;
			while (num-- > 0)
			{
				string text = StatusUpdateTypes[num];
				ControlGroupItem controlGroupItem = UnityEngine.Object.Instantiate(AlertGroupPrefab, SettingsGroup.transform);
				controlGroupItem.Title.text = text;
				list.Add(controlGroupItem);
			}
			AllStatusUpdates.Sort(StatusUpdate.Compare);
			foreach (StatusUpdate statusUpdate in AllStatusUpdates)
			{
				statusUpdate.SetStatusUpdateVoiceByLanguage();
				if (!(statusUpdate.AudioAlert == null))
				{
					AlertItem alertItem = UnityEngine.Object.Instantiate(AlertItemPrefab, list.Find((ControlGroupItem g) => g.Title.text == statusUpdate.Type.ToString()).transform);
					alertItem.Name.text = statusUpdate.GetDisplayName();
					alertItem.Icon.sprite = statusUpdate.Icon;
					statusUpdate.Display = alertItem;
					alertItem.Assign(statusUpdate);
				}
			}
		}
		else
		{
			foreach (StatusUpdate allStatusUpdate in AllStatusUpdates)
			{
				allStatusUpdate.SetStatusUpdateVoiceByLanguage();
				if (AlertItems.ContainsKey(allStatusUpdate.Key))
				{
					AlertItem alertItem2 = AlertItems[allStatusUpdate.Key];
					if (alertItem2 != null)
					{
						alertItem2.Assign(allStatusUpdate);
						alertItem2.Name.text = allStatusUpdate.GetDisplayName();
						alertItem2.Icon.sprite = allStatusUpdate.Icon;
						allStatusUpdate.Display = alertItem2;
					}
				}
			}
		}
		Load(Settings.CurrentData.VoiceNotifications);
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
		_organTypes = Resources.LoadAll<Organ>("Objects/");
	}

	public void DisableStatus()
	{
		_waitStart = 1f;
		Parent = null;
		for (int i = 0; i < StatusTransform.childCount; i++)
		{
			StatusTransform.GetChild(i).gameObject.SetActive(value: false);
		}
		StatusTransform.gameObject.SetActive(value: false);
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		if ((bool)Parent && GameManager.GameState == GameState.Running)
		{
			if (_waitStart < 1f)
			{
				if (!StatusTransform.gameObject.activeInHierarchy)
				{
					StatusTransform.gameObject.SetActive(value: true);
				}
				HandleIconUpdates();
				HandleDamageIndicators();
			}
			if (_waitStart > 0f)
			{
				_waitStart -= Time.deltaTime;
			}
		}
		else if (Parent == null)
		{
			_waitStart = 2f;
		}
	}

	public static Organ[] GetOrganTypes()
	{
		return Instance._organTypes;
	}

	public void HandleDamageIndicators()
	{
		float totalRatio = Parent.DamageState.TotalRatio;
		float a = (Parent.OrganBrain ? Parent.OrganBrain.DamageState.TotalRatio : 1f);
		a = Mathf.Max(a, totalRatio);
		IndicatorHead.color = DamageGradient.Evaluate(a).SetAlpha(DamageIndicatorAlpha);
		float a2 = 1f;
		switch (_human.SpeciesClass)
		{
		case SpeciesClass.Human:
		case SpeciesClass.Zrilian:
			a2 = (Parent.OrganLungs ? Parent.OrganLungs.DamageState.TotalRatio : 1f);
			break;
		case SpeciesClass.Robot:
			a2 = (_human.RobotBattery ? _human.RobotBattery.DamageState.TotalRatio : 1f);
			break;
		}
		a2 = Mathf.Max(a2, totalRatio);
		IndicatorBodyUpper.color = DamageGradient.Evaluate(a2).SetAlpha(DamageIndicatorAlpha);
		IndicatorBodyLower.color = DamageGradient.Evaluate(totalRatio).SetAlpha(DamageIndicatorAlpha);
		IndicatorArmLeft.color = DamageGradient.Evaluate(totalRatio).SetAlpha(DamageIndicatorAlpha);
		IndicatorArmRight.color = DamageGradient.Evaluate(totalRatio).SetAlpha(DamageIndicatorAlpha);
		IndicatorLegLeft.color = DamageGradient.Evaluate(totalRatio).SetAlpha(DamageIndicatorAlpha);
		IndicatorLegRight.color = DamageGradient.Evaluate(totalRatio).SetAlpha(DamageIndicatorAlpha);
	}

	public static bool IsInCrewChair()
	{
		return _isCrewScreenAccessible;
	}

	public bool IsJetpackOn()
	{
		if (MovementController != null)
		{
			if (MovementController.ControlMode != MovementController.Mode.Jetpack)
			{
				return MovementController.ControlMode == MovementController.Mode.JetpackGravity;
			}
			return true;
		}
		return false;
	}

	public bool IsJetpackPropellentCaution()
	{
		if (_human.BackpackSlot.Contains<Jetpack>(out var occupant) && occupant.PropellantLow)
		{
			return !occupant.PropellantCritical;
		}
		return false;
	}

	public bool IsJetpackPropellentCritical()
	{
		if (_human.BackpackSlot.Contains<Jetpack>(out var occupant))
		{
			return occupant.PropellantCritical;
		}
		return false;
	}

	public bool IsJetpackPropellentCautionOrCritical()
	{
		if (_human.BackpackSlot.Contains<Jetpack>(out var occupant))
		{
			if (!occupant.PropellantLow)
			{
				return occupant.PropellantCritical;
			}
			return true;
		}
		return false;
	}

	public bool IsLeavingMissionArea()
	{
		PlayableAreaRule playableAreaState = _human.PlayableAreaState;
		return playableAreaState == PlayableAreaRule.Warning || playableAreaState == PlayableAreaRule.Invalid;
	}

	public bool NotZeroGee()
	{
		if (_human.Room == null)
		{
			return WorldManager.HasGravityAtHeight(_human.Position.y);
		}
		return true;
	}

	public bool IsToxinCritical()
	{
		if (!_human)
		{
			return false;
		}
		switch (_human.SpeciesClass)
		{
		case SpeciesClass.Human:
			if (_atmosphere != null)
			{
				return _atmosphere.PartialPressureHumanToxins > Entity.ToxicPartialPressureForDamage;
			}
			return false;
		case SpeciesClass.Zrilian:
			if (_atmosphere != null)
			{
				return _atmosphere.PartialPressureZrillianToxins > Entity.ToxicPartialPressureForDamage;
			}
			return false;
		default:
			return false;
		}
	}

	public bool IsToxinCaution()
	{
		if (!_human)
		{
			return false;
		}
		switch (_human.SpeciesClass)
		{
		case SpeciesClass.Human:
			if (_atmosphere != null)
			{
				return _atmosphere.PartialPressureHumanToxins > Entity.ToxicPartialPressureForWarning;
			}
			return false;
		case SpeciesClass.Zrilian:
			if (_atmosphere != null)
			{
				return _atmosphere.PartialPressureZrillianToxins > Entity.ToxicPartialPressureForWarning;
			}
			return false;
		default:
			return false;
		}
	}

	public bool IsColdCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return _temperature < Chemistry.Temperature.ZeroDegrees - new TemperatureKelvin(10.0);
		}
		return false;
	}

	public bool IsColdCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsColdCritical())
			{
				return _temperature < Chemistry.Temperature.ZeroDegrees;
			}
			return false;
		}
		return false;
	}

	public bool IsHotCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return _temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(80.0);
		}
		return false;
	}

	public bool IsHotCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsHotCritical())
			{
				return _temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(50.0);
			}
			return false;
		}
		return false;
	}

	public bool IsPowerCaution()
	{
		if (!_human)
		{
			return false;
		}
		switch (_human.SpeciesClass)
		{
		case SpeciesClass.Human:
		case SpeciesClass.Zrilian:
			if (_suit != null && _suit.Battery != null)
			{
				return _suit.Battery.Mode <= 3;
			}
			return false;
		case SpeciesClass.Robot:
			if (_robotBattery != null)
			{
				return _robotBattery.Mode <= 3;
			}
			return false;
		default:
			return false;
		}
	}

	public bool IsPowerCritical()
	{
		if (!_human)
		{
			return false;
		}
		switch (_human.SpeciesClass)
		{
		case SpeciesClass.Human:
		case SpeciesClass.Zrilian:
			if (_suit != null)
			{
				if (!(_suit.Battery == null))
				{
					return _suit.Battery.Mode <= 1;
				}
				return true;
			}
			return false;
		case SpeciesClass.Robot:
			if (!(_robotBattery == null))
			{
				return _robotBattery.Mode <= 1;
			}
			return true;
		default:
			return false;
		}
	}

	public bool IsPressureLowCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return _pressure < Chemistry.ArmstrongLimit;
		}
		return false;
	}

	public bool IsPressureLowCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsPressureLowCritical())
			{
				return _pressure < Chemistry.Limits.PressureMinimumSafe;
			}
			return false;
		}
		return false;
	}

	public bool IsPressureHighCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return _pressure > Chemistry.Limits.PressureMaximumSafe;
		}
		return false;
	}

	public bool IsPressureHighCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsPressureHighCritical())
			{
				return _pressure > Chemistry.Limits.PressureMaximumSafe * 0.5;
			}
			return false;
		}
		return false;
	}

	public bool IsWasteCritical()
	{
		ISuit suit = _suit;
		if (suit != null && suit.HasWasteTankSlot)
		{
			if ((bool)_suit.WasteTank && !_suit.WasteTank.IsBroken)
			{
				return _suit.WasteTank.Pressure >= _suit.WasteMaxPressure * 0.949999988079071;
			}
			return true;
		}
		return false;
	}

	public bool IsWasteCaution()
	{
		if (!IsWasteCritical())
		{
			ISuit suit = _suit;
			if (suit != null && suit.HasWasteTankSlot)
			{
				if ((bool)_suit.WasteTank)
				{
					return _suit.WasteTank.Pressure >= _suit.WasteMaxPressure * 0.75;
				}
				return false;
			}
		}
		return false;
	}

	public bool IsCoolantCritical()
	{
		if (_suit is SuitBase suitBase)
		{
			return suitBase.IsCoolantCritical();
		}
		return false;
	}

	public bool IsCoolantCaution()
	{
		if (_suit is SuitBase suitBase)
		{
			return suitBase.IsCoolantWarning();
		}
		return false;
	}

	public static bool IsBelow(GasCanister canister, MoleQuantity moles)
	{
		if (canister.InternalAtmosphere != null)
		{
			return canister.InternalAtmosphere.TotalMoles < moles;
		}
		return false;
	}

	public bool IsAirTankCritical()
	{
		if (_suit != null)
		{
			if ((bool)_suit.AirTank && !_suit.AirTank.IsBroken)
			{
				return IsBelow(_suit.AirTank, AirTankMolesCritical);
			}
			return true;
		}
		return false;
	}

	public bool IsAirTankCaution()
	{
		if (!IsAirTankCritical() && _suit != null)
		{
			if ((bool)_suit.AirTank)
			{
				return IsBelow(_suit.AirTank, AirTankMolesCaution);
			}
			return false;
		}
		return false;
	}

	public bool IsHydrationCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if ((bool)Parent)
			{
				return Parent.Hydration < Parent.CriticalHydration;
			}
			return false;
		}
		return false;
	}

	public bool IsHydrationCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsHydrationCritical() && (bool)Parent)
			{
				return Parent.Hydration < Parent.WarningHydration;
			}
			return false;
		}
		return false;
	}

	public bool IsSanitationCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return !IsSanitationCritical() && (bool)Parent && Parent.SanitationRatio > Parent.CautionSanitation;
		}
		return false;
	}

	public bool IsSanitationCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return (bool)Parent && Parent.SanitationRatio > Parent.CriticalSanitation;
		}
		return false;
	}

	public bool IsSoiled()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return (bool)Parent && Parent.IsSoiled;
		}
		return false;
	}

	public bool IsNutritionCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return (bool)Parent && Parent.Nutrition < Parent.CriticalNutrition;
		}
		return false;
	}

	public bool IsNutritionCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			return !IsNutritionCritical() && (bool)Parent && Parent.Nutrition < Parent.WarningNutrition;
		}
		return false;
	}

	public bool IsNutritionFull()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if ((bool)Parent)
			{
				return Parent.Nutrition >= Parent.FullNutrition;
			}
			return false;
		}
		return false;
	}

	public bool IsNutritionCautionOrCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if (!IsNutritionCritical())
			{
				return IsNutritionCaution();
			}
			return true;
		}
		return false;
	}

	public bool IsHealthCritical()
	{
		if ((bool)Parent)
		{
			return Parent.DamageState.TotalRatio > Parent.CriticalHealth;
		}
		return false;
	}

	public bool IsHealthCaution()
	{
		if (!IsHealthCritical() && (bool)Parent)
		{
			return Parent.DamageState.TotalRatio > Parent.WarningHealth;
		}
		return false;
	}

	public bool IsHygieneCritical()
	{
		if ((bool)Parent)
		{
			return Parent.Hygiene <= Parent.CriticalHygiene;
		}
		return false;
	}

	public bool IsHygieneCaution()
	{
		if (!IsHygieneCritical() && (bool)Parent)
		{
			return Parent.Hygiene < Parent.WarningHygiene;
		}
		return false;
	}

	public bool IsMoodCritical()
	{
		if ((bool)Parent)
		{
			return Parent.Mood <= Parent.CriticalMood;
		}
		return false;
	}

	public bool IsMoodCaution()
	{
		if (!IsMoodCritical() && (bool)Parent)
		{
			return Parent.Mood < Parent.WarningMood;
		}
		return false;
	}

	public bool IsLifeSuspended()
	{
		if (Parent?.ParentSlot?.Parent is ILifeSuspender lifeSuspender)
		{
			return lifeSuspender.IsSuspendingLife;
		}
		return false;
	}

	public bool IsStunCritical()
	{
		if ((bool)Parent && !IsLifeSuspended())
		{
			return Parent.DamageState.Stun / 100f > Parent.CriticalStun;
		}
		return false;
	}

	public bool IsStunCaution()
	{
		if (!IsStunCritical() && !IsLifeSuspended() && (bool)Parent)
		{
			return Parent.DamageState.Stun / 100f > Parent.WarningStun;
		}
		return false;
	}

	public bool HasRespawned()
	{
		if ((bool)Parent)
		{
			return Parent.RespawnStressTime > 0f;
		}
		return false;
	}

	public bool IsRefreshed()
	{
		if ((bool)Parent)
		{
			return Parent.Hygiene > 1f;
		}
		return false;
	}

	public bool IsHealing()
	{
		return Parent?.IsHealing() ?? false;
	}

	public bool IsStimmed()
	{
		return Parent?.IsStimmed() ?? false;
	}

	public bool IsStunned()
	{
		return Parent?.IsStunned() ?? false;
	}

	public bool IsFilterCritical()
	{
		if (_suit != null)
		{
			if (_suit.HasFilters)
			{
				return _suit.EmptyFilter;
			}
			return true;
		}
		return false;
	}

	public bool IsFilterCaution()
	{
		if (!IsFilterCritical() && _suit != null)
		{
			return _suit.LowFilter;
		}
		return false;
	}

	public bool IsOxygenCritical()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if ((bool)Parent)
			{
				return Parent.OxygenQuality < Parent.CriticalOxygen;
			}
			return false;
		}
		return false;
	}

	public bool IsOxygenCaution()
	{
		if (!_human)
		{
			return false;
		}
		SpeciesClass speciesClass = _human.SpeciesClass;
		if (speciesClass - 1 <= SpeciesClass.Human)
		{
			if ((bool)Parent)
			{
				return Parent.OxygenQuality < Parent.WarningOxygen;
			}
			return false;
		}
		return false;
	}

	public bool IsLeakingCritical()
	{
		if ((bool)_human)
		{
			if (_human.Suit == null || !(_human.Suit.LeakRatio > 0.2f))
			{
				if (_human.HeadAsSpaceHelmet != null)
				{
					return _human.HeadAsSpaceHelmet.LeakRatio > 0.2f;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool IsLeakingCaution()
	{
		if (!IsLeakingCritical())
		{
			if (!_human || _human.Suit == null || !(_human.Suit.LeakRatio > 0f))
			{
				if ((bool)_human && _human.HeadAsSpaceHelmet != null)
				{
					return _human.HeadAsSpaceHelmet.LeakRatio > 0f;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool IsInternalsOn()
	{
		if (_helmet != null && _helmet.IsOpen)
		{
			PlayerStateWindow.Instance.HelmetImageToggle.SetImage(1);
		}
		else
		{
			PlayerStateWindow.Instance.HelmetImageToggle.SetImage(0);
		}
		if ((bool)_helmet)
		{
			return !_helmet.IsOpen;
		}
		return false;
	}

	public bool IsInternalsOff()
	{
		if ((bool)_helmet)
		{
			return _helmet.IsOpen;
		}
		return false;
	}

	public bool IsLightOn()
	{
		if (_light != null && _light.OnOff)
		{
			PlayerStateWindow.Instance.LightPanel.SetActive(value: true);
			PlayerStateWindow.Instance.LightImageToggle.SetImage(1);
		}
		else if (_light != null && !_light.OnOff)
		{
			PlayerStateWindow.Instance.LightPanel.SetActive(value: true);
			PlayerStateWindow.Instance.LightImageToggle.SetImage(0);
		}
		else
		{
			PlayerStateWindow.Instance.LightPanel.SetActive(value: false);
		}
		if (_light != null)
		{
			return _light.OnOff;
		}
		return false;
	}

	public bool IsLightOff()
	{
		if (_light != null)
		{
			return !_light.OnOff;
		}
		return false;
	}

	public bool IsHandPowerOn()
	{
		if ((bool)_activeHandThing && _activeHandThing.HasOnOffState)
		{
			return _activeHandThing.OnOff;
		}
		return false;
	}

	public bool IsHandPowerOff()
	{
		if ((bool)_activeHandThing && _activeHandThing.HasOnOffState)
		{
			return !_activeHandThing.OnOff;
		}
		return false;
	}

	public static async UniTask PlayWarnings()
	{
		while (_soundEvents.Count > 0)
		{
			AudioEventInstance soundEvent = _soundEvents[0];
			while (soundEvent.Source.isPlaying)
			{
				await UniTask.WaitForEndOfFrame();
			}
			if (soundEvent.Clip != null)
			{
				soundEvent.Source.clip = soundEvent.Clip;
				soundEvent.Source.Play();
				await UniTask.Delay(Mathf.RoundToInt((soundEvent.Clip.length + 0.5f) * 1000f), ignoreTimeScale: true);
			}
			_soundEvents.RemoveAt(0);
		}
	}

	public static void PlaySound(AudioSource audioSource, AudioClip audioClip, bool force = false)
	{
		if (GameManager.IsBatchMode || (!force && (GameManager.GameState != GameState.Running || Instance._waitStart > 0f)))
		{
			return;
		}
		AudioEventInstance item = new AudioEventInstance
		{
			Clip = audioClip,
			Source = audioSource
		};
		if (!_soundEvents.Contains(item))
		{
			_soundEvents.Add(item);
			if (_playingSound.Status != UniTaskStatus.Pending)
			{
				_playingSound = PlayWarnings();
			}
		}
	}

	private void RunFlashPower()
	{
		bool flag = IsPowerCritical();
		if (flag != PowerStateCritical._lastFlashState)
		{
			PowerStateCritical._lastFlashState = flag;
			if (PowerStateCritical.UpdateTask.Status != UniTaskStatus.Pending && IsPowerCritical())
			{
				PowerStateCritical.UpdateTask = PowerStateCritical.FlashState(IsPowerCritical, IsPowerCaution);
			}
		}
	}

	private void RunFlashPressureHigh()
	{
		bool flag = IsPressureHighCritical();
		if (flag != PressureHighCritical._lastFlashState)
		{
			PressureHighCritical._lastFlashState = flag;
			if (PressureHighCritical.UpdateTask.Status != UniTaskStatus.Pending && IsPressureHighCritical())
			{
				PressureHighCritical.UpdateTask = PressureHighCritical.FlashState(IsPressureHighCritical, IsPressureHighCaution);
			}
		}
	}

	private void RunFlashPressureLow()
	{
		bool flag = IsPressureLowCritical();
		if (flag != PressureLowCritical._lastFlashState)
		{
			PressureLowCritical._lastFlashState = flag;
			if (PressureLowCritical.UpdateTask.Status != UniTaskStatus.Pending && IsPressureLowCritical())
			{
				PressureLowCritical.UpdateTask = PressureLowCritical.FlashState(IsPressureLowCritical, IsPressureLowCaution);
			}
		}
	}

	private void RunFlashOxygen()
	{
		bool flag = IsOxygenCritical();
		if (flag != OxygenCritical._lastFlashState)
		{
			OxygenCritical._lastFlashState = flag;
			if (OxygenCritical.UpdateTask.Status != UniTaskStatus.Pending && IsOxygenCritical())
			{
				OxygenCritical.UpdateTask = OxygenCritical.FlashState(IsOxygenCritical, IsOxygenCaution);
			}
		}
	}

	private void RunFlashToxin()
	{
		bool flag = IsToxinCritical();
		if (flag != ToxinsCritical._lastFlashState)
		{
			ToxinsCritical._lastFlashState = flag;
			if (ToxinsCritical.UpdateTask.Status != UniTaskStatus.Pending && IsToxinCritical())
			{
				ToxinsCritical.UpdateTask = ToxinsCritical.FlashState(IsToxinCritical, IsToxinCaution);
			}
		}
	}

	private void RunFlashWaste()
	{
		bool flag = IsWasteCritical();
		if (flag != WasteCritical._lastFlashState)
		{
			WasteCritical._lastFlashState = flag;
			if (WasteCritical.UpdateTask.Status != UniTaskStatus.Pending && IsWasteCritical())
			{
				WasteCritical.UpdateTask = WasteCritical.FlashState(IsWasteCritical, IsWasteCaution);
			}
		}
	}

	private void RunFlashCoolant()
	{
		bool flag = IsCoolantCritical();
		if (flag != CoolantCritical._lastFlashState)
		{
			CoolantCritical._lastFlashState = flag;
			if (CoolantCritical.UpdateTask.Status != UniTaskStatus.Pending && IsCoolantCritical())
			{
				CoolantCritical.UpdateTask = CoolantCritical.FlashState(IsCoolantCritical, IsCoolantCaution);
			}
		}
	}

	private void RunFlashTemperatureHigh()
	{
		bool flag = IsHotCritical();
		if (flag != TemperatureHighCritical._lastFlashState)
		{
			TemperatureHighCritical._lastFlashState = flag;
			if (TemperatureHighCritical.UpdateTask.Status != UniTaskStatus.Pending && IsHotCritical())
			{
				TemperatureHighCritical.UpdateTask = TemperatureHighCritical.FlashState(IsHotCritical, IsHotCaution);
			}
		}
	}

	private void RunFlashTemperatureLow()
	{
		bool flag = IsColdCritical();
		if (flag != TemperatureLowCritical._lastFlashState)
		{
			TemperatureLowCritical._lastFlashState = flag;
			if (TemperatureLowCritical.UpdateTask.Status != UniTaskStatus.Pending && IsColdCritical())
			{
				TemperatureLowCritical.UpdateTask = TemperatureLowCritical.FlashState(IsColdCritical, IsColdCaution);
			}
		}
	}

	private void RunFlashLeak()
	{
		bool flag = IsLeakingCritical();
		if (flag != LeakCritical._lastFlashState)
		{
			LeakCritical._lastFlashState = flag;
			if (LeakCritical.UpdateTask.Status != UniTaskStatus.Pending && IsLeakingCritical())
			{
				LeakCritical.UpdateTask = LeakCritical.FlashState(IsLeakingCritical, IsLeakingCaution);
			}
		}
	}

	private void RunFlashNutrition()
	{
		bool flag = IsNutritionCritical();
		if (flag != NutritionCritical._lastFlashState)
		{
			NutritionCritical._lastFlashState = flag;
			if (NutritionCritical.UpdateTask.Status != UniTaskStatus.Pending && IsNutritionCritical())
			{
				NutritionCritical.UpdateTask = NutritionCritical.FlashState(IsNutritionCritical, IsNutritionCaution);
			}
		}
	}

	private void RunFlashHydration()
	{
		bool flag = IsHydrationCritical();
		if (flag != HydrationCritical._lastFlashState)
		{
			HydrationCritical._lastFlashState = flag;
			if (HydrationCritical.UpdateTask.Status != UniTaskStatus.Pending && IsHydrationCritical())
			{
				HydrationCritical.UpdateTask = HydrationCritical.FlashState(IsHydrationCritical, IsHydrationCaution);
			}
		}
	}

	private void RunFlashJetpack()
	{
		bool flag = IsJetpackPropellentCritical();
		if (flag != JetpackCritical._lastFlashState)
		{
			JetpackCritical._lastFlashState = flag;
			if (JetpackCritical.UpdateTask.Status != UniTaskStatus.Pending && IsJetpackPropellentCritical())
			{
				JetpackCritical.UpdateTask = JetpackCritical.FlashState(IsJetpackPropellentCritical, IsJetpackPropellentCaution);
			}
		}
	}

	private void RunFlashHealth()
	{
		bool flag = IsHealthCritical();
		if (flag != HealthCritical._lastFlashState)
		{
			HealthCritical._lastFlashState = flag;
			if (HealthCritical.UpdateTask.Status != UniTaskStatus.Pending && IsHealthCritical())
			{
				HealthCritical.UpdateTask = HealthCritical.FlashState(IsHealthCritical, IsHealthCaution);
			}
		}
	}

	private void RunFlashSanitation()
	{
		bool flag = IsSanitationCritical();
		if (flag != SanitationCritical._lastFlashState)
		{
			SanitationCritical._lastFlashState = flag;
			if (SanitationCritical.UpdateTask.Status != UniTaskStatus.Pending && IsSanitationCritical())
			{
				SanitationCritical.UpdateTask = SanitationCritical.FlashState(IsSanitationCritical, IsSanitationCaution);
			}
		}
	}

	private void RunFlashLeavingMissionArea()
	{
		bool flag = IsLeavingMissionArea();
		if (flag != LeavingMissionArea._lastFlashState)
		{
			LeavingMissionArea._lastFlashState = flag;
			if (LeavingMissionArea.UpdateTask.Status != UniTaskStatus.Pending)
			{
				LeavingMissionArea.UpdateTask = LeavingMissionArea.FlashState(IsLeavingMissionArea);
			}
		}
	}

	private void RunFlashMood()
	{
		bool flag = IsMoodCritical();
		if (flag != MoodCritical._lastFlashState)
		{
			MoodCritical._lastFlashState = flag;
			if (MoodCritical.UpdateTask.Status != UniTaskStatus.Pending && IsMoodCritical())
			{
				MoodCritical.UpdateTask = MoodCritical.FlashState(IsMoodCritical, IsMoodCaution);
			}
		}
	}

	private void RunFlashFilter()
	{
		bool flag = IsFilterCritical();
		if (flag != FilterCritical._lastFlashState)
		{
			FilterCritical._lastFlashState = flag;
			if (FilterCritical.UpdateTask.Status != UniTaskStatus.Pending && IsFilterCritical())
			{
				FilterCritical.UpdateTask = FilterCritical.FlashState(IsFilterCritical, IsFilterCaution);
			}
		}
	}

	private void RunFlashAirTank()
	{
		bool flag = IsAirTankCritical();
		if (flag != AirTankCritical._lastFlashState)
		{
			AirTankCritical._lastFlashState = flag;
			if (AirTankCritical.UpdateTask.Status != UniTaskStatus.Pending && IsAirTankCritical())
			{
				AirTankCritical.UpdateTask = AirTankCritical.FlashState(IsAirTankCritical, IsAirTankCaution);
			}
		}
	}

	private void RunFlashStun()
	{
		bool flag = IsStunCritical();
		if (flag != StunCritical._lastFlashState)
		{
			StunCritical._lastFlashState = flag;
			if (StunCritical.UpdateTask.Status != UniTaskStatus.Pending && IsStunCritical())
			{
				StunCritical.UpdateTask = StunCritical.FlashState(IsStunCritical, IsStunCaution);
			}
		}
	}

	public static void RefreshAtmosphereValues()
	{
		if (!(Instance == null) && !(Parent == null))
		{
			Instance._atmosphere = Parent.BreathingAtmosphere;
			Instance._pressure = Instance._atmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero;
			TemperatureKelvin temperature = Instance._atmosphere?.Temperature ?? TemperatureKelvin.Zero;
			Thing rootParent = Parent.RootParent;
			if (rootParent is CryoTube { WillRevive: not false } && rootParent.Powered && !rootParent.IsOpen)
			{
				temperature = new TemperatureKelvin(268.15);
			}
			Instance._temperature = temperature;
		}
	}

	private void HandleIconUpdates()
	{
		_human = Parent?.AsHuman;
		_suit = _human?.Suit;
		_robotBattery = _human?.RobotBattery;
		_helmet = _human?.HeadAsSpaceHelmet;
		_light = _human?.HelmetSlot.Get<IWearableLight>();
		_activeHandThing = InventoryManager.ActiveHandSlot?.Occupant;
		_gForce = _human?.GForce ?? 0f;
		CacheCrewScreen();
		RunFlashPower();
		RunFlashPressureHigh();
		RunFlashPressureLow();
		RunFlashOxygen();
		RunFlashToxin();
		RunFlashWaste();
		RunFlashCoolant();
		RunFlashTemperatureHigh();
		RunFlashTemperatureLow();
		RunFlashLeak();
		RunFlashNutrition();
		RunFlashHydration();
		RunFlashJetpack();
		RunFlashHealth();
		RunFlashSanitation();
		RunFlashMood();
		RunFlashFilter();
		RunFlashAirTank();
		RunFlashStun();
		RunFlashLeavingMissionArea();
		UpdateHelmetPanel();
		PlayerStateWindow.Instance.JetPackImageToggle.SetImage(IsJetpackOn() ? 1 : 0);
		PowerStateWarning.RunStaticState(IsPowerCaution(), IsPowerCritical());
		PressureHighWarning.RunStaticState(IsPressureHighCaution(), IsPressureHighCritical());
		PressureLowWarning.RunStaticState(IsPressureLowCaution(), IsPressureLowCritical());
		OxygenWarning.RunStaticState(IsOxygenCaution(), IsOxygenCritical());
		ToxinsWarning.RunStaticState(IsToxinCaution(), IsToxinCritical());
		LightOnState.RunStaticState(IsLightOn(), IsLightOff());
		SoiledCritical.RunStaticState(IsSoiled());
		HygieneCritical.RunStaticState(IsHygieneCritical(), IsHygieneCaution());
		WasteWarning.RunStaticState(IsWasteCaution(), IsWasteCritical());
		CoolantWarning.RunStaticState(IsCoolantCaution(), IsCoolantCritical());
		TemperatureHighWarning.RunStaticState(IsHotCaution(), IsHotCritical());
		TemperatureLowWarning.RunStaticState(IsColdCaution(), IsColdCritical());
		LeakWarning.RunStaticState(IsLeakingCaution(), IsLeakingCritical());
		NutritionWarning.RunStaticState(IsNutritionCaution(), IsNutritionCritical());
		HydrationWarning.RunStaticState(IsHydrationCaution(), IsHydrationCritical());
		SanitationWarning.RunStaticState(IsSanitationCaution(), IsSanitationCritical());
		JetpackWarning.RunStaticState(IsJetpackPropellentCaution(), IsJetpackPropellentCritical());
		JetpackNotice.RunStaticState(IsJetpackOn());
		StatusCrewChair.RunStaticState(IsInCrewChair());
		HealthWarning.RunStaticState(IsHealthCaution(), IsHealthCritical());
		MoodWarning.RunStaticState(IsMoodCaution(), IsMoodCritical());
		HygieneWarning.RunStaticState(IsHygieneCaution(), IsHygieneCritical());
		FilterWarning.RunStaticState(IsFilterCaution(), IsFilterCritical());
		AirTankWarning.RunStaticState(IsAirTankCaution(), IsAirTankCritical());
		ZeroGeeState.RunStaticState(IsZeroGee());
		AccelerationWarning.RunStaticState(IsAccelerationCaution(), IsAccelerationCritical());
		AccelerationCritical.RunStaticState(IsAccelerationCritical(), IsAccelerationCaution());
		StunWarning.RunStaticState(IsStunCaution(), IsStunCritical());
		RespawnWarning.RunStaticState(HasRespawned());
		RefreshedNotice.RunStaticState(IsRefreshed());
		HealingNotice.RunStaticState(IsHealing());
		LifeSuspendedNotice.RunStaticState(IsLifeSuspended());
		if (IsHealing())
		{
			HealingNotice.UpdateSprite(Time.unscaledDeltaTime);
			HealingNotice.UpdateText(Parent?.GetTime<IHealEffectMoodle>() ?? 0f);
		}
		StimulantNotice.RunStaticState(IsStimmed());
		if (IsStimmed())
		{
			StimulantNotice.UpdateText(Parent?.GetTime<IStimEffectMoodle>() ?? 0f);
		}
		StunNotice.RunStaticState(IsStunned());
		if (IsStunned())
		{
			StunNotice.UpdateText(Parent?.GetTime<IStunEffectMoodle>() ?? 0f);
		}
		firstTime = false;
		if (WasteCritical.Image.gameObject.activeSelf)
		{
			TextWaste.text = GetPercentageString(((bool)_human.Suit?.AsThing && (bool)_human.Suit.WasteTank) ? ((_human.Suit.WasteTank.Pressure / _human.Suit.WasteMaxPressure).ToFloat() * 100f) : 0f);
		}
		if (AirTankCritical.Image.gameObject.activeSelf)
		{
			TexAirTank.text = GetPercentageString(((bool)_human.Suit?.AsThing && (bool)_human.Suit.AirTank) ? ((_human.Suit.AirTank.InternalAtmosphere.TotalMoles / AirTankMolesCaution).ToFloat() * 100f) : 0f);
		}
		StatusClimbing.gameObject.SetActive(IsClimbing);
		StatusInSlot.gameObject.SetActive(IsInSlot);
		StatusState.gameObject.SetActive(Parent.Unconscious);
		if (_statusStateIndicator.Status != UniTaskStatus.Pending && Parent.Unconscious)
		{
			_statusStateIndicator = UnconsciousOn();
		}
	}

	private void UpdateHelmetPanel()
	{
		Human parentHuman = InventoryManager.ParentHuman;
		if (!(parentHuman == null))
		{
			if (parentHuman.Suit == null)
			{
				PlayerStateWindow.Instance.HelmetPanel.SetActive(value: false);
			}
			else if (parentHuman.HeadAsSpaceHelmet != null)
			{
				PlayerStateWindow.Instance.HelmetPanel.SetActive(value: true);
			}
		}
	}

	private void CacheCrewScreen()
	{
		if (Parent == null || !(Parent.ParentSlot?.Parent is CrewModuleChair crewModuleChair))
		{
			_isCrewScreenAccessible = false;
			return;
		}
		foreach (IRocketInternals @internal in crewModuleChair.RocketNetwork.Internals)
		{
			if (!(@internal is RocketDataDownLink rocketDataDownLink))
			{
				continue;
			}
			foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in rocketDataDownLink.ConnectedDataNetReceivers)
			{
				foreach (Device device in connectedDataNetReceiver.DataCableNetwork.DeviceList)
				{
					if (device is Computer computer && computer.CurrentMotherboard is RocketMotherboard)
					{
						_isCrewScreenAccessible = true;
						return;
					}
				}
			}
		}
	}

	private bool IsZeroGee()
	{
		return _gForce < 0.01f;
	}

	private bool IsAccelerationCaution()
	{
		return _gForce >= 1.5f;
	}

	private bool IsAccelerationCritical()
	{
		return _gForce >= 4f;
	}

	private static string GetPercentageString(float val)
	{
		int num = (int)val;
		if (num >= 0 && num < Percentages.Length)
		{
			return Percentages[num];
		}
		return "Out of range: " + num;
	}

	public static void ResetStatusIcons()
	{
		if (Instance == null)
		{
			return;
		}
		Instance.StopAllCoroutines();
		foreach (StatusUpdate allStatusUpdate in AllStatusUpdates)
		{
			allStatusUpdate.Reset();
		}
	}

	private async UniTask UnconsciousOn()
	{
		StatusState.gameObject.SetActive(value: true);
		int index = 0;
		while (Parent != null && Parent.Unconscious)
		{
			if (index >= IconStatusUnconscious.Count)
			{
				index = 0;
			}
			StatusState.sprite = IconStatusUnconscious[index];
			await UniTask.Delay(500);
			index++;
		}
		StatusState.sprite = IconState;
	}

	public static void OnSettingChanged()
	{
		Instance.Initialize();
		AllStatusUpdates.Pick()?.Speak(force: true);
	}

	public string DefaultToolTip(StatusUpdate statusUpdate)
	{
		_sb.Clear();
		if ((bool)statusUpdate?.ConnectedIcon)
		{
			if (!string.IsNullOrEmpty(statusUpdate.ConnectedIcon.DisplayNameKey))
			{
				_sb.Append(Localization.GetToolTip(statusUpdate.ConnectedIcon.DisplayNameKey));
			}
			if (!string.IsNullOrEmpty(statusUpdate.ConnectedIcon.DescriptionKey))
			{
				_sb.Newline();
				_sb.Append(StringManager.Indent).Append(Localization.GetToolTip(statusUpdate.ConnectedIcon.DescriptionKey).AsColor("#B4B4B4"));
			}
		}
		return _sb.ToString();
	}

	public string CrewChairTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.CrewChairHeading);
		_sb.Newline();
		_sb.Append(GameStrings.CrewChairDescription.AsString(Parent.ParentSlot?.Parent?.ToTooltip() ?? "Unknown").AsColor("#B4B4B4"));
		return _sb.ToString();
	}

	public string HygieneToolTip()
	{
		_sb.Clear();
		string arg = PlayerStatsPanel.StatDeltaString(InventoryManager.ParentHuman.CalculateHygieneChange());
		_sb.AppendLine(GameStrings.PlayerStatsHygieneDeltaState.AsString(arg));
		if (InventoryManager.ParentHuman.SuitOrHelmetOn())
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateHygieneDecreasing, GameStrings.PlayerStatsSuitOrHelmetOn.AsColor("red"), "#B4B4B4", indent: true);
		}
		else
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateHygieneIncreasing, GameStrings.PlayerStatsNoSuitOrHelmet.AsColor("green"), "#B4B4B4", indent: true);
		}
		if (InventoryManager.ParentHuman.IsSoiled)
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateHygieneDecreasing, GameStrings.SoiledHeading.AsColor("red"), "#B4B4B4", indent: true);
		}
		return _sb.ToString();
	}

	public string MoodToolTip()
	{
		_sb.Clear();
		string arg = PlayerStatsPanel.StatDeltaString(InventoryManager.ParentHuman.CalculateMoodChange());
		_sb.AppendLine(GameStrings.PlayerStatsMoodDeltaState.AsString(arg));
		if (InventoryManager.ParentHuman.HygieneLow)
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateMoodDecreasing, GameStrings.PlayerStatsLowHygiene.AsColor("red"), "#B4B4B4", indent: true);
		}
		else if (InventoryManager.ParentHuman.HygieneOk)
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateMoodIncreasing, GameStrings.PlayerStatsHygieneOk.AsColor("green"), "#B4B4B4", indent: true);
		}
		if (InventoryManager.ParentHuman.RoomStateOk())
		{
			StringManager.AddKeyValueLine(_sb, GameStrings.PlayerStateMoodIncreasing, GameStrings.PlayerStatsRoomStateOk.AsColor("green"), "#B4B4B4", indent: true);
		}
		if (InventoryManager.ParentHuman.Mood < float.Epsilon)
		{
			if ((float)DifficultySetting.Current.MoodToolSpeedMultiplier < 1f || (float)DifficultySetting.Current.MoodToolSpeedMultiplier > 1f)
			{
				int num = Mathf.RoundToInt((float)DifficultySetting.Current.MoodToolSpeedMultiplier * 100f);
				StringManager.AddKeyValueLine(_sb, GameStrings.ToolUseSpeed, num.ToStringPercent((num < 100) ? "red" : "green"), "#B4B4B4", indent: true);
			}
			int value = Mathf.RoundToInt(95f);
			StringManager.AddKeyValueLine(_sb, GameStrings.MovementSpeed, value.ToStringPercent("red"), "#B4B4B4", indent: true);
		}
		return _sb.ToString();
	}

	public string RespawnStressTooltip()
	{
		_sb.Clear();
		_sb.AppendLine(GameStrings.EntityRespawnStress);
		if ((float)DifficultySetting.Current.RespawnStressConsumptionSpeed < 1f || (float)DifficultySetting.Current.RespawnStressConsumptionSpeed > 1f)
		{
			int value = Mathf.RoundToInt((float)DifficultySetting.Current.RespawnStressConsumptionSpeed * 100f);
			StringManager.AddKeyValueLine(_sb, GameStrings.ConsumptionSpeed, value.ToStringPercent(((float)DifficultySetting.Current.RespawnStressConsumptionSpeed < 1f) ? "red" : "green"), "#B4B4B4", indent: true);
		}
		if ((float)DifficultySetting.Current.RespawnStressToolUseSpeed < 1f)
		{
			int value2 = Mathf.RoundToInt((float)DifficultySetting.Current.RespawnStressToolUseSpeed * 100f);
			StringManager.AddKeyValueLine(_sb, GameStrings.ToolUseSpeed, value2.ToStringPercent(((float)DifficultySetting.Current.RespawnStressToolUseSpeed < 1f) ? "red" : "green"), "#B4B4B4", indent: true);
		}
		if ((float)DifficultySetting.Current.RespawnStressTradePenalty < 1f)
		{
			int value3 = Mathf.RoundToInt((float)DifficultySetting.Current.RespawnStressTradePenalty * 100f);
			StringManager.AddKeyValueLine(_sb, GameStrings.TradePenalty, value3.ToStringPercent(((float)DifficultySetting.Current.RespawnStressTradePenalty < 1f) ? "red" : "green"), "#B4B4B4", indent: true);
		}
		return _sb.ToString();
	}

	public string OxygenTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.Oxygenation).Append(' ').Append(IsOxygenCritical() ? GameStrings.Critical : GameStrings.Warning);
		_sb.Newline();
		if (InventoryManager.ParentHuman?.OrganLungs != null)
		{
			PressurekPa partialPressureO = InventoryManager.ParentHuman.BreathingAtmosphere.PartialPressureO2;
			PressurekPa minimumOxygenPartialPressure = Chemistry.MinimumOxygenPartialPressure;
			string color = ((!(partialPressureO < minimumOxygenPartialPressure)) ? "green" : ((partialPressureO < minimumOxygenPartialPressure * 0.5) ? "red" : "orange"));
			float value = (partialPressureO.ToFloat() * 1000f).RoundToSignificantDigits(3);
			StringManager.AddKeyValueLine(_sb, GameStrings.PartialPressureO2, value.ToStringPrefix(Chemistry.PascalUnit).AsColor(color), "#B4B4B4", indent: true);
			int num = Mathf.RoundToInt(InventoryManager.ParentHuman.OrganLungs.AtmosphericEfficiency * 100f);
			StringManager.AddKeyValueLine(_sb, GameStrings.LungsEfficiency, num.ToStringPercent((num >= 100) ? "green" : (((float)num < 50f) ? "red" : "orange")), "#B4B4B4", indent: true);
			int num2 = Mathf.RoundToInt(InventoryManager.ParentHuman.OrganLungs.DamageState?.GetDamageValue(DamageUpdateType.Toxic).Value ?? 1f);
			int num3 = Mathf.RoundToInt(InventoryManager.ParentHuman.OrganLungs.DamageState?.GetDamageValue(DamageUpdateType.Burn).Value ?? 1f);
			int num4 = Mathf.RoundToInt(InventoryManager.ParentHuman.OrganLungs.DamageState?.GetDamageValue(DamageUpdateType.Oxygen).Value ?? 1f);
			if ((float)num2 > 0f)
			{
				StringManager.AddKeyValueLine(_sb, GameStrings.ToxinDamage, num2.ToStringPercent(((float)num2 > 50f) ? "red" : "orange"), "#B4B4B4", indent: true);
			}
			if ((float)num3 > 0f)
			{
				StringManager.AddKeyValueLine(_sb, GameStrings.BurnDamage, num3.ToStringPercent((num3 > 50) ? "red" : "orange"), "#B4B4B4", indent: true);
			}
			if ((float)num4 > 0f)
			{
				StringManager.AddKeyValueLine(_sb, GameStrings.VacuumDamage, num4.ToStringPercent((num4 > 50) ? "red" : "orange"), "#B4B4B4", indent: true);
			}
		}
		return _sb.ToString();
	}

	public string MedicalHealingTooltip()
	{
		_sb.Clear();
		if (Parent?.ParentSlot?.Parent is CryoTube cryoTube)
		{
			_sb.Append(GameStrings.CryogenicHealingHeading);
			_sb.Newline();
			cryoTube.AddOccupantString(_sb, healthInfo: true, indent: true, alwaysShow: true, organInfo: true);
		}
		else
		{
			_sb.Append(GameStrings.HealingHeading);
			_sb.Newline();
			_sb.Append(GameStrings.MedicalEffectsDescription.AsColor("#B4B4B4"));
		}
		return _sb.ToString();
	}

	public string MedicalStimTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.StimulantHeading);
		_sb.Newline();
		_sb.Append(GameStrings.MedicalEffectsDescription.AsColor("#B4B4B4"));
		return _sb.ToString();
	}

	public string LifeSuspendedTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.LifeSuspendedHeader);
		_sb.Newline();
		_sb.AppendLine(GameStrings.LifeSuspendedDescription.AsColor("#B4B4B4"));
		if ((float)DifficultySetting.Current.HungerRate > 0f)
		{
			_sb.Append(StringManager.Indent).Append(GameStrings.NoNeedToDrink.AsColor("#B4B4B4")).AppendLine();
		}
		if ((float)DifficultySetting.Current.HydrationRate > 0f)
		{
			_sb.Append(StringManager.Indent).Append(GameStrings.NoNeedToEat.AsColor("#B4B4B4")).AppendLine();
		}
		return _sb.ToString();
	}

	public string MedicalStunTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.StunnedHeading);
		_sb.Newline();
		_sb.Append(GameStrings.MedicalEffectsDescription.AsColor("#B4B4B4"));
		return _sb.ToString();
	}

	public string NutritionTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.Nutrition).Append(' ').Append(IsNutritionCritical() ? GameStrings.Critical : GameStrings.Warning);
		if (InventoryManager.ParentHuman.Nutrition <= float.Epsilon)
		{
			_sb.Newline();
			_sb.Append(StringManager.Indent).Append(GameStrings.DamageFromLackOfFood.AsColor("#B4B4B4"));
		}
		return _sb.ToString();
	}

	public string HydrationTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.Hydration).Append(' ').Append(IsHydrationCritical() ? GameStrings.Critical : GameStrings.Warning);
		if (InventoryManager.ParentHuman.Hydration <= float.Epsilon)
		{
			_sb.Newline();
			_sb.Append(StringManager.Indent).Append(GameStrings.DamageFromLackOfWater.AsColor("#B4B4B4"));
		}
		return _sb.ToString();
	}

	public string SanitationTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.SanitationNeed).Append(' ').Append(IsSanitationCritical() ? GameStrings.Critical : GameStrings.Warning);
		_sb.Newline();
		_sb.Append(GameStrings.SanitationDescription.AsColor("#B4B4B4"));
		return _sb.ToString();
	}

	public string SoiledTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.SoiledHeading);
		_sb.Newline();
		_sb.Append(GameStrings.SoiledDescription.AsColor("#B4B4B4"));
		_sb.Newline();
		int value = Mathf.RoundToInt(95f);
		StringManager.AddKeyValueLine(_sb, GameStrings.MovementSpeed, value.ToStringPercent("red"), "#B4B4B4", indent: true);
		return _sb.ToString();
	}

	public string AccelerationTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.UndergoingAcceleration);
		_sb.Newline();
		StringManager.AddKeyValueLine(_sb, GameStrings.GForce, _gForce.ToStringPrefix("G"), "#B4B4B4", indent: true);
		return _sb.ToString();
	}

	public string StunTooltip()
	{
		_sb.Clear();
		_sb.Append(GameStrings.Stun).Append(' ').Append(IsStunCritical() ? GameStrings.Critical : GameStrings.Warning);
		_sb.Newline();
		if (InventoryManager.ParentHuman.SpeciesClass != SpeciesClass.Robot)
		{
			float num = InventoryManager.ParentHuman.OxygenQuality * 100f;
			num.RoundToSignificantDigits(3);
			if (num < 100f)
			{
				StringManager.AddKeyValueLine(_sb, GameStrings.Oxygenation, num.ToStringPercent((num < 50f) ? "red" : "orange"), "#B4B4B4", indent: true);
			}
			PressurekPa obj = InventoryManager.ParentHuman.BreathingAtmosphere?.PartialPressureNitrousOxide ?? PressurekPa.Zero;
			PressurekPa safeN2OPartialPressure = InventoryManager.ParentHuman.GetSafeN2OPartialPressure();
			float num2 = (obj / safeN2OPartialPressure).ToFloat();
			string color = ((num2 > 1f) ? "red" : "orange");
			float value = (obj * 1000.0).ToFloat().RoundToSignificantDigits(3);
			if (num2 > 0f)
			{
				StringManager.AddKeyValueLine(_sb, GameStrings.PartialPressureN2O, value.ToStringPrefix(Chemistry.PascalUnit).AsColor(color), "#B4B4B4", indent: true);
			}
		}
		else if (!InventoryManager.ParentHuman.RobotBattery)
		{
			_sb.Append(StringManager.Indent).Append(GameStrings.RobotHasNoBattery.AsColor("#B4B4B4"));
		}
		else if (InventoryManager.ParentHuman.RobotBattery.IsEmpty)
		{
			_sb.Append(StringManager.Indent).Append(GameStrings.RobotBatteryNoCharge.AsColor("#B4B4B4"));
		}
		return _sb.ToString();
	}

	public string RefreshedTooltip()
	{
		_sb.Clear();
		_sb.AppendLine(GameStrings.Refreshed);
		if (InventoryManager.ParentHuman.Hygiene > 1f)
		{
			int value = Mathf.RoundToInt(InventoryManager.ParentHuman.Hygiene * 100f);
			StringManager.AddKeyValueLine(_sb, GameStrings.Hygiene, value.ToStringPercent("green"), "#B4B4B4", indent: true);
			int value2 = Mathf.RoundToInt(104.99999f);
			StringManager.AddKeyValueLine(_sb, GameStrings.MovementSpeed, value2.ToStringPercent("green"), "#B4B4B4", indent: true);
			float num = ((InventoryManager.ParentHuman.Hygiene > 1f) ? ((float)DifficultySetting.Current.GoodHygieneToolSpeedMultiplier) : 1f);
			if (num > 1f || num < 1f)
			{
				int value3 = Mathf.RoundToInt(num * 100f);
				string color = ((num > 1f) ? "green" : "red");
				StringManager.AddKeyValue(_sb, GameStrings.ToolUseSpeed, value3.ToStringPercent(color), "#B4B4B4", indent: true);
			}
		}
		return _sb.ToString();
	}
}
