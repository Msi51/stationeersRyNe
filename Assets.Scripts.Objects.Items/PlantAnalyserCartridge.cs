using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Genetics;
using Assets.Scripts.Util;
using Genetics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class PlantAnalyserCartridge : Cartridge
{
	public class GasRequirementInfo
	{
		public string GasName;

		public string PercentRequired;

		public string PercentPresent;

		public GasRequirementInfo(Plant plant, GasQuantityRatioData inhaledGas)
		{
			if ((object)plant != null && inhaledGas != null)
			{
				GasName = Localization.GetName(inhaledGas.Type);
				PercentRequired = StringManager.Get(inhaledGas.Ratio * 100f) + "%";
				if (plant.BreathingAtmosphere != null)
				{
					string text = ((plant.BreathingAtmosphere.GetGasTypeRatio(inhaledGas.Type) > inhaledGas.Ratio) ? "green" : "red");
					PercentPresent = "<color=" + text + ">" + StringManager.Get(plant.BreathingAtmosphere.GetGasTypeRatio(inhaledGas.Type) * 100f) + "%</color>";
				}
			}
		}

		public static void UpdateRequirements(Plant plant, ref List<GasRequirementInfo> gasRequirementInfos)
		{
			gasRequirementInfos.Clear();
			if ((object)plant == null)
			{
				return;
			}
			foreach (GasQuantityRatioData inhaledGase in plant.lifeRequirements.Data.InhaledGases)
			{
				gasRequirementInfos.Add(new GasRequirementInfo(plant, inhaledGase));
			}
		}
	}

	public class ToxicGasInfo
	{
		public string GasName;

		public string PartialPressureLimit;

		public string PartialPressurePresent;

		public ToxicGasInfo(Plant plant, Chemistry.GasType gasType)
		{
			GasName = Localization.GetName(gasType);
			PartialPressureLimit = ((float)plant.lifeRequirements.UndesiredGasResistance * 1000f).ToStringPrefix(Chemistry.PascalUnit);
			if (plant.BreathingAtmosphere != null)
			{
				string text = ((plant.BreathingAtmosphere.PartialPressure(gasType).ToFloat() < (float)plant.lifeRequirements.UndesiredGasResistance) ? "green" : "red");
				PartialPressurePresent = "<color=" + text + ">" + (plant.BreathingAtmosphere.PartialPressure(gasType) * 1000.0).ToFloat().ToStringPrefix(Chemistry.PascalUnit) + "</color>";
			}
		}

		public static void UpdateToxicGasInfo(Plant plant, ref List<ToxicGasInfo> toxicGasInfos)
		{
			toxicGasInfos.Clear();
			foreach (GasPressureData harmfulGase in plant.lifeRequirements.Data.HarmfulGases)
			{
				toxicGasInfos.Add(new ToxicGasInfo(plant, harmfulGase.Type));
			}
		}
	}

	private string _scannedPlantName = string.Empty;

	private string _growthEfficiency = string.Empty;

	private string _breathingEfficiency = string.Empty;

	private List<GasRequirementInfo> InhaledRequirementInfos = new List<GasRequirementInfo>();

	private List<ToxicGasInfo> ToxicGasInfos = new List<ToxicGasInfo>();

	[SerializeField]
	private List<GasRequirementText> InhaledRequirementTexts = new List<GasRequirementText>();

	[SerializeField]
	private List<ToxicGasText> ToxicGasTexts = new List<ToxicGasText>();

	private string _temperatureEfficiency = string.Empty;

	private string _minIdealTemp = string.Empty;

	private string _maxIdealTemp = string.Empty;

	private string _currentTemp = string.Empty;

	private string _pressureEfficiency = string.Empty;

	private string _minIdealPressure = string.Empty;

	private string _maxIdealPressure = string.Empty;

	private string _currentPressure = string.Empty;

	private string _lightEfficiency = string.Empty;

	private string _lightIntensity = string.Empty;

	private string _lightingStress = string.Empty;

	private string _lightDeficiency = string.Empty;

	private string _darknessDeficiency = string.Empty;

	private string _hydrationEfficiency = string.Empty;

	[Header("List Elements")]
	[SerializeField]
	private TextMeshProUGUI growthEfficiencyValue;

	[SerializeField]
	private TextMeshProUGUI breathingEfficiencyValue;

	[SerializeField]
	private TextMeshProUGUI temperatureEfficiencyValue;

	[SerializeField]
	private TextMeshProUGUI minIdealTemperatureValue;

	[SerializeField]
	private TextMeshProUGUI maxIdealTemperatureValue;

	[SerializeField]
	private TextMeshProUGUI currentTemperatureValue;

	[SerializeField]
	private TextMeshProUGUI pressureEfficiencyValue;

	[SerializeField]
	private TextMeshProUGUI minIdealPressureValue;

	[SerializeField]
	private TextMeshProUGUI maxIdealPressureValue;

	[SerializeField]
	private TextMeshProUGUI currentPressureValue;

	[SerializeField]
	private TextMeshProUGUI lightEfficiencyValue;

	[SerializeField]
	private TextMeshProUGUI lightIntensityValue;

	[SerializeField]
	private TextMeshProUGUI lightingStressValue;

	[SerializeField]
	private TextMeshProUGUI lightReceivedValue;

	[SerializeField]
	private TextMeshProUGUI darknessReceivedValue;

	[SerializeField]
	private TextMeshProUGUI hydrationEfficiencyValue;

	[Space(15f)]
	[SerializeField]
	private GridLayoutGroup _dynamicGrid;

	[SerializeField]
	private GameObject entryPrefab;

	[SerializeField]
	private Transform contentParent;

	public Plant ScannedPlant { get; private set; }

	private Plant GetScannedPlant()
	{
		if (!RootParent || !RootParent.HasAuthority)
		{
			return null;
		}
		if (!InPlayerHand())
		{
			return null;
		}
		Collider cursorTargetCollider = CursorManager.Instance.CursorTargetCollider;
		Thing cursorThing = CursorManager.CursorThing;
		if (!(cursorThing is HydroponicTray hydroponicTray))
		{
			if (cursorThing is HydroponicsTrayDevice hydroponicsTrayDevice)
			{
				return hydroponicsTrayDevice.Plant;
			}
			Slot slot = cursorThing?.GetSlot(cursorTargetCollider);
			if (slot != null && slot.Occupant is Plant result)
			{
				return result;
			}
			return (cursorThing ? cursorThing : RootParent) as Plant;
		}
		return hydroponicTray.Plant;
	}

	private void ClearElements()
	{
		_growthEfficiency = string.Empty;
		_breathingEfficiency = string.Empty;
		InhaledRequirementInfos.Clear();
		ToxicGasInfos.Clear();
		_temperatureEfficiency = string.Empty;
		_minIdealTemp = string.Empty;
		_maxIdealTemp = string.Empty;
		_currentTemp = string.Empty;
		_pressureEfficiency = string.Empty;
		_minIdealPressure = string.Empty;
		_maxIdealPressure = string.Empty;
		_currentPressure = string.Empty;
		_lightEfficiency = string.Empty;
		_lightIntensity = string.Empty;
		_lightingStress = string.Empty;
		_lightDeficiency = string.Empty;
		_darknessDeficiency = string.Empty;
		_hydrationEfficiency = string.Empty;
	}

	private void GenerateInfoStrings()
	{
		ScannedPlant = GetScannedPlant();
		_scannedPlantName = (ScannedPlant ? ScannedPlant.DisplayName.ToUpper() : GameStrings.NotApplicableString.DisplayString);
		if (!ScannedPlant || ScannedPlant.BreathingAtmosphere == null || ScannedPlant.IsBeingDestroyed)
		{
			ClearElements();
			return;
		}
		if (ScannedPlant.IsDead)
		{
			ClearElements();
			_scannedPlantName = "<color=red>Dead</color> " + _scannedPlantName;
			return;
		}
		_growthEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.GrowthEfficiencyPercent, ValueDisplay.Unit.Percentage);
		_breathingEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.BreathingEfficiencyPercent, ValueDisplay.Unit.Percentage);
		GasRequirementInfo.UpdateRequirements(ScannedPlant, ref InhaledRequirementInfos);
		ToxicGasInfo.UpdateToxicGasInfo(ScannedPlant, ref ToxicGasInfos);
		_temperatureEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.TemperatureEfficiencyPercent, ValueDisplay.Unit.Percentage);
		_minIdealTemp = ValueDisplay.GetUnitValue(ScannedPlant.lifeRequirements.GrowTemperatureC.IdealMin(), ValueDisplay.Unit.TemperatureC);
		_maxIdealTemp = ValueDisplay.GetUnitValue(ScannedPlant.lifeRequirements.GrowTemperatureC.IdealMax(), ValueDisplay.Unit.TemperatureC);
		_currentTemp = ValueDisplay.GetUnitValue(ScannedPlant.BreathingAtmosphere.Temperature.ToFloat(), ValueDisplay.Unit.TemperatureKToC);
		_pressureEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.PressureEfficiencyPercent, ValueDisplay.Unit.Percentage);
		_minIdealPressure = ValueDisplay.GetUnitValue(ScannedPlant.lifeRequirements.GrowPressure.IdealMin(), ValueDisplay.Unit.PressureKpa);
		_maxIdealPressure = ValueDisplay.GetUnitValue(ScannedPlant.lifeRequirements.GrowPressure.IdealMax(), ValueDisplay.Unit.PressureKpa);
		_currentPressure = (ScannedPlant.BreathingAtmosphere.PressureGassesAndLiquids * 1000.0).ToFloat().ToStringPrefix(Chemistry.PascalUnit);
		_lightEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.LightEfficiencyPercent, ValueDisplay.Unit.Percentage);
		_lightIntensity = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.CurrentLightExposurePercent, ValueDisplay.Unit.Percentage);
		_lightingStress = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.LightStressPercent, ValueDisplay.Unit.Percentage);
		_lightDeficiency = ((ScannedPlant.PlantStatus.LightPercent <= 0) ? ("<color=red>" + GameStrings.LightDeficient.DisplayString + "</color>") : GameStrings.NoDeficiency.DisplayString);
		_darknessDeficiency = ((ScannedPlant.PlantStatus.DarknessPercent <= 0) ? ("<color=red>" + GameStrings.DarknessDeficient.DisplayString + "</color>") : GameStrings.NoDeficiency.DisplayString);
		_hydrationEfficiency = ValueDisplay.GetUnitValue((int)ScannedPlant.PlantStatus.HydrationEfficiencyPercent, ValueDisplay.Unit.Percentage);
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		GenerateInfoStrings();
		SelectedTitle.text = _scannedPlantName;
		for (int num = InhaledRequirementTexts.Count - 1; num >= 0; num--)
		{
			if (InhaledRequirementInfos.Count <= num)
			{
				InhaledRequirementTexts[num].gameObject.SetActive(value: false);
			}
			else
			{
				InhaledRequirementTexts[num].gameObject.SetActive(value: true);
				InhaledRequirementTexts[num].Apply(InhaledRequirementInfos[num]);
			}
		}
		for (int num2 = ToxicGasTexts.Count - 1; num2 >= 0; num2--)
		{
			if (ToxicGasInfos.Count <= num2)
			{
				ToxicGasTexts[num2].gameObject.SetActive(value: false);
			}
			else
			{
				ToxicGasTexts[num2].gameObject.SetActive(value: true);
				ToxicGasTexts[num2].Apply(ToxicGasInfos[num2]);
			}
		}
		growthEfficiencyValue.text = _growthEfficiency;
		breathingEfficiencyValue.text = _breathingEfficiency;
		temperatureEfficiencyValue.text = _temperatureEfficiency;
		minIdealTemperatureValue.text = _minIdealTemp;
		maxIdealTemperatureValue.text = _maxIdealTemp;
		currentTemperatureValue.text = _currentTemp;
		pressureEfficiencyValue.text = _pressureEfficiency;
		minIdealPressureValue.text = _minIdealPressure;
		maxIdealPressureValue.text = _maxIdealPressure;
		currentPressureValue.text = _currentPressure;
		lightEfficiencyValue.text = _lightEfficiency;
		lightIntensityValue.text = _lightIntensity;
		lightingStressValue.text = _lightingStress;
		lightReceivedValue.text = _lightDeficiency;
		darknessReceivedValue.text = _darknessDeficiency;
		hydrationEfficiencyValue.text = _hydrationEfficiency;
		_scrollPanel.SetContentHeight(_dynamicGrid.preferredHeight);
	}
}
