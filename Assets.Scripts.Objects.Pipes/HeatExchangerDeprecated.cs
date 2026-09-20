using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class HeatExchangerDeprecated : DeviceInputOutput
{
	[Header("HeatExchanger")]
	public float Volume = 100f;

	public readonly float HeatExchangeArea = 10f;

	public Atmosphere InternalAtmosphere2 = new Atmosphere();

	public Atmosphere InternalAtmosphere3 = new Atmosphere();

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsInput2Valid && base.IsOutputValid && base.IsOutput2Valid;
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

	public override void Start()
	{
		base.Start();
		InternalAtmosphere2.Volume = new VolumeLitres(Volume);
		InternalAtmosphere3.Volume = new VolumeLitres(Volume);
	}

	public override void OnAtmosphericTick()
	{
		if (InputNetwork != null && InputNetwork2 != null && OutputNetwork != null && OutputNetwork2 != null && InputNetwork.Atmosphere != null && InputNetwork2.Atmosphere != null)
		{
			bool flag = InputNetwork.Atmosphere.PressureGassesAndLiquids < OutputNetwork.Atmosphere.PressureGassesAndLiquids + new PressurekPa(10.0);
			if (flag && InputNetwork.NetworkContentType == Pipe.ContentType.Liquid)
			{
				flag = InputNetwork.Atmosphere.LiquidVolumeRatio < OutputNetwork.Atmosphere.LiquidVolumeRatio + 0.001f;
			}
			bool flag2 = InputNetwork2.Atmosphere.PressureGassesAndLiquids < OutputNetwork2.Atmosphere.PressureGassesAndLiquids + new PressurekPa(10.0);
			if (flag2 && InputNetwork2.NetworkContentType == Pipe.ContentType.Liquid)
			{
				flag2 = InputNetwork2.Atmosphere.LiquidVolumeRatio < OutputNetwork2.Atmosphere.LiquidVolumeRatio + 0.001f;
			}
			if (flag)
			{
				AtmosphereHelper.Mix(InternalAtmosphere2, InputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, InternalAtmosphere2, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
			}
			if (flag2)
			{
				AtmosphereHelper.Mix(InternalAtmosphere3, InputNetwork2.Atmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.MoveToEqualize(InputNetwork2.Atmosphere, InternalAtmosphere3, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
			}
			MoleEnergy convectionHeat = AtmosphereHelper.GetConvectionHeat(InternalAtmosphere2, InternalAtmosphere3, HeatExchangeArea * HeatExchangeRatio(InternalAtmosphere2, InternalAtmosphere3));
			InternalAtmosphere2.GasMixture.TransferEnergyTo(ref InternalAtmosphere3.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedSeconds * 1.0);
			if (flag)
			{
				AtmosphereHelper.Mix(InternalAtmosphere2, OutputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.MoveToEqualize(InternalAtmosphere2, OutputNetwork.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
			}
			if (flag2)
			{
				AtmosphereHelper.Mix(InternalAtmosphere3, OutputNetwork2.Atmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.MoveToEqualize(InternalAtmosphere3, OutputNetwork2.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
			}
		}
	}

	protected float HeatExchangeRatio(Atmosphere leftSide, Atmosphere rightSide)
	{
		return leftSide.HeatExchangeRatio() * rightSide.HeatExchangeRatio();
	}

	protected override void CheckConnections()
	{
		INetworkedPipe iNetworkedPipe = InputConnection.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe2 = InputConnection2.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe3 = OutputConnection.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe4 = OutputConnection2.GetINetworkedPipe();
		InputNetwork = iNetworkedPipe?.PipeNetwork;
		InputNetwork2 = iNetworkedPipe2?.PipeNetwork;
		OutputNetwork = iNetworkedPipe3?.PipeNetwork;
		OutputNetwork2 = iNetworkedPipe4?.PipeNetwork;
		AssessError();
	}

	public override void AssessError()
	{
		bool flag = base.IsInputValid && base.IsInput2Valid && base.IsOutputValid && base.IsOutput2Valid;
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new HeatExchangerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is HeatExchangerSaveData heatExchangerSaveData)
		{
			InternalAtmosphere2.Load(heatExchangerSaveData.atmos2);
			InternalAtmosphere3.Load(heatExchangerSaveData.atmos3);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is HeatExchangerSaveData heatExchangerSaveData)
		{
			heatExchangerSaveData.atmos2 = new AtmosphereSaveData(InternalAtmosphere2);
			heatExchangerSaveData.atmos3 = new AtmosphereSaveData(InternalAtmosphere3);
		}
	}
}
