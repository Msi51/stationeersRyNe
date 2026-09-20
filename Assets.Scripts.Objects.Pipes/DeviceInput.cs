using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceInput : DeviceAtmospherics
{
	public PipeNetwork ConnectedPipeNetwork;

	private int InputConnectionIndex;

	public override bool HasValidConnections => IsInputValid;

	public virtual bool IsInputValid
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
			bool flag = IsInputValid && base.GridController.CanContainAtmos(base.WorldGrid);
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
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureInput:
			return ConnectedPipeNetwork.Atmosphere.PressureGassesAndLiquids.ToDouble();
		case LogicType.TemperatureInput:
			return ConnectedPipeNetwork.Atmosphere.Temperature.ToDouble();
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
			return AtmosphereHelper.GasRatio(logicType, ConnectedPipeNetwork.Atmosphere);
		case LogicType.TotalMolesInput:
			return ConnectedPipeNetwork.Atmosphere.TotalMoles.ToDouble();
		case LogicType.CombustionInput:
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
