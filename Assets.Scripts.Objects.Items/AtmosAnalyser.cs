using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Electrical;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class AtmosAnalyser : Cartridge, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable
{
	public GridLayoutGroup MoleGrid;

	public TextMeshProUGUI PressureValue;

	public TextMeshProUGUI TemperatureValue;

	public TextMeshProUGUI ConvectedValue;

	public TextMeshProUGUI RadiatedValue;

	public TextMeshProUGUI LiquidValue;

	public TextMeshProUGUI CapacityValue;

	[FormerlySerializedAs("LatantValue")]
	public TextMeshProUGUI LatentValue;

	public TextMeshProUGUI StressValue;

	public TextMeshProUGUI PressureTitle;

	public TextMeshProUGUI TemperatureTitle;

	public TextMeshProUGUI ConvectedTitle;

	public TextMeshProUGUI RadiatedTitle;

	public TextMeshProUGUI LiquidTitle;

	public TextMeshProUGUI CapacityTitle;

	[FormerlySerializedAs("LatantTitle")]
	public TextMeshProUGUI LatentTitle;

	public TextMeshProUGUI StressTitle;

	public GasItem GasItemPrefab;

	private static string _worldString = "WORLD";

	private static string _roomString = "ROOM {0}";

	private static string _pipeNetworkString = "PIPE NETWORK {0}";

	private static string _landinpadNetworkString = "LANDING PAD NETWORK {0}";

	private static string _notApplicableString = "N/A";

	private static string _unitColorString = "grey";

	private static string _temperatureString = "{0:F1}°C";

	private static string _percentString = "{0:F0}%";

	private string _pressureValueText = string.Empty;

	private string _liquidVolumeValueText = string.Empty;

	private string _capacityValueText = string.Empty;

	private string _temperatureValueText = string.Empty;

	private string _selectedText = string.Empty;

	private string _energyConvectedText = string.Empty;

	private string _energyRadiatedText = string.Empty;

	private string _latentText = string.Empty;

	private string _stressText = string.Empty;

	private Dictionary<int, GasItem> _moleDisplay = new Dictionary<int, GasItem>();

	private HashSet<GasItem> _gasItems = new HashSet<GasItem>();

	private const string WHITE = "white";

	private const string ORANGE = "orange";

	private const string RED = "red";

	private bool _showThermalValues;

	private bool _isGasPipe;

	private float _topValue;

	public Atmosphere ScannedAtmosphere { get; private set; }

	private Atmosphere GetScannedAtmosphere()
	{
		Human rootParentHuman = RootParentHuman;
		if ((object)CursorManager.CursorThing == null && (object)rootParentHuman != null)
		{
			return rootParentHuman.WorldAtmosphere;
		}
		if ((object)rootParentHuman == null || rootParentHuman != InventoryManager.ParentHuman)
		{
			return base.WorldAtmosphere;
		}
		if (CursorManager.CursorThing.InternalAtmosphere != null)
		{
			return CursorManager.CursorThing.InternalAtmosphere;
		}
		Thing cursorThing = CursorManager.CursorThing;
		GasTankStorage gasTankStorage;
		if (cursorThing is INetworkedAtmospherics networkedAtmospherics)
		{
			if (networkedAtmospherics.StructureNetwork is AtmosphericsNetwork { Atmosphere: var atmosphere })
			{
				return atmosphere;
			}
			gasTankStorage = cursorThing as GasTankStorage;
			if ((object)gasTankStorage == null)
			{
				goto IL_008f;
			}
		}
		else
		{
			gasTankStorage = cursorThing as GasTankStorage;
			if ((object)gasTankStorage == null)
			{
				if (!(cursorThing is Human human))
				{
					goto IL_008f;
				}
				if ((bool)human.Suit?.AsThing && human.Suit.AsThing.HasReadableAtmosphere)
				{
					return human.Suit.InternalAtmosphere;
				}
				goto IL_0120;
			}
		}
		if (gasTankStorage.Slots[0].Occupant is GasCanister { InternalAtmosphere: var internalAtmosphere })
		{
			return internalAtmosphere;
		}
		goto IL_0120;
		IL_008f:
		if (cursorThing is VendingMachineRefrigerated vendingMachineRefrigerated && cursorThing.InternalAtmosphere != null)
		{
			return vendingMachineRefrigerated.InternalAtmosphere;
		}
		goto IL_0120;
		IL_0120:
		return base.WorldAtmosphere;
	}

	public override void Awake()
	{
		base.Awake();
		Array values = Enum.GetValues(typeof(Chemistry.GasType));
		for (int i = 0; i < values.Length; i++)
		{
			Chemistry.GasType gasType = (Chemistry.GasType)values.GetValue(i);
			GasItem gasItem = UnityEngine.Object.Instantiate(GasItemPrefab, MoleGrid.transform);
			foreach (GasThumbnail gasThumbnail in Stationpedia.Instance._gasThumbnails)
			{
				if (gasThumbnail.GasType == gasType)
				{
					gasItem.GasSymbol.sprite = gasThumbnail.Thumbnail;
					break;
				}
			}
			gasItem.GasMoles.text = string.Empty;
			gasItem.GasPercentage.text = string.Empty;
			gasItem.Parent.SetActive(value: false);
			gasItem.SetSymbol(GasItem.StateSymbolType.none);
			_moleDisplay.Add((int)gasType, gasItem);
			_gasItems.Add(gasItem);
		}
	}

	private Grid3 GetTargetGridPosition()
	{
		Grid3 gridPosition = GridPosition;
		if ((object)RootParentHuman != null)
		{
			gridPosition = RootParentHuman.GridPosition;
		}
		return gridPosition;
	}

	private void Clear()
	{
		Room room = base.GridController.RoomController.GetRoom(GetTargetGridPosition());
		_selectedText = ((room != null) ? string.Format(_roomString, StringManager.Get(room.RoomId)) : _worldString);
		_pressureValueText = _notApplicableString;
		_stressText = _notApplicableString;
		_temperatureValueText = _notApplicableString;
		_capacityValueText = _notApplicableString;
		_liquidVolumeValueText = _notApplicableString;
		foreach (GasItem gasItem in _gasItems)
		{
			gasItem.IsActive = false;
		}
	}

	private void ClearThermal()
	{
		_showThermalValues = false;
		_energyConvectedText = string.Empty;
		_energyRadiatedText = string.Empty;
		_latentText = string.Empty;
	}

	public static string GetEnergyUnitString(float energyInJoules)
	{
		if (RocketMath.Approximately(energyInJoules, 0f, 1E-06f))
		{
			return _notApplicableString;
		}
		if (Mathf.Abs(energyInJoules) > 1000000f)
		{
			return StringManager.Get(energyInJoules / 1000000f) + " MJ";
		}
		if (Mathf.Abs(energyInJoules) > 1000f)
		{
			return StringManager.Get(energyInJoules / 1000f) + " kJ";
		}
		if (Mathf.Abs(energyInJoules) < 1f)
		{
			return StringManager.Get(energyInJoules * 1000f) + " mJ";
		}
		return StringManager.Get(energyInJoules) + " J";
	}

	private void PrepareText()
	{
		IThermal thermal = null;
		if (InPlayerHand())
		{
			thermal = CursorManager.CursorThing as IThermal;
			ScannedAtmosphere = GetScannedAtmosphere();
			ScannedAtmosphere = (((ScannedAtmosphere == null || ScannedAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.World) && thermal != null) ? thermal.ThermalAtmosphere : ScannedAtmosphere);
			if (thermal is PipeRadiator)
			{
				ScannedAtmosphere = null;
			}
		}
		else
		{
			ScannedAtmosphere = base.WorldAtmosphere;
		}
		if (ScannedAtmosphere == null)
		{
			Clear();
		}
		if (thermal == null)
		{
			ClearThermal();
		}
		if (thermal != null)
		{
			_selectedText = thermal.DisplayName.ToUpper();
			_showThermalValues = true;
			_energyConvectedText = GetEnergyUnitString(thermal.EnergyConvected);
			_energyRadiatedText = GetEnergyUnitString(thermal.EnergyRadiated);
			_latentText = ((ScannedAtmosphere != null) ? GetEnergyUnitString(ScannedAtmosphere.LastTickLatentEnergy.ToFloat()) : _notApplicableString);
		}
		if (ScannedAtmosphere == null)
		{
			return;
		}
		switch (ScannedAtmosphere.Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
		case AtmosphereHelper.AtmosphereMode.Global:
		{
			Room room = base.GridController.RoomController.GetRoom(GetTargetGridPosition());
			_selectedText = ((room != null) ? string.Format(_roomString, StringManager.Get(room.RoomId)) : _worldString);
			break;
		}
		case AtmosphereHelper.AtmosphereMode.Network:
		{
			string format = ((ScannedAtmosphere.AtmosphericsNetwork is PipeNetwork) ? _pipeNetworkString : _landinpadNetworkString);
			_selectedText = string.Format(format, StringManager.Get(ScannedAtmosphere.AtmosphericsNetwork.ReferenceId)).ToUpper();
			break;
		}
		case AtmosphereHelper.AtmosphereMode.Thing:
			if (!ScannedAtmosphere.Thing)
			{
				Clear();
				return;
			}
			_selectedText = ScannedAtmosphere.Thing.DisplayName.ToUpper();
			break;
		}
		_capacityValueText = StringManager.Get((int)ScannedAtmosphere.Volume.ToFloat()) + "L";
		if (ScannedAtmosphere.PressureGassesAndLiquids > PressurekPa.Zero)
		{
			_pressureValueText = (ScannedAtmosphere.PressureGassesAndLiquids * 1000.0).ToFloat().RoundToSignificantDigits(3).ToStringPrefix(Chemistry.PascalUnit);
		}
		else
		{
			_pressureValueText = _notApplicableString;
		}
		if (ScannedAtmosphere.TotalMoles > MoleQuantity.Zero)
		{
			_temperatureValueText = string.Format(_temperatureString, StringManager.Get((ScannedAtmosphere.Temperature - Chemistry.Temperature.ZeroDegrees).ToFloat()));
		}
		else
		{
			_temperatureValueText = _notApplicableString;
		}
		bool flag = (_isGasPipe = ScannedAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Network && ScannedAtmosphere.AllowedMatterState == AtmosphereHelper.MatterState.Gas);
		float num = (ScannedAtmosphere.TotalVolumeLiquids / ScannedAtmosphere.Volume).ToFloat();
		string text = "white";
		if (flag && num > 0.006f)
		{
			text = "orange";
		}
		if (flag && num > 0.018f)
		{
			text = "red";
		}
		if (flag)
		{
			_stressText = "<color=" + text + ">" + StringManager.Get(num / 0.02f * 100f) + "%</color>";
		}
		if (ScannedAtmosphere.TotalVolumeLiquids > VolumeLitres.Zero)
		{
			_liquidVolumeValueText = "<color=" + text + ">" + StringManager.Get(ScannedAtmosphere.TotalVolumeLiquids.ToFloat().RoundToSignificantDigits(3)) + "L</color>";
		}
		else
		{
			_liquidVolumeValueText = _notApplicableString;
		}
		try
		{
			SetHash(ScannedAtmosphere.GasMixture.Oxygen, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.Nitrogen, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.Methane, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.Water, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.PollutedWater, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Pollutant, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.CarbonDioxide, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.NitrousOxide, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.LiquidNitrogen, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidOxygen, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidMethane, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Steam, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.LiquidCarbonDioxide, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidPollutant, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidNitrousOxide, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Hydrogen, ScannedAtmosphere, "white");
			SetHash(ScannedAtmosphere.GasMixture.LiquidHydrogen, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Hydrazine, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidHydrazine, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidAlcohol, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Helium, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidSodiumChloride, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Silanol, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidSilanol, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.HydrochloricAcid, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidHydrochloricAcid, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.Ozone, ScannedAtmosphere, text);
			SetHash(ScannedAtmosphere.GasMixture.LiquidOzone, ScannedAtmosphere, text);
		}
		catch (Exception)
		{
		}
	}

	private void SetHash(Mole mole, Atmosphere atmos, string volumeTextColor)
	{
		try
		{
			int type = (int)mole.Type;
			_moleDisplay.TryGetValue(type, out var value);
			if ((bool)value)
			{
				float value2 = mole.Quantity.ToFloat().RoundToSignificantDigits(3);
				value.Moles = value2.ToStringPrefix(Chemistry.MoleUnit);
				float value3 = mole.Volume.ToFloat().RoundToSignificantDigits(3);
				value.Volume = "<color=" + volumeTextColor + ">" + value3.ToStringPrefix(Chemistry.LitreUnit) + "</color>";
				float value4 = ((mole.Quantity / atmos.GasMixture.GetTotalMolesGassesAndLiquids).ToFloat() * 100f).RoundToSignificantDigits(3);
				value.Percent = string.Format(_percentString, StringManager.Get(value4));
				GasItem.StateSymbolType stateSymbolType = GasItem.StateSymbolType.none;
				if (mole.Quantity > Chemistry.MINIMUM_QUANTITY_MOLES && atmos.Temperature <= mole.FreezingTemperature() + TemperatureKelvin.One)
				{
					stateSymbolType |= GasItem.StateSymbolType.freezing;
				}
				if (mole.CheckChangeState(atmos.PressureGassesAndLiquids) == GasItem.StateSymbolType.condensation)
				{
					stateSymbolType |= GasItem.StateSymbolType.condensation;
				}
				if (mole.CheckChangeState(atmos.PressureGassesAndLiquids) == GasItem.StateSymbolType.evaporation)
				{
					stateSymbolType |= GasItem.StateSymbolType.evaporation;
				}
				SetGasItemStateSymbol(value, stateSymbolType).Forget();
				value.VolumeValue = mole.Volume.ToFloat();
				value.MolesValue = mole.Quantity.ToFloat();
				value.IsActive = mole.Quantity > MoleQuantity.Zero;
			}
		}
		catch (Exception)
		{
		}
	}

	private async UniTaskVoid SetGasItemStateSymbol(GasItem gasItem, GasItem.StateSymbolType state)
	{
		await UniTask.SwitchToMainThread();
		if (GameManager.GameState == GameState.Running && !(gasItem == null))
		{
			gasItem.SetSymbol(state);
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		PrepareText();
		SelectedTitle.text = _selectedText;
		PressureValue.text = _pressureValueText;
		StressValue.text = _stressText;
		TemperatureValue.text = _temperatureValueText;
		CapacityValue.text = _capacityValueText;
		LiquidValue.text = _liquidVolumeValueText;
		if (!_isGasPipe)
		{
			StressTitle.color = Color.clear;
			StressValue.color = Color.clear;
		}
		else
		{
			StressTitle.color = Color.gray;
			StressValue.color = Color.white;
		}
		if (_showThermalValues)
		{
			ConvectedValue.text = _energyConvectedText;
			RadiatedValue.text = _energyRadiatedText;
			LatentValue.text = _latentText;
			ConvectedTitle.color = Color.gray;
			ConvectedValue.color = Color.white;
			RadiatedTitle.color = Color.gray;
			RadiatedValue.color = Color.white;
			LatentTitle.color = Color.gray;
			LatentValue.color = Color.white;
		}
		else
		{
			ConvectedTitle.color = Color.clear;
			ConvectedValue.color = Color.clear;
			RadiatedTitle.color = Color.clear;
			RadiatedValue.color = Color.clear;
			LatentValue.color = Color.clear;
			LatentTitle.color = Color.clear;
		}
		foreach (GasItem gasItem in _gasItems)
		{
			if (gasItem.IsActive != gasItem.Parent.activeSelf)
			{
				gasItem.Parent.SetActive(gasItem.IsActive);
			}
			if (gasItem.IsActive)
			{
				if (gasItem.MolesValue > _topValue)
				{
					gasItem.ParentTransform.SetAsFirstSibling();
					_topValue = gasItem.MolesValue;
				}
				gasItem.GasMoles.text = gasItem.Moles;
				gasItem.GasPercentage.text = gasItem.Percent;
				gasItem.GasVolume.enabled = gasItem.VolumeValue > 0f && ScannedAtmosphere.Mode != AtmosphereHelper.AtmosphereMode.World;
				gasItem.GasVolume.text = gasItem.Volume;
			}
		}
		_scrollPanel.SetContentHeight(MoleGrid.preferredHeight);
	}
}
