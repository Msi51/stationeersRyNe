using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class GasTankStorage : Device, ISmartRotatable, IRocketInternals, IRocketComponent
{
	[Header("TankStorage")]
	public Pipe.ContentType ContentType = Pipe.ContentType.Gas;

	public List<GasCanister> ConnectedGasCanisters = new List<GasCanister>();

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public AtmosphereHelper.MatterState MatterState => ContentType switch
	{
		Pipe.ContentType.Unknown => AtmosphereHelper.MatterState.All, 
		Pipe.ContentType.Gas => AtmosphereHelper.MatterState.Gas, 
		Pipe.ContentType.Liquid => AtmosphereHelper.MatterState.Liquid, 
		Pipe.ContentType.All => AtmosphereHelper.MatterState.All, 
		_ => AtmosphereHelper.MatterState.All, 
	};

	public PressurekPa TankPressure
	{
		get
		{
			if (ConnectedGasCanisters.Count <= 0)
			{
				return PressurekPa.Zero;
			}
			return ConnectedGasCanisters[0].InternalAtmosphere.PressureGassesAndLiquids;
		}
	}

	public TemperatureKelvin TankTemperature
	{
		get
		{
			if (ConnectedGasCanisters.Count <= 0)
			{
				return TemperatureKelvin.Zero;
			}
			return ConnectedGasCanisters[0].InternalAtmosphere.Temperature;
		}
	}

	public MoleQuantity TankQuantity
	{
		get
		{
			if (ConnectedGasCanisters.Count <= 0)
			{
				return MoleQuantity.Zero;
			}
			return ConnectedGasCanisters[0].InternalAtmosphere.TotalMoles;
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		GasCanister gasCanister = newChild as GasCanister;
		if (!(gasCanister == null))
		{
			if (gasCanister.CanisterContentType == ContentType)
			{
				ConnectedGasCanisters.Add(gasCanister);
			}
			newChild.ThingTransform.localScale = Vector3.one;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		GasCanister gasCanister = previousChild as GasCanister;
		if (!(gasCanister == null))
		{
			ConnectedGasCanisters.Remove(gasCanister);
		}
	}

	public override void OnAtmosphericTick()
	{
		if (ConnectedGasCanisters.Count == 0 || ConnectedPipeNetworks.Count == 0)
		{
			return;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		VolumeLitres zero = VolumeLitres.Zero;
		foreach (GasCanister connectedGasCanister in ConnectedGasCanisters)
		{
			if (!(connectedGasCanister == null))
			{
				gasMixture.Add(connectedGasCanister.InternalAtmosphere.GasMixture);
				zero += connectedGasCanister.InternalAtmosphere.GetVolume(AtmosphereHelper.MatterState.All);
			}
		}
		foreach (PipeNetwork connectedPipeNetwork in ConnectedPipeNetworks)
		{
			gasMixture.Add(connectedPipeNetwork.Atmosphere.GasMixture);
			zero += connectedPipeNetwork.Atmosphere.GetVolume(AtmosphereHelper.MatterState.All);
		}
		foreach (GasCanister connectedGasCanister2 in ConnectedGasCanisters)
		{
			if (!(connectedGasCanister2 == null))
			{
				GasMixture gasMixture2 = new GasMixture(gasMixture);
				gasMixture2.Scale((connectedGasCanister2.InternalAtmosphere.GetVolume(AtmosphereHelper.MatterState.All) / zero).ToFloat());
				connectedGasCanister2.InternalAtmosphere.GasMixture.Set(gasMixture2);
			}
		}
		foreach (PipeNetwork connectedPipeNetwork2 in ConnectedPipeNetworks)
		{
			GasMixture gasMixture3 = new GasMixture(gasMixture);
			gasMixture3.Scale((connectedPipeNetwork2.Atmosphere.GetVolume(AtmosphereHelper.MatterState.All) / zero).ToFloat());
			connectedPipeNetwork2.Atmosphere.GasMixture.Set(gasMixture3);
		}
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
		case LogicType.Quantity:
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
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
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
			if (ConnectedGasCanisters.Count <= 0)
			{
				return 0.0;
			}
			return AtmosphereHelper.GasRatio(logicType, ConnectedGasCanisters[0].InternalAtmosphere);
		case LogicType.Pressure:
			return TankPressure.ToDouble();
		case LogicType.Quantity:
			return TankQuantity.ToDouble();
		case LogicType.Temperature:
			return TankTemperature.ToDouble();
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
