using System.Collections.Generic;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Genetics;

public class PanelPlantGenetics : InputWindowBase, IModal
{
	public static PanelPlantGenetics Instance;

	[SerializeField]
	private Image _background;

	[SerializeField]
	private TextMeshProUGUI plantName;

	[Space(15f)]
	[SerializeField]
	private GeneInfo _geneInfoPrefab;

	[SerializeField]
	private MultiGeneInfo _multiGeneInfoPrefab;

	[Space(15f)]
	[SerializeField]
	private Transform _geneInfoParent;

	[Space(15f)]
	[SerializeField]
	private AnimationCurve _temperatureScale;

	[SerializeField]
	private AnimationCurve _pressureScale;

	private List<GeneInfo> _geneInfo = new List<GeneInfo>();

	private List<MultiGeneInfo> _multiGeneInfo = new List<MultiGeneInfo>();

	private bool _isOpaque;

	private int _currentGeneInfoIndex;

	private int _currentMultiGeneInfoIndex;

	public bool UnlockCursor => true;

	public override void Initialize()
	{
		base.Initialize();
		_isOpaque = true;
		Instance = this;
		SetVisible(isVisble: false);
	}

	public void Show(PlantSample plantSample)
	{
		CursorManager.Instance.BlockCursorRaycast = true;
		MouseModeController.AddModal(this);
		_currentGeneInfoIndex = 0;
		_currentMultiGeneInfoIndex = 0;
		plantName.text = plantSample.PlantName;
		_temperatureScale.ClearKeys();
		_temperatureScale.AddKey(plantSample.MinGrowTemperatureC.MaxValue, 0f);
		_temperatureScale.AddKey(plantSample.MaxGrowTemperatureC.MaxValue, 1f);
		AddMultiGeneInfo(GameStrings.GrowthTemperatureRange.DisplayString, plantSample.MinGrowTemperatureC.MaxValue, plantSample.MaxGrowTemperatureC.MaxValue, plantSample.MinGrowTemperatureC.CurrentValue, plantSample.MaxGrowTemperatureC.CurrentValue, plantSample.MinIdealGrowTemperatureC.CurrentValue, plantSample.MaxIdealGrowTemperatureC.CurrentValue, _temperatureScale, ValueDisplay.Unit.TemperatureC);
		AddMultiGeneInfo(GameStrings.GrowthPressureRange.DisplayString, plantSample.MinGrowPressure.MaxValue, plantSample.MaxGrowPressure.MaxValue, plantSample.MinGrowPressure.CurrentValue, plantSample.MaxGrowPressure.CurrentValue, plantSample.MinIdealGrowPressure.CurrentValue, plantSample.MaxIdealGrowPressure.CurrentValue, _pressureScale, ValueDisplay.Unit.PressureKpa);
		AddGeneInfo(GameStrings.GrowthSpeedMultiplier.DisplayString, plantSample.GrowthSpeedMultiplier, ValueDisplay.Unit.RatioToPercentage);
		Achievements.AssessGottaGrowFast(plantSample.GrowthSpeedMultiplier.CurrentValue, plantSample.GrowthSpeedMultiplier.MaxValue);
		AddGeneInfo(GameStrings.LightPerDay.DisplayString, plantSample.LightPerDay, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.DarknessPerDay.DisplayString, plantSample.DarknessPerDay, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.GasProduction.DisplayString, plantSample.GasProduction, ValueDisplay.Unit.RatioToPercentage);
		AddGeneInfo(GameStrings.WaterUsage.DisplayString, plantSample.WaterUsage, ValueDisplay.Unit.RatioToPercentage);
		AddGeneInfo(GameStrings.UndesiredGasResistance.DisplayString, plantSample.UndesiredGasResistance, ValueDisplay.Unit.PressureKpa);
		AddGeneInfo(GameStrings.TimeUntilDehydrationDamage.DisplayString, plantSample.TimeUntilDehydrationDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilUndesiredGasDamage.DisplayString, plantSample.TimeUntilUndesiredGasDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilFrozenDamage.DisplayString, plantSample.TimeUntilFrozenDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilOverheatDamage.DisplayString, plantSample.TimeUntilOverHeatedDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilSuffocateDamage.DisplayString, plantSample.TimeUntilSuffocatedDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilLowPressureDamage.DisplayString, plantSample.TimeUntilLowPressureDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilHighPressureDamage.DisplayString, plantSample.TimeUntilHighPressureDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilLightDamage.DisplayString, plantSample.TimeUntilLightDamage, ValueDisplay.Unit.Time);
		AddGeneInfo(GameStrings.TimeUntilDarknessDamage.DisplayString, plantSample.TimeUntilDarknessDamage, ValueDisplay.Unit.Time);
		SetVisible(isVisble: true);
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			CloseButtonClicked();
		}
	}

	private void AddGeneInfo(string name, RequirementWrapper wrapper, ValueDisplay.Unit unit)
	{
		GeneInfo geneInfo;
		if (_geneInfo.Count > _currentGeneInfoIndex)
		{
			geneInfo = _geneInfo[_currentGeneInfoIndex];
		}
		else
		{
			geneInfo = Object.Instantiate(_geneInfoPrefab, _geneInfoParent);
			_geneInfo.Add(geneInfo);
		}
		_currentGeneInfoIndex++;
		geneInfo.SetValues(name, wrapper.BaseValue, wrapper.CurrentValue, wrapper.MinValue, wrapper.MaxValue, unit);
	}

	private void AddMultiGeneInfo(string name, float minPossible, float maxPossible, float outerMin, float outerMax, float innerMin, float innerMax, AnimationCurve scalingCurve, ValueDisplay.Unit unit)
	{
		MultiGeneInfo multiGeneInfo;
		if (_multiGeneInfo.Count > _currentMultiGeneInfoIndex)
		{
			multiGeneInfo = _multiGeneInfo[_currentMultiGeneInfoIndex];
		}
		else
		{
			multiGeneInfo = Object.Instantiate(_multiGeneInfoPrefab, _geneInfoParent);
			_multiGeneInfo.Add(multiGeneInfo);
		}
		_currentMultiGeneInfoIndex++;
		multiGeneInfo.SetValues(name, minPossible, maxPossible, outerMin, outerMax, innerMin, innerMax, scalingCurve, unit);
	}

	public void CloseButtonClicked()
	{
		CursorManager.Instance.BlockCursorRaycast = false;
		SetVisible(isVisble: false);
		MouseModeController.RemoveModal(this);
	}

	public void OpacityButtonClicked()
	{
		_isOpaque = !_isOpaque;
		_background.color = _background.color.SetAlpha(_isOpaque ? 1f : 0.75f);
	}
}
