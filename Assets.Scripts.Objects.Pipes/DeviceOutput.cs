using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceOutput : DeviceAtmospherics
{
	public PipeNetwork ConnectedPipeNetwork;

	private int InputConnectionIndex;

	public override bool HasValidConnections => IsOutputValid;

	public bool IsOutputValid
	{
		get
		{
			if (ConnectedPipeNetwork != null && ConnectedPipeNetwork.IsNetworkValid())
			{
				return !ConnectedPipeNetwork.IsAwaitingEvent;
			}
			return false;
		}
	}

	public override bool HasPipeNetwork
	{
		get
		{
			if (ConnectedPipeNetwork != null)
			{
				return ConnectedPipeNetwork.IsNetworkValid();
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = IsOutputValid && base.GridController.CanContainAtmos(base.WorldGrid);
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	public override void Awake()
	{
		base.Awake();
		InputConnectionIndex = OpenEnds.FindIndex((Connection end) => end.ConnectionType == NetworkType.Pipe && end.ConnectionRole == ConnectionRole.Input);
	}

	protected override void CheckConnections()
	{
		_ = IsOperable;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
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
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureOutput:
			return (ConnectedPipeNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.TemperatureOutput:
			return (ConnectedPipeNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
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
			if (ConnectedPipeNetwork?.Atmosphere == null)
			{
				return 0.0;
			}
			return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere);
		case LogicType.TotalMolesOutput:
			return (ConnectedPipeNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.CombustionOutput:
			if (ConnectedPipeNetwork?.Atmosphere == null)
			{
				return 0.0;
			}
			return ConnectedPipeNetwork.Atmosphere.Inflamed ? 1 : 0;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void OnAddPipeNetwork(PipeNetwork newNetwork)
	{
		base.OnAddPipeNetwork(newNetwork);
		ConnectedPipeNetwork = newNetwork;
		if (GameManager.RunSimulation)
		{
			CheckConnections();
		}
	}

	public override void OnRemovePipeNetwork(PipeNetwork oldNetwork)
	{
		base.OnRemovePipeNetwork(oldNetwork);
		if (oldNetwork == ConnectedPipeNetwork)
		{
			ConnectedPipeNetwork = null;
		}
		if (GameManager.RunSimulation)
		{
			CheckConnections();
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		InputConnectionIndex = OpenEnds.FindIndex((Connection end) => end.ConnectionType == NetworkType.Pipe && end.ConnectionRole == ConnectionRole.Input);
		if (InputConnectionIndex > 0)
		{
			ConnectedPipeNetwork = OpenEnds[InputConnectionIndex].GetINetworkedPipe()?.PipeNetwork;
		}
		if (GameManager.RunSimulation)
		{
			CheckConnections();
		}
	}
}
