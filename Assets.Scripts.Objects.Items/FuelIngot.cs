using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class FuelIngot : Ingot, ISolidFuel, IQuantity, ITradable, IEvaluable, IReferencable
{
	[Header("FuelIngot")]
	[SerializeField]
	[FormerlySerializedAs("Temperature")]
	private float temperature = 273.15f;

	public List<SpawnGas> SpawnContents = new List<SpawnGas>();

	[NonSerialized]
	public GasMixture GasMixture = GasMixtureHelper.Create();

	[Header("Fuel Ore")]
	public float EnergyPerSecond;

	public TemperatureKelvin Temperature => new TemperatureKelvin(temperature);

	public float GetEnergyPerSecond()
	{
		return EnergyPerSecond;
	}

	public override void Start()
	{
		base.Start();
		foreach (SpawnGas spawnContent in SpawnContents)
		{
			GasMixture.Add(new Mole(spawnContent.Type, spawnContent.GetQuantity(), spawnContent.GetEnergy()));
		}
	}

	public override StringBuilder GetSlotTooltip()
	{
		return FuelOre.MakeSlotTooltip(base.GetSlotTooltip(), this);
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (SpawnContents.Count > 0)
		{
			if (localAtmosphere.IsGlobalAtmosphere)
			{
				localAtmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
				GasMixture.TotalEnergy = IdealGas.Energy(GasMixture.HeatCapacity, RocketMath.Lerp(localAtmosphere.Temperature, Temperature, 0.6));
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, GasMixture);
			}
			else if (!localAtmosphere.IsAboveArmstrong())
			{
				GasMixture.TotalEnergy = IdealGas.Energy(GasMixture.HeatCapacity, Temperature);
				AtmosphericEventInstance.CreateAdd(localAtmosphere, GasMixture);
			}
			else
			{
				GasMixture.TotalEnergy = IdealGas.Energy(GasMixture.HeatCapacity, RocketMath.Lerp(localAtmosphere.Temperature, Temperature, 0.6));
				AtmosphericEventInstance.CreateAdd(localAtmosphere, GasMixture);
			}
		}
		base.Smelt(localAtmosphere, reagentMixture);
	}
}
