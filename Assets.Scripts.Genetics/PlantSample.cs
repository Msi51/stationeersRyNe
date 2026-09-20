using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using UnityEngine.Networking;

namespace Assets.Scripts.Genetics;

public class PlantSample : IRocketReaderWriter, IGenetics
{
	public string PlantName { get; set; } = string.Empty;

	public List<GeneWrapper> Genes { get; set; }

	public RequirementWrapper TimeUntilDehydrationDamage { get; set; }

	public RequirementWrapper TimeUntilUndesiredGasDamage { get; set; }

	public RequirementWrapper TimeUntilFrozenDamage { get; set; }

	public RequirementWrapper TimeUntilOverHeatedDamage { get; set; }

	public RequirementWrapper TimeUntilSuffocatedDamage { get; set; }

	public RequirementWrapper TimeUntilLowPressureDamage { get; set; }

	public RequirementWrapper TimeUntilHighPressureDamage { get; set; }

	public RequirementWrapper LightPerDay { get; set; }

	public RequirementWrapper DarknessPerDay { get; set; }

	public RequirementWrapper TimeUntilLightDamage { get; set; }

	public RequirementWrapper TimeUntilDarknessDamage { get; set; }

	public RequirementWrapper WaterUsage { get; set; }

	public RequirementWrapper GasProduction { get; set; }

	public RequirementWrapper UndesiredGasResistance { get; set; }

	public RequirementWrapper MinGrowTemperatureC { get; set; }

	public RequirementWrapper MinIdealGrowTemperatureC { get; set; }

	public RequirementWrapper MaxGrowTemperatureC { get; set; }

	public RequirementWrapper MaxIdealGrowTemperatureC { get; set; }

	public RequirementWrapper MinGrowPressure { get; set; }

	public RequirementWrapper MinIdealGrowPressure { get; set; }

	public RequirementWrapper MaxGrowPressure { get; set; }

	public RequirementWrapper MaxIdealGrowPressure { get; set; }

	public RequirementWrapper GrowthSpeedMultiplier { get; set; }

	public PlantSample()
	{
	}

