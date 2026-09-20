using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using CharacterCustomisation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PlayerStateWindow : UserInterfaceBase
{
	public RectTransform JetpackTransform;

	public UserInterfaceBase InfoJetpack;

	public UserInterfaceBase InfoInternal;

	public UserInterfaceBase InfoExternal;

	public UserInterfaceBase InfoHealth;

	public UserInterfaceBase InfoJetpackPressureDeltaPanel;

	public UserInterfaceBase InfoJetpackPowerPanel;

	public static PlayerStateWindow Instance;

	public TextMeshProUGUI InfoJetpackThrust;

	public TextMeshProUGUI InfoJetpackPressureDeltaText;

	public TextMeshProUGUI InfoJetpackPowerText;

	public TextMeshProUGUI InfoExternalVelocity;

	public TextMeshProUGUI InfoExternalDays;

	public TextMeshProUGUI InfoExternalPressure;

	public TextMeshProUGUI InfoExternalTemperature;

	public TextMeshProUGUI InfoInternalPressure;

	public TextMeshProUGUI InfoInternalPressureSetting;

	public TextMeshProUGUI InfoInternalTemperature;

	public TextMeshProUGUI InfoInternalTemperatureSetting;

	public RectTransform InfoExternalPressureRampBack;

	public RectTransform InfoExternalPressureRampFront;

	public RectTransform InfoInternalPressureRampBack;

	public RectTransform InfoInternalPressureRampFront;

	public AnimationCurve PressureCurve;

	public AnimationCurve TemperatureCurve;

	public Gradient PressureGradient = new Gradient();

	public Gradient TemperatureGradient = new Gradient();

	public InventoryManager InventoryManager;

	[Header("External")]
	public UserInterfaceBase HeaderExternal;

	public TextMeshProUGUI HeaderExternalText;

	[Header("WellBeing Percentages")]
	public TextMeshProUGUI HealthPercentage;

	public TextMeshProUGUI HungerPercentage;

	public TextMeshProUGUI HydrationPercentage;

	public TextMeshProUGUI WastePercentage;

	public TextMeshProUGUI ToxinPercentage;

	public TextMeshProUGUI CognitionPercentage;

	public TextMeshProUGUI NavigationText;

	public TextMeshProUGUI MoodText;

	public TextMeshProUGUI HygieneText;

	public TextMeshProUGUI FoodQualityText;

	[Header("PanelGameObjects")]
	public GameObject VitalsObject;

	public GameObject HealthPercentageObject;

	public GameObject HungerPercentageObject;

	public GameObject HydrationPercentageObject;

	public GameObject WastePercentageObject;

	public GameObject ToxinPercentageObject;

	public GameObject PersonDamageObject;

	public GameObject CognitionPercentageObject;

	[Header("InteractableNotifiers")]
	public ImageToggle JetPackImageToggle;

	public ImageToggle LightImageToggle;

	public ImageToggle HelmetImageToggle;

	public GameObject HelmetPanel;

	public GameObject LightPanel;

	[Header("Pressure/Temperature Icons")]
	public ImageToggle ExternalTemperatureToggle;

	public ImageToggle ExternalPressureToggle;

	public ImageToggle InternalTemperatureToggle;

	public ImageToggle InternalPressureToggle;

	public GameObject IconJetpackPressureDeltaLow;

	public GameObject IconJetpackPowerLow;

	[Header("Food Icons")]
	public ImageToggle FoodQualityToggle;

	[Header("Misc")]
	public VerticalLayoutGroup InfoInternalVerticalLayoutGroup;

	public Canvas InfoInternalCanvas;

	public float _tempExternal;

	public TemperatureKelvin _tempExternalK;

	public float _tempInternal;

	public TemperatureKelvin _tempInternalK;

	public PressurekPa _pressureExternal;

	public PressurekPa _pressureInternal;

	private float _airTankPressure;

	private float _wasteTankPressure;

	private float _propellentPressure;

	private float _lifeSupportCapacity;

	private StateInstance _externalVelocityState;

	private StateInstance _externalPressureState;

	private StateInstance _externalTemperatureState;

	private StateInstance _jetpackThrustState;

	private StateInstance _jetpackPressureDeltaState;

	private StateInstance _jetpackPowerState;

	private StateInstance _internalPressureState;

	private StateInstance _internalTemperatureState;

	private StateInstance _internalPressureSettingState;

	private StateInstance _internalTemperatureSettingState;

	private StateInstance _compassState;

	private StateInstance _hungerState;

	private StateInstance _hydrationState;

	private StateInstance _wasteState;

	private StateInstance _healthState;

	private StateInstance _toxinState;

	private StateInstance _cognitionState;

	private StateInstance _daysPassed;

	private StateInstance _exteriorType;

	private StateInstance _hygiene;

	private StateInstance _mood;

	private StateInstance _foodQuality;

	public static readonly TemperatureKelvin ShowTooHotLimit = new TemperatureKelvin(323.15);

	public static readonly TemperatureKelvin ShowTooColdLimit = new TemperatureKelvin(273.15);

	public static readonly PressurekPa ShowPressureHighLimit = new PressurekPa(260.0);

	public static readonly PressurekPa ShowPressureLowLimit = new PressurekPa(20.0);

	private const float SHOW_STUN_PERCENTAGE = 0.5f;

	private const float RATIO_TO_PERCENTAGE = 100f;

	public Human Parent => InventoryManager.ParentHuman;

	public void Awake()
	{
		StringManager.Initialize();
		if (Instance == null)
		{
			Instance = this;
		}
		_externalPressureState = new StateInstance(InfoExternalPressure);
		_externalPressureState.OnChanged += delegate
		{
			float num = PressureCurve.Evaluate(Mathf.Clamp(_pressureExternal.ToFloat(), 0f, PressureCurve.keys[PressureCurve.length - 1].time));
			InfoExternalPressureRampFront.sizeDelta = new Vector2(InfoExternalPressureRampBack.rect.width * num, InfoExternalPressureRampBack.rect.height);
		};
		_externalPressureState.ChangeEvents();
		_externalTemperatureState = new StateInstance(InfoExternalTemperature, Unit.DegreesCelcius);
		_externalTemperatureState.ChangeEvents();
		_internalPressureState = new StateInstance(InfoInternalPressure);
		_internalPressureState.OnChanged += delegate
		{
			float num = PressureCurve.Evaluate(_pressureInternal.ToFloat());
			InfoInternalPressureRampFront.sizeDelta = new Vector2(InfoExternalPressureRampBack.rect.width * num, InfoInternalPressureRampBack.rect.height);
		};
		_internalPressureState.ChangeEvents();
		_internalTemperatureState = new StateInstance(InfoInternalTemperature, Unit.DegreesCelcius);
		_internalTemperatureState.ChangeEvents();
		_externalVelocityState = new StateInstance(InfoExternalVelocity);
		_jetpackThrustState = new StateInstance(InfoJetpackThrust);
		_jetpackPressureDeltaState = new StateInstance(InfoJetpackPressureDeltaText);
		_jetpackPowerState = new StateInstance(InfoJetpackPowerText);
		_internalPressureSettingState = new StateInstance(InfoInternalPressureSetting);
		_internalTemperatureSettingState = new StateInstance(InfoInternalTemperatureSetting);
		_compassState = new StateInstance(NavigationText);
		_hungerState = new StateInstance(HungerPercentage);
		_hydrationState = new StateInstance(HydrationPercentage);
		_wasteState = new StateInstance(WastePercentage);
		_healthState = new StateInstance(HealthPercentage);
		_toxinState = new StateInstance(ToxinPercentage);
		_cognitionState = new StateInstance(CognitionPercentage);
		_daysPassed = new StateInstance(InfoExternalDays);
		_hygiene = new StateInstance(HygieneText);
		_mood = new StateInstance(MoodText);
		_foodQuality = new StateInstance(FoodQualityText);
		_exteriorType = new StateInstance(HeaderExternalText);
	}

	public void UpdateDaysPastText()
	{
		_daysPassed.UpdateText(GameStrings.DaysPassedHudText.AsString(InventoryManager.Parent?.DaysLived.ToString()));
	}

	public static void OnNextDay()
	{
		Instance?.UpdateDaysPastText();
	}

	private void HandleInternalExternalIcons()
	{
		if (_tempExternalK > ShowTooHotLimit)
		{
			ExternalTemperatureToggle.SetImage(1);
			ExternalTemperatureToggle.HideImage(isHidden: false);
		}
		else if (_tempExternalK < ShowTooColdLimit)
		{
			ExternalTemperatureToggle.SetImage(0);
			ExternalTemperatureToggle.HideImage(isHidden: false);
		}
		else
		{
			ExternalTemperatureToggle.HideImage();
		}
		if (_pressureExternal > ShowPressureHighLimit)
		{
			ExternalPressureToggle.SetImage(1);
			ExternalPressureToggle.HideImage(isHidden: false);
		}
		else if (_pressureExternal < ShowPressureLowLimit)
		{
			ExternalPressureToggle.SetImage(0);
			ExternalPressureToggle.HideImage(isHidden: false);
		}
		else
		{
			ExternalPressureToggle.HideImage();
		}
		if (_pressureInternal > ShowPressureHighLimit)
		{
			InternalPressureToggle.SetImage(1);
			InternalPressureToggle.HideImage(isHidden: false);
		}
		else if (_pressureInternal < ShowPressureLowLimit)
		{
			InternalPressureToggle.SetImage(0);
			InternalPressureToggle.HideImage(isHidden: false);
		}
		else
		{
			InternalPressureToggle.HideImage();
		}
		if (_tempInternalK > ShowTooHotLimit)
		{
			InternalTemperatureToggle.SetImage(1);
			InternalTemperatureToggle.HideImage(isHidden: false);
		}
		else if (_tempInternalK < ShowTooColdLimit)
		{
			InternalTemperatureToggle.SetImage(0);
			InternalTemperatureToggle.HideImage(isHidden: false);
		}
		else
		{
			InternalTemperatureToggle.HideImage();
		}
	}

	private int GetFoodQualityIndex()
	{
		if (!IsVisible || !Parent)
		{
			return 0;
		}
		float foodQuality = Parent.FoodQuality;
		if (foodQuality < 0.7f)
		{
			if (foodQuality < 0.45f)
			{
				return 0;
			}
			return 1;
		}
		if (foodQuality < 0.9f)
		{
			return 2;
		}
		return 3;
	}

	private void Update()
	{
		if (!IsVisible || !Parent)
		{
			return;
		}
		bool flag = Parent.HasInternals && Parent.InternalsOn;
		Canvas infoInternalCanvas = InfoInternalCanvas;
		bool flag2 = (InfoInternalVerticalLayoutGroup.enabled = flag);
		infoInternalCanvas.enabled = flag2;
		FoodQualityToggle.SetImage(GetFoodQualityIndex());
		if (!flag)
		{
			if (Parent.HeadAsSpaceHelmet == null && HelmetPanel.activeInHierarchy)
			{
				HelmetPanel.SetActive(value: false);
			}
			HelmetImageToggle.SetImage(1);
		}
		else
		{
			if (!HelmetPanel.activeInHierarchy)
			{
				HelmetPanel.SetActive(value: true);
			}
			HelmetImageToggle.SetImage(0);
		}
		UpdateJetpackPanels();
		if (InfoExternalVelocity.enabled)
		{
			_externalVelocityState.UpdateText(Parent.VelocityMagnitude);
		}
		if ((bool)InfoExternalPressure && InfoExternalPressure.enabled)
		{
			_pressureExternal = RocketMath.Lerp(_pressureExternal, (Parent.WorldAtmosphere != null && Parent.WorldAtmosphere.IsValid()) ? Parent.WorldAtmosphere.PressureGassesAndLiquids : PressurekPa.Zero, (double)Time.deltaTime * 3.0);
			_externalPressureState.UpdateText(_pressureExternal.ToFloat());
		}
		if (InfoExternalTemperature.enabled)
		{
			_tempExternalK = ((Parent.WorldAtmosphere != null && Parent.WorldAtmosphere.IsValid()) ? Parent.WorldAtmosphere.Temperature : TemperatureKelvin.Zero);
			Thing rootParent = Parent.RootParent;
			if (rootParent is CryoTube { WillRevive: not false } && rootParent.Powered && !rootParent.IsOpen)
			{
				_tempExternalK = new TemperatureKelvin(268.15);
			}
			_tempExternal = (_tempExternalK - Chemistry.Temperature.ZeroDegrees).ToFloat();
			_externalTemperatureState.UpdateText(_tempExternal);
		}
		if (InfoInternalCanvas.enabled && InfoInternalTemperature.enabled)
		{
			_tempInternalK = ((Parent.BreathingAtmosphere != null && Parent.BreathingAtmosphere.IsValid()) ? Parent.BreathingAtmosphere.Temperature : TemperatureKelvin.Zero);
			_tempInternal = (_tempInternalK - Chemistry.Temperature.ZeroDegrees).ToFloat();
			_internalTemperatureState.UpdateText(_tempInternal);
			_internalTemperatureSettingState.UpdateText(Parent.Suit?.AsThing ? (Parent.Suit.OutputTemperature - Chemistry.Temperature.ZeroDegrees).ToFloat() : 0f);
		}
		if (InfoInternalCanvas.enabled && InfoInternalPressure.enabled)
		{
			_pressureInternal = RocketMath.Lerp(_pressureInternal, (Parent.BreathingAtmosphere != null && Parent.BreathingAtmosphere.IsValid()) ? Parent.BreathingAtmosphere.PressureGassesAndLiquids : PressurekPa.Zero, Time.deltaTime * 3f);
			_internalPressureState.UpdateText(_pressureInternal.ToFloat());
			_internalPressureSettingState.UpdateText(Parent.Suit?.AsThing ? Parent.Suit.OutputSetting : 0f);
		}
		_compassState.UpdateText((int)((Parent.EntityRotation.eulerAngles.y + 180f) % 360f));
		_hungerState.UpdateText((int)(Parent.NutritionRatio * 100f));
		_hydrationState.UpdateText((int)(Parent.HydrationRatio * 100f));
		if (Parent.SpeciesClass != SpeciesClass.Robot && Parent.SanitationRatio > 0.25f)
		{
			_wasteState.UpdateText((int)(Parent.SanitationRatio * 100f));
			WastePercentageObject.SetActive(value: true);
		}
		else
		{
			WastePercentageObject.SetActive(value: false);
		}
		_mood.UpdateText((int)(Parent.MoodRatio * 100f));
		_hygiene.UpdateText((int)(Parent.HygieneRatio * 100f));
		_foodQuality.UpdateText((int)(Parent.FoodQualityRatio * 100f));
		_exteriorType.UpdateText(EnumCollections.ExteriorStates.GetName(Parent.GetExteriorState()));
		if (Parent.SpeciesClass == SpeciesClass.Robot)
		{
			HungerPercentageObject.SetActive(value: false);
			HydrationPercentageObject.SetActive(value: false);
			WastePercentageObject.SetActive(value: false);
			if (!CognitionPercentageObject.activeSelf)
			{
				VitalsObject.SetActive(value: false);
			}
		}
		else
		{
			if (!VitalsObject.activeInHierarchy)
			{
				VitalsObject.SetActive(value: true);
			}
			HungerPercentageObject.SetActive(value: true);
			HydrationPercentageObject.SetActive(value: true);
		}
		if (Parent.IsDamaged())
		{
			HealthPercentageObject.SetActive(value: true);
			PersonDamageObject.SetActive(value: true);
			_healthState.UpdateText((int)(100f - Parent.DamageState.TotalRatio * 100f));
		}
		else
		{
			HealthPercentageObject.SetActive(value: false);
			PersonDamageObject.SetActive(value: false);
		}
		if (Parent.DamageState.Toxic > 0.5f)
		{
			ToxinPercentageObject.SetActive(value: true);
			_toxinState.UpdateText((int)Parent.DamageState.Toxic);
		}
		else
		{
			ToxinPercentageObject.SetActive(value: false);
		}
		if (Parent.DamageState.Stun > 0.5f)
		{
			CognitionPercentageObject.SetActive(value: true);
			VitalsObject.SetActive(value: true);
			_cognitionState.UpdateText((int)Parent.DamageState.Stun);
		}
		else
		{
			CognitionPercentageObject.SetActive(value: false);
		}
		HandleInternalExternalIcons();
	}

	private void UpdateJetpackPanels()
	{
		Jetpack jetpack = Parent.BackpackSlot.Get<Jetpack>();
		if (!jetpack)
		{
			if (InfoJetpack.IsVisible)
			{
				InfoJetpack.SetVisible(isVisble: false);
			}
			return;
		}
		if (!InfoJetpack.IsVisible)
		{
			InfoJetpack.SetVisible(isVisble: true);
		}
		if (InfoJetpackThrust.enabled && Parent.MovementController.HeightEfficiency < 1f)
		{
			_jetpackThrustState.UpdateText(jetpack.OutputSetting * Parent.MovementController.HeightEfficiency * 100f);
		}
		else
		{
			_jetpackThrustState.UpdateText(jetpack.OutputSetting * 100f);
		}
		if (jetpack.IsGasPowered)
		{
			ShowGasJetpackUI(jetpack);
			HideBatteryJetpackUI();
		}
		else if (jetpack.IsBatteryPowered)
		{
			HideGasJetpackUI();
			ShowBatteryJetpackUI(jetpack);
		}
		else
		{
			HideGasJetpackUI();
			HideBatteryJetpackUI();
		}
	}

	private void HideGasJetpackUI()
	{
		InfoJetpackPressureDeltaPanel.SetVisible(isVisble: false);
		IconJetpackPressureDeltaLow.SetActive(value: false);
	}

	private void ShowGasJetpackUI(Jetpack jetpack)
	{
		if (jetpack.PropellentSlot.Contains<GasCanister>(out var occupant) && occupant.InternalAtmosphere != null)
		{
			InfoJetpackPressureDeltaPanel.SetVisible(isVisble: true);
			PressurekPa pressurekPa = occupant.InternalAtmosphere.PressureGassesAndLiquids - (jetpack.WorldAtmosphere?.PressureGasses ?? PressurekPa.Zero);
			_jetpackPressureDeltaState.UpdateText(pressurekPa.ToFloat());
			IconJetpackPressureDeltaLow.SetActive(pressurekPa < Jetpack.LowPropellantDeltaLevel);
		}
		else
		{
			HideGasJetpackUI();
		}
	}

	private void HideBatteryJetpackUI()
	{
		InfoJetpackPowerPanel.SetVisible(isVisble: false);
		IconJetpackPowerLow.SetActive(value: false);
	}

	private void ShowBatteryJetpackUI(Jetpack jetpack)
	{
		if (jetpack is JetpackElectric { Battery: var battery } jetpackElectric)
		{
			if ((bool)battery)
			{
				InfoJetpackPowerPanel.SetVisible(isVisble: true);
				_jetpackPowerState.UpdateText(battery.CurrentPowerPercentage);
				IconJetpackPowerLow.SetActive(jetpackElectric.PowerLow);
			}
			else
			{
				HideBatteryJetpackUI();
			}
		}
	}
}
