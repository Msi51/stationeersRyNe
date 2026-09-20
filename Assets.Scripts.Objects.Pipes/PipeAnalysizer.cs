using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PipeAnalysizer : DevicePipeMounted, IRocketInternals, IRocketComponent
{
	private string _tooltip;

	public new RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public new bool StrictlyInternal => false;

	public new RocketNetwork RocketNetwork { get; set; }

	public bool PipeBurst
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return false;
			}
			if (base.SmallCell?.Pipe?.PipeNetwork == null)
			{
				return false;
			}
			return base.SmallCell.Pipe.PipeNetwork.HasNetworkFault;
		}
	}

	public bool PipeIgnited
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return false;
			}
			return base.NetworkAtmosphere.Sparked;
		}
	}

	public PressurekPa PipePressure
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return new PressurekPa(-1.0);
			}
			return base.NetworkAtmosphere.PressureGassesAndLiquids;
		}
	}

	public TemperatureKelvin PipeTemperature
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return new TemperatureKelvin(-1.0);
			}
			return base.NetworkAtmosphere.Temperature;
		}
	}

	public MoleQuantity TotalMoles
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return new MoleQuantity(-1.0);
			}
			return base.NetworkAtmosphere.TotalMoles;
		}
	}

	public VolumeLitres Volume
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return new VolumeLitres(-1.0);
			}
			return base.NetworkAtmosphere.GetVolume(base.NetworkAtmosphere.AllowedMatterState);
		}
	}

	public VolumeLitres VolumeOfLiquid
	{
		get
		{
			if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
			{
				return new VolumeLitres(-1.0);
			}
			return base.NetworkAtmosphere.TotalVolumeLiquids;
		}
	}

	public new void OnLaunch(bool immediate = false)
	{
	}

	public new void OnLanded(bool immediate = false)
	{
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.TotalMoles:
		case LogicType.Volume:
		case LogicType.RatioNitrousOxide:
		case LogicType.Combustion:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.VolumeOfLiquid:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.NetworkFault:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.NetworkFault:
			if (!PipeBurst)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.Combustion:
			if (!PipeIgnited)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.Pressure:
			return PipePressure.ToDouble();
		case LogicType.Temperature:
			return PipeTemperature.ToDouble();
		case LogicType.TotalMoles:
			return TotalMoles.ToDouble();
		case LogicType.Volume:
			return Volume.ToDouble();
		case LogicType.VolumeOfLiquid:
			return VolumeOfLiquid.ToDouble();
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return GasRatio(logicType);
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override double GasRatio(LogicType logicType)
	{
		if (!OnOff || !Powered || Error == 1 || !HasReadableAtmosphere)
		{
			return -1.0;
		}
		return AtmosphereHelper.GasRatio(logicType, base.NetworkAtmosphere);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == null || hitCollider.transform != ThingTransform)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		if (!OnOff || !Powered || Error == 1)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		if (!HasReadableAtmosphere)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		StringBuilder stringBuilder = new StringBuilder();
		AtmosphericsManager.MakeGasTooltip(base.SmallCell.Pipe.PipeNetwork.Atmosphere, stringBuilder);
		result.Title = DisplayName;
		result.Extended = stringBuilder.ToString();
		return result;
	}

	public override void CheckForPipe()
	{
		if (GameManager.RunSimulation && !IsValidPipe() && Error == 0)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && IsValidPipe() && Error == 1)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running && !IsCursor && (interactable.Action == InteractableType.OnOff || interactable.Action == InteractableType.Powered))
		{
			CheckForPipe();
		}
	}
}