	public PlantSample(Plant plant)
	{
		PlantName = plant.DisplayName;
		Genes = new List<GeneWrapper>(plant.Genes.Lookup.Count);
		foreach (GeneWrapper value in plant.Genes.Lookup.Values)
		{
			Genes.Add(new GeneWrapper(value));
		}
		TimeUntilDehydrationDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilDehydrationDamage);
		TimeUntilUndesiredGasDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilUndesiredGasDamage);
		TimeUntilFrozenDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilFrozenDamage);
		TimeUntilOverHeatedDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilOverHeatedDamage);
		TimeUntilSuffocatedDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilSuffocatedDamage);
		TimeUntilLowPressureDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilLowPressureDamage);
		TimeUntilHighPressureDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilHighPressureDamage);
		LightPerDay = new RequirementWrapper(plant.lifeRequirements.LightPerDay);
		DarknessPerDay = new RequirementWrapper(plant.lifeRequirements.DarknessPerDay);
		WaterUsage = new RequirementWrapper(plant.lifeRequirements.WaterUsage);
		GasProduction = new RequirementWrapper(plant.lifeRequirements.GasProduction);
		UndesiredGasResistance = new RequirementWrapper(plant.lifeRequirements.UndesiredGasResistance);
		GrowthSpeedMultiplier = new RequirementWrapper(plant.lifeRequirements.GrowthSpeedMultiplier);
		TimeUntilLightDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilLightDamage);
		TimeUntilDarknessDamage = new RequirementWrapper(plant.lifeRequirements.TimeUntilDarknessDamage);
		MinGrowTemperatureC = new RequirementWrapper(plant.lifeRequirements.GrowTemperatureC.Min(0f), plant.lifeRequirements.GrowTemperatureC.Min(-1f), plant.lifeRequirements.GrowTemperatureC.Min(1f), plant.lifeRequirements.GrowTemperatureC.Min());
		MinIdealGrowTemperatureC = new RequirementWrapper(plant.lifeRequirements.GrowTemperatureC.IdealMin(0f), plant.lifeRequirements.GrowTemperatureC.IdealMin(-1f), plant.lifeRequirements.GrowTemperatureC.IdealMin(1f), plant.lifeRequirements.GrowTemperatureC.IdealMin());
		MaxGrowTemperatureC = new RequirementWrapper(plant.lifeRequirements.GrowTemperatureC.Max(0f), plant.lifeRequirements.GrowTemperatureC.Max(-1f), plant.lifeRequirements.GrowTemperatureC.Max(1f), plant.lifeRequirements.GrowTemperatureC.Max());
		MaxIdealGrowTemperatureC = new RequirementWrapper(plant.lifeRequirements.GrowTemperatureC.IdealMax(0f), plant.lifeRequirements.GrowTemperatureC.IdealMax(-1f), plant.lifeRequirements.GrowTemperatureC.IdealMax(1f), plant.lifeRequirements.GrowTemperatureC.IdealMax());
		MinGrowPressure = new RequirementWrapper(plant.lifeRequirements.GrowPressure.Min(0f), plant.lifeRequirements.GrowPressure.Min(-1f), plant.lifeRequirements.GrowPressure.Min(1f), plant.lifeRequirements.GrowPressure.Min());
		MinIdealGrowPressure = new RequirementWrapper(plant.lifeRequirements.GrowPressure.IdealMin(0f), plant.lifeRequirements.GrowPressure.IdealMin(-1f), plant.lifeRequirements.GrowPressure.IdealMin(1f), plant.lifeRequirements.GrowPressure.IdealMin());
		MaxGrowPressure = new RequirementWrapper(plant.lifeRequirements.GrowPressure.Max(0f), plant.lifeRequirements.GrowPressure.Max(-1f), plant.lifeRequirements.GrowPressure.Max(1f), plant.lifeRequirements.GrowPressure.Max());
		MaxIdealGrowPressure = new RequirementWrapper(plant.lifeRequirements.GrowPressure.IdealMax(0f), plant.lifeRequirements.GrowPressure.IdealMax(-1f), plant.lifeRequirements.GrowPressure.IdealMax(1f), plant.lifeRequirements.GrowPressure.IdealMax());
	}

	public void Read(RocketBinaryReader reader)
	{
		PlantName = reader.ReadString();
		int num = reader.ReadInt32();
		if (Genes == null)
		{
			List<GeneWrapper> list = (Genes = new List<GeneWrapper>());
		}
		for (int i = 0; i < num; i++)
		{
			GeneWrapper geneWrapper = new GeneWrapper();
			geneWrapper.Read(reader);
			Genes.Add(geneWrapper);
		}
		TimeUntilDehydrationDamage = RequirementWrapper.Create(reader);
		TimeUntilUndesiredGasDamage = RequirementWrapper.Create(reader);
		TimeUntilFrozenDamage = RequirementWrapper.Create(reader);
		TimeUntilOverHeatedDamage = RequirementWrapper.Create(reader);
		TimeUntilSuffocatedDamage = RequirementWrapper.Create(reader);
		TimeUntilLowPressureDamage = RequirementWrapper.Create(reader);
		TimeUntilHighPressureDamage = RequirementWrapper.Create(reader);
		LightPerDay = RequirementWrapper.Create(reader);
		DarknessPerDay = RequirementWrapper.Create(reader);
		WaterUsage = RequirementWrapper.Create(reader);
		GasProduction = RequirementWrapper.Create(reader);
		UndesiredGasResistance = RequirementWrapper.Create(reader);
		MinGrowTemperatureC = RequirementWrapper.Create(reader);
		MinIdealGrowTemperatureC = RequirementWrapper.Create(reader);
		MaxGrowTemperatureC = RequirementWrapper.Create(reader);
		MaxIdealGrowTemperatureC = RequirementWrapper.Create(reader);
		MinGrowPressure = RequirementWrapper.Create(reader);
		MinIdealGrowPressure = RequirementWrapper.Create(reader);
		MaxGrowPressure = RequirementWrapper.Create(reader);
		MaxIdealGrowPressure = RequirementWrapper.Create(reader);
		GrowthSpeedMultiplier = RequirementWrapper.Create(reader);
		TimeUntilLightDamage = RequirementWrapper.Create(reader);
		TimeUntilDarknessDamage = RequirementWrapper.Create(reader);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteString(PlantName);
		writer.WriteInt32(Genes.Count);
		foreach (GeneWrapper gene in Genes)
		{
			gene.Write(writer);
		}
		TimeUntilDehydrationDamage.Write(writer);
		TimeUntilUndesiredGasDamage.Write(writer);
		TimeUntilFrozenDamage.Write(writer);
		TimeUntilOverHeatedDamage.Write(writer);
		TimeUntilSuffocatedDamage.Write(writer);
		TimeUntilLowPressureDamage.Write(writer);
		TimeUntilHighPressureDamage.Write(writer);
		LightPerDay.Write(writer);
		DarknessPerDay.Write(writer);
		WaterUsage.Write(writer);
		GasProduction.Write(writer);
		UndesiredGasResistance.Write(writer);
		MinGrowTemperatureC.Write(writer);
		MinIdealGrowTemperatureC.Write(writer);
		MaxGrowTemperatureC.Write(writer);
		MaxIdealGrowTemperatureC.Write(writer);
		MinGrowPressure.Write(writer);
		MinIdealGrowPressure.Write(writer);
		MaxGrowPressure.Write(writer);
		MaxIdealGrowPressure.Write(writer);
		GrowthSpeedMultiplier.Write(writer);
		TimeUntilLightDamage.Write(writer);
		TimeUntilDarknessDamage.Write(writer);
	}
}
