using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceInputOutput : DeviceAtmospherics
{
	[Header("Input/Output")]
	public Connection InputConnection;

	public Connection InputConnection2;

	public Connection OutputConnection;

	public Connection OutputConnection2;

	[NonSerialized]
	[ReadOnly]
	public PipeNetwork InputNetwork;

	[NonSerialized]
	[ReadOnly]
	public PipeNetwork InputNetwork2;

	[NonSerialized]
	[ReadOnly]
	public PipeNetwork OutputNetwork;

	[NonSerialized]
	[ReadOnly]
	public PipeNetwork OutputNetwork2;

	public bool IsInputValid
	{
		get
		{
			if (InputNetwork != null && InputNetwork.IsNetworkValid())
			{
				return !InputNetwork.IsAwaitingEvent;
			}
			return false;
		}
	}

	public bool IsOutputValid
	{
		get
		{
			if (OutputNetwork != null && OutputNetwork.IsNetworkValid())
			{
				return !OutputNetwork.IsAwaitingEvent;
			}
			return false;
		}
	}

	public bool IsInput2Valid
	{
		get
		{
			if (InputNetwork2 != null && InputNetwork2.IsNetworkValid())
			{
				return !InputNetwork2.IsAwaitingEvent;
			}
			return false;
		}
	}

	public bool IsOutput2Valid
	{
		get
		{
			if (OutputNetwork2 != null && OutputNetwork2.IsNetworkValid())
			{
				return !OutputNetwork2.IsAwaitingEvent;
			}
			return false;
		}
	}

	public override bool HasValidConnections
	{
		get
		{
			if (IsInputValid && IsOutputValid && IsInput2Valid)
			{
				return IsOutput2Valid;
			}
			return false;
		}
	}

	public bool HasValidAtmospheres
	{
		get
		{
			if (InputNetwork?.Atmosphere != null && InputNetwork2?.Atmosphere != null && OutputNetwork?.Atmosphere != null)
			{
				return OutputNetwork2?.Atmosphere != null;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = IsInputValid && IsOutputValid;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation && HasErrorState)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
			{
				return true;
			}
			if (GameManager.RunSimulation && HasErrorState)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		PassiveTooltip passiveTooltip2 = passiveTooltip;
		if (InputConnection != null && InputConnection.Collider != null && hitCollider == InputConnection.Collider)
		{
			return passiveTooltip2.Populate(InputConnection);
		}
		if (OutputConnection != null && OutputConnection.Collider != null && hitCollider == OutputConnection.Collider)
		{
			return passiveTooltip2.Populate(OutputConnection);
		}
		if (InputConnection2 != null && InputConnection2.Collider != null && hitCollider == InputConnection2.Collider)
		{
			return passiveTooltip2.Populate(InputConnection2);
		}
		if (OutputConnection2 != null && OutputConnection2.Collider != null && hitCollider == OutputConnection2.Collider)
		{
			return passiveTooltip2.Populate(OutputConnection2);
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	protected override void CheckConnections()
	{
		INetworkedPipe networkedPipe = InputConnection?.GetINetworkedPipe();
		INetworkedPipe networkedPipe2 = InputConnection2?.GetINetworkedPipe();
		INetworkedPipe networkedPipe3 = OutputConnection?.GetINetworkedPipe();
		INetworkedPipe networkedPipe4 = OutputConnection2?.GetINetworkedPipe();
		InputNetwork = networkedPipe?.PipeNetwork;
		InputNetwork2 = networkedPipe2?.PipeNetwork;
		OutputNetwork = networkedPipe3?.PipeNetwork;
		OutputNetwork2 = networkedPipe4?.PipeNetwork;
		AssessError();
	}

	public virtual void AssessError()
	{
		bool flag = !IsInputValid;
		bool flag2 = !IsOutputValid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			CheckConnections();
			if (!OnOff)
			{
				Error = 0;
			}
		}
		if (GameManager.GameState == GameState.Running && interactable.Action != InteractableType.Error)
		{
			AssessError();
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		CheckConnections();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		AssessError();
	}

	public override void OnAddPipeNetwork(PipeNetwork newNetwork)
	{
		base.OnAddPipeNetwork(newNetwork);
		CheckConnections();
	}

	public override void OnRemovePipeNetwork(PipeNetwork oldNetwork)
	{
		base.OnRemovePipeNetwork(oldNetwork);
		if (oldNetwork == InputNetwork)
		{
			InputNetwork = null;
		}
		if (oldNetwork == OutputNetwork)
		{
			OutputNetwork = null;
		}
		if (oldNetwork == InputNetwork2)
		{
			InputNetwork2 = null;
		}
		CheckConnections();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (this is ICircuitHolder || this is ILogicAtmospheric)
		{
			switch (logicType)
			{
			case LogicType.PressureInput:
			case LogicType.TemperatureInput:
			case LogicType.RatioOxygenInput:
			case LogicType.RatioCarbonDioxideInput:
			case LogicType.RatioNitrogenInput:
			case LogicType.RatioPollutantInput:
			case LogicType.RatioMethaneInput:
			case LogicType.RatioWaterInput:
			case LogicType.RatioNitrousOxideInput:
			case LogicType.TotalMolesInput:
			case LogicType.CombustionInput:
			case LogicType.RatioLiquidNitrogenInput:
			case LogicType.RatioLiquidOxygenInput:
			case LogicType.RatioLiquidMethaneInput:
			case LogicType.RatioSteamInput:
			case LogicType.RatioLiquidCarbonDioxideInput:
			case LogicType.RatioLiquidPollutantInput:
			case LogicType.RatioLiquidNitrousOxideInput:
			case LogicType.RatioHydrogenInput:
			case LogicType.RatioLiquidHydrogenInput:
			case LogicType.RatioPollutedWaterInput:
			case LogicType.RatioHydrazineInput:
			case LogicType.RatioLiquidHydrazineInput:
			case LogicType.RatioLiquidAlcoholInput:
			case LogicType.RatioHeliumInput:
			case LogicType.RatioLiquidSodiumChlorideInput:
			case LogicType.RatioSilanolInput:
			case LogicType.RatioLiquidSilanolInput:
			case LogicType.RatioHydrochloricAcidInput:
			case LogicType.RatioLiquidHydrochloricAcidInput:
			case LogicType.RatioOzoneInput:
			case LogicType.RatioLiquidOzoneInput:
				return InputConnection?.IsValid ?? false;
			case LogicType.PressureInput2:
			case LogicType.TemperatureInput2:
			case LogicType.RatioOxygenInput2:
			case LogicType.RatioCarbonDioxideInput2:
			case LogicType.RatioNitrogenInput2:
			case LogicType.RatioPollutantInput2:
			case LogicType.RatioMethaneInput2:
			case LogicType.RatioWaterInput2:
			case LogicType.RatioNitrousOxideInput2:
			case LogicType.TotalMolesInput2:
			case LogicType.CombustionInput2:
			case LogicType.RatioLiquidNitrogenInput2:
			case LogicType.RatioLiquidOxygenInput2:
			case LogicType.RatioLiquidMethaneInput2:
			case LogicType.RatioSteamInput2:
			case LogicType.RatioLiquidCarbonDioxideInput2:
			case LogicType.RatioLiquidPollutantInput2:
			case LogicType.RatioLiquidNitrousOxideInput2:
			case LogicType.RatioHydrogenInput2:
			case LogicType.RatioLiquidHydrogenInput2:
			case LogicType.RatioPollutedWaterInput2:
			case LogicType.RatioHydrazineInput2:
			case LogicType.RatioLiquidHydrazineInput2:
			case LogicType.RatioLiquidAlcoholInput2:
			case LogicType.RatioHeliumInput2:
			case LogicType.RatioLiquidSodiumChlorideInput2:
			case LogicType.RatioSilanolInput2:
			case LogicType.RatioLiquidSilanolInput2:
			case LogicType.RatioHydrochloricAcidInput2:
			case LogicType.RatioLiquidHydrochloricAcidInput2:
			case LogicType.RatioOzoneInput2:
			case LogicType.RatioLiquidOzoneInput2:
				return InputConnection2?.IsValid ?? false;
			case LogicType.PressureOutput:
			case LogicType.TemperatureOutput:
			case LogicType.RatioOxygenOutput:
			case LogicType.RatioCarbonDioxideOutput:
			case LogicType.RatioNitrogenOutput:
			case LogicType.RatioPollutantOutput:
			case LogicType.RatioMethaneOutput:
			case LogicType.RatioWaterOutput:
			case LogicType.RatioNitrousOxideOutput:
			case LogicType.TotalMolesOutput:
			case LogicType.CombustionOutput:
			case LogicType.RatioLiquidNitrogenOutput:
			case LogicType.RatioLiquidOxygenOutput:
			case LogicType.RatioLiquidMethaneOutput:
			case LogicType.RatioSteamOutput:
			case LogicType.RatioLiquidCarbonDioxideOutput:
			case LogicType.RatioLiquidPollutantOutput:
			case LogicType.RatioLiquidNitrousOxideOutput:
			case LogicType.RatioHydrogenOutput:
			case LogicType.RatioLiquidHydrogenOutput:
			case LogicType.RatioPollutedWaterOutput:
			case LogicType.RatioHydrazineOutput:
			case LogicType.RatioLiquidHydrazineOutput:
			case LogicType.RatioLiquidAlcoholOutput:
			case LogicType.RatioHeliumOutput:
			case LogicType.RatioLiquidSodiumChlorideOutput:
			case LogicType.RatioSilanolOutput:
			case LogicType.RatioLiquidSilanolOutput:
			case LogicType.RatioHydrochloricAcidOutput:
			case LogicType.RatioLiquidHydrochloricAcidOutput:
			case LogicType.RatioOzoneOutput:
			case LogicType.RatioLiquidOzoneOutput:
				return OutputConnection?.IsValid ?? false;
			case LogicType.PressureOutput2:
			case LogicType.TemperatureOutput2:
			case LogicType.RatioOxygenOutput2:
			case LogicType.RatioCarbonDioxideOutput2:
			case LogicType.RatioNitrogenOutput2:
			case LogicType.RatioPollutantOutput2:
			case LogicType.RatioMethaneOutput2:
			case LogicType.RatioWaterOutput2:
			case LogicType.RatioNitrousOxideOutput2:
			case LogicType.TotalMolesOutput2:
			case LogicType.CombustionOutput2:
			case LogicType.RatioLiquidNitrogenOutput2:
			case LogicType.RatioLiquidOxygenOutput2:
			case LogicType.RatioLiquidMethaneOutput2:
			case LogicType.RatioSteamOutput2:
			case LogicType.RatioLiquidCarbonDioxideOutput2:
			case LogicType.RatioLiquidPollutantOutput2:
			case LogicType.RatioLiquidNitrousOxideOutput2:
			case LogicType.RatioHydrogenOutput2:
			case LogicType.RatioLiquidHydrogenOutput2:
			case LogicType.RatioPollutedWaterOutput2:
			case LogicType.RatioHydrazineOutput2:
			case LogicType.RatioLiquidHydrazineOutput2:
			case LogicType.RatioLiquidAlcoholOutput2:
			case LogicType.RatioHeliumOutput2:
			case LogicType.RatioLiquidSodiumChlorideOutput2:
			case LogicType.RatioSilanolOutput2:
			case LogicType.RatioLiquidSilanolOutput2:
			case LogicType.RatioHydrochloricAcidOutput2:
			case LogicType.RatioLiquidHydrochloricAcidOutput2:
			case LogicType.RatioOzoneOutput2:
			case LogicType.RatioLiquidOzoneOutput2:
				return OutputConnection2?.IsValid ?? false;
			}
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureInput:
			return (InputNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.TemperatureInput:
			return (InputNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicType.RatioOxygenInput:
		case LogicType.RatioCarbonDioxideInput:
		case LogicType.RatioNitrogenInput:
		case LogicType.RatioPollutantInput:
		case LogicType.RatioMethaneInput:
		case LogicType.RatioWaterInput:
		case LogicType.RatioNitrousOxideInput:
		case LogicType.RatioLiquidNitrogenInput:
		case LogicType.RatioLiquidOxygenInput:
		case LogicType.RatioLiquidMethaneInput:
		case LogicType.RatioSteamInput:
		case LogicType.RatioLiquidCarbonDioxideInput:
		case LogicType.RatioLiquidPollutantInput:
		case LogicType.RatioLiquidNitrousOxideInput:
		case LogicType.RatioHydrogenInput:
		case LogicType.RatioLiquidHydrogenInput:
		case LogicType.RatioPollutedWaterInput:
		case LogicType.RatioHydrazineInput:
		case LogicType.RatioLiquidHydrazineInput:
		case LogicType.RatioLiquidAlcoholInput:
		case LogicType.RatioHeliumInput:
		case LogicType.RatioLiquidSodiumChlorideInput:
		case LogicType.RatioSilanolInput:
		case LogicType.RatioLiquidSilanolInput:
		case LogicType.RatioHydrochloricAcidInput:
		case LogicType.RatioLiquidHydrochloricAcidInput:
		case LogicType.RatioOzoneInput:
		case LogicType.RatioLiquidOzoneInput:
			return AtmosphereHelper.GasRatio(logicType, InputNetwork?.Atmosphere);
		case LogicType.TotalMolesInput:
			return (InputNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.CombustionInput:
			return (InputNetwork?.Atmosphere != null) ? (InputNetwork.Atmosphere.Inflamed ? 1 : 0) : 0;
		case LogicType.PressureInput2:
			return (InputNetwork2?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.TemperatureInput2:
			return (InputNetwork2?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicType.RatioOxygenInput2:
		case LogicType.RatioCarbonDioxideInput2:
		case LogicType.RatioNitrogenInput2:
		case LogicType.RatioPollutantInput2:
		case LogicType.RatioMethaneInput2:
		case LogicType.RatioWaterInput2:
		case LogicType.RatioNitrousOxideInput2:
		case LogicType.RatioLiquidNitrogenInput2:
		case LogicType.RatioLiquidOxygenInput2:
		case LogicType.RatioLiquidMethaneInput2:
		case LogicType.RatioSteamInput2:
		case LogicType.RatioLiquidCarbonDioxideInput2:
		case LogicType.RatioLiquidPollutantInput2:
		case LogicType.RatioLiquidNitrousOxideInput2:
		case LogicType.RatioHydrogenInput2:
		case LogicType.RatioLiquidHydrogenInput2:
		case LogicType.RatioPollutedWaterInput2:
		case LogicType.RatioHydrazineInput2:
		case LogicType.RatioLiquidHydrazineInput2:
		case LogicType.RatioLiquidAlcoholInput2:
		case LogicType.RatioHeliumInput2:
		case LogicType.RatioLiquidSodiumChlorideInput2:
		case LogicType.RatioSilanolInput2:
		case LogicType.RatioLiquidSilanolInput2:
		case LogicType.RatioHydrochloricAcidInput2:
		case LogicType.RatioLiquidHydrochloricAcidInput2:
		case LogicType.RatioOzoneInput2:
		case LogicType.RatioLiquidOzoneInput2:
			return AtmosphereHelper.GasRatio(logicType, InputNetwork2?.Atmosphere);
		case LogicType.TotalMolesInput2:
			return (InputNetwork2?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.CombustionInput2:
			return (InputNetwork2?.Atmosphere != null) ? (InputNetwork2.Atmosphere.Inflamed ? 1 : 0) : 0;
		case LogicType.PressureOutput:
			return (OutputNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.TemperatureOutput:
			return (OutputNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicType.RatioOxygenOutput:
		case LogicType.RatioCarbonDioxideOutput:
		case LogicType.RatioNitrogenOutput:
		case LogicType.RatioPollutantOutput:
		case LogicType.RatioMethaneOutput:
		case LogicType.RatioWaterOutput:
		case LogicType.RatioNitrousOxideOutput:
		case LogicType.RatioLiquidNitrogenOutput:
		case LogicType.RatioLiquidOxygenOutput:
		case LogicType.RatioLiquidMethaneOutput:
		case LogicType.RatioSteamOutput:
		case LogicType.RatioLiquidCarbonDioxideOutput:
		case LogicType.RatioLiquidPollutantOutput:
		case LogicType.RatioLiquidNitrousOxideOutput:
		case LogicType.RatioHydrogenOutput:
		case LogicType.RatioLiquidHydrogenOutput:
		case LogicType.RatioPollutedWaterOutput:
		case LogicType.RatioHydrazineOutput:
		case LogicType.RatioLiquidHydrazineOutput:
		case LogicType.RatioLiquidAlcoholOutput:
		case LogicType.RatioHeliumOutput:
		case LogicType.RatioLiquidSodiumChlorideOutput:
		case LogicType.RatioSilanolOutput:
		case LogicType.RatioLiquidSilanolOutput:
		case LogicType.RatioHydrochloricAcidOutput:
		case LogicType.RatioLiquidHydrochloricAcidOutput:
		case LogicType.RatioOzoneOutput:
		case LogicType.RatioLiquidOzoneOutput:
			return AtmosphereHelper.GasRatio(logicType, OutputNetwork?.Atmosphere);
		case LogicType.TotalMolesOutput:
			return (OutputNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.CombustionOutput:
			return (OutputNetwork?.Atmosphere != null) ? (OutputNetwork.Atmosphere.Inflamed ? 1 : 0) : 0;
		case LogicType.PressureOutput2:
			return (OutputNetwork2?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.TemperatureOutput2:
			return (OutputNetwork2?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicType.RatioOxygenOutput2:
		case LogicType.RatioCarbonDioxideOutput2:
		case LogicType.RatioNitrogenOutput2:
		case LogicType.RatioPollutantOutput2:
		case LogicType.RatioMethaneOutput2:
		case LogicType.RatioWaterOutput2:
		case LogicType.RatioNitrousOxideOutput2:
		case LogicType.RatioLiquidNitrogenOutput2:
		case LogicType.RatioLiquidOxygenOutput2:
		case LogicType.RatioLiquidMethaneOutput2:
		case LogicType.RatioSteamOutput2:
		case LogicType.RatioLiquidCarbonDioxideOutput2:
		case LogicType.RatioLiquidPollutantOutput2:
		case LogicType.RatioLiquidNitrousOxideOutput2:
		case LogicType.RatioHydrogenOutput2:
		case LogicType.RatioLiquidHydrogenOutput2:
		case LogicType.RatioPollutedWaterOutput2:
		case LogicType.RatioHydrazineOutput2:
		case LogicType.RatioLiquidHydrazineOutput2:
		case LogicType.RatioLiquidAlcoholOutput2:
		case LogicType.RatioHeliumOutput2:
		case LogicType.RatioLiquidSodiumChlorideOutput2:
		case LogicType.RatioSilanolOutput2:
		case LogicType.RatioLiquidSilanolOutput2:
		case LogicType.RatioHydrochloricAcidOutput2:
		case LogicType.RatioLiquidHydrochloricAcidOutput2:
		case LogicType.RatioOzoneOutput2:
		case LogicType.RatioLiquidOzoneOutput2:
			return AtmosphereHelper.GasRatio(logicType, OutputNetwork2?.Atmosphere);
		case LogicType.TotalMolesOutput2:
			return (OutputNetwork2?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.CombustionOutput2:
			return (OutputNetwork2?.Atmosphere != null) ? (OutputNetwork2.Atmosphere.Inflamed ? 1 : 0) : 0;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		InputConnection.SetGrids();
		InputConnection2.SetGrids();
		OutputConnection.SetGrids();
		OutputConnection2.SetGrids();
	}
}
