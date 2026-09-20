using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Genetics;
using Genetics;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

[Serializable]
public class PlantLifeRequirements
{
	private static readonly float PlantResiliance = 0.2f;

	public LifeRequirementsData Data;

	public PlantStat WaterUsage = new PlantStat(Gene.WaterUsage);

	public PlantStat GasProduction = new PlantStat(Gene.GasProduction);

	public PlantStat GrowthSpeedMultiplier = new PlantStat(Gene.GrowthSpeedMultiplier);

	public PlantStat TimeUntilDehydrationDamage = new PlantStat(Gene.DroughtTolerance);

	public PlantStat TimeUntilUndesiredGasDamage = new PlantStat(Gene.UndesiredGasTolerance);

	public PlantStat UndesiredGasResistance = new PlantStat(Gene.UndesiredGasResistance);

	public PlantStat TimeUntilFrozenDamage = new PlantStat(Gene.LowTemperatureTolerance);

	public PlantStat TimeUntilOverHeatedDamage = new PlantStat(Gene.HighTemperatureTolerance);

	public PlantStat TimeUntilSuffocatedDamage = new PlantStat(Gene.SuffocationTolerance);

	public PlantStat TimeUntilLowPressureDamage = new PlantStat(Gene.LowPressureTolerance);

	public PlantStat TimeUntilHighPressureDamage = new PlantStat(Gene.HighPressureTolerance);

	public PlantStat TimeUntilLightDamage = new PlantStat(Gene.LightTolerance);

	public PlantStat TimeUntilDarknessDamage = new PlantStat(Gene.DarknessTolerance);

	public PlantStat LightPerDay = new PlantStat(Gene.LightPerDay);

	public PlantStat DarknessPerDay = new PlantStat(Gene.DarkPerDay);

	public MultiPlantStat GrowTemperatureC = new MultiPlantStat(Gene.LowTemperatureResistance, Gene.HighTemperatureResistance);

	public MultiPlantStat GrowPressure = new MultiPlantStat(Gene.LowPressureResistance, Gene.HighPressureResistance);

	private int _defaultLifeRequirementsId = Animator.StringToHash("LifeRequirementsDefault");

	private static readonly float MaxBias = 0.25f;

	private static readonly float PlantGeneticResistance = 0.5f;

	public Plant Plant { get; private set; }

	public AnimationCurve TemperatureCurve { get; private set; }

	public AnimationCurve PressureCurve { get; private set; }

	public MoleQuantity WaterPerTick => new MoleQuantity(Data.LiquidPerTick.Quantity) * (float)WaterUsage;

	private void InitPlantStats(Plant plant)
	{
		if (string.IsNullOrWhiteSpace(plant.LifeRequirementsId) || !DataCollection.TryGet<LifeRequirementsData>(plant.LifeRequirementsId, out var data))
		{
			data = DataCollection.Get<LifeRequirementsData>(_defaultLifeRequirementsId);
		}
		Data = data;
		WaterUsage.Initialize(data.WaterUsage, plant);
		GasProduction.Initialize(data.GasProduction, plant);
		GrowthSpeedMultiplier.Initialize(data.GrowthSpeedMultiplier, plant);
		TimeUntilDehydrationDamage.Initialize(data.TimeUntilDehydrationDamage, plant);
		TimeUntilUndesiredGasDamage.Initialize(data.TimeUntilUndesiredGasDamage, plant);
		UndesiredGasResistance.Initialize(data.UndesiredGasResistance, plant);
		TimeUntilFrozenDamage.Initialize(data.TimeUntilFrozenDamage, plant);
		TimeUntilOverHeatedDamage.Initialize(data.TimeUntilOverHeatedDamage, plant);
		TimeUntilSuffocatedDamage.Initialize(data.TimeUntilSuffocatedDamage, plant);
		TimeUntilLowPressureDamage.Initialize(data.TimeUntilLowPressureDamage, plant);
		TimeUntilHighPressureDamage.Initialize(data.TimeUntilHighPressureDamage, plant);
		TimeUntilLightDamage.Initialize(data.TimeUntilLightDamage, plant);
		TimeUntilDarknessDamage.Initialize(data.TimeUntilDarknessDamage, plant);
		LightPerDay.Initialize(data.LightPerDay, plant);
		DarknessPerDay.Initialize(data.DarknessPerDay, plant);
		GrowTemperatureC.Initialize(data.GrowTemperatureC, plant);
		GrowPressure.Initialize(data.GrowPressure, plant);
	}

	public void AddToStationpedia(ref StationpediaPage page)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		foreach (GasQuantityRatioData inhaledGase in Data.InhaledGases)
		{
			text = text + "<color=green><link=Gas" + inhaledGase.Type.ToString() + ">" + Localization.GetName(inhaledGase.Type) + "</link></color> <color=orange>" + ValueDisplay.GetUnitValue(inhaledGase.Quantity, ValueDisplay.Unit.MolesPerHour) + "</color> \n";
		}
		foreach (GasQuantityData exhaledGase in Data.ExhaledGases)
		{
			text2 = text2 + "<color=green><link=Gas" + exhaledGase.Type.ToString() + ">" + Localization.GetName(exhaledGase.Type) + "</link></color> <color=orange>" + ValueDisplay.GetUnitValue(exhaledGase.Quantity, ValueDisplay.Unit.MolesPerHour) + "</color> \n";
		}
		StationLifeRequirement stationLifeRequirement = new StationLifeRequirement();
		StationLifeRequirement stationLifeRequirement2 = new StationLifeRequirement();
		stationLifeRequirement.Name = GameStrings.InhaledGassesStationpedia;
		stationLifeRequirement.Value = text;
		stationLifeRequirement.Gene = "<link=Gene" + Gene.GasProduction.ToString() + "><color=green>" + GeneHelper.DisplayName(Gene.GasProduction) + "</color></link>";
		stationLifeRequirement.ValueSize = ((Data.InhaledGases.Count > 1) ? 12 : 18);
		StationLifeRequirement stationLifeRequirement3 = new StationLifeRequirement();
		stationLifeRequirement2.Name = GameStrings.ExhaledGassesStationpedia;
		stationLifeRequirement2.Value = text2;
		stationLifeRequirement2.Gene = "<link=Gene" + Gene.GasProduction.ToString() + "><color=green>" + GeneHelper.DisplayName(Gene.GasProduction) + "</color></link>";
		stationLifeRequirement2.ValueSize = ((Data.ExhaledGases.Count > 1) ? 12 : 18);
		string text3 = string.Empty;
		foreach (GasPressureData harmfulGase in Data.HarmfulGases)
		{
			text3 += $"<link=Gas{harmfulGase.Type}><color=green>{Localization.GetName(harmfulGase.Type)}</color></link>, ";
		}
		stationLifeRequirement3.Name = GameStrings.ToxicGassesStatiopedia;
		stationLifeRequirement3.Value = text3;
		stationLifeRequirement3.Gene = "<link=Gene" + Gene.UndesiredGasResistance.ToString() + "><color=green>" + GeneHelper.DisplayName(Gene.UndesiredGasResistance) + "</color></link>";
		page.LifeRequirements.Add(stationLifeRequirement);
		page.LifeRequirements.Add(stationLifeRequirement2);
		page.LifeRequirements.Add(stationLifeRequirement3);
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.WaterUsage.DisplayString, ValueDisplay.GetUnitValue(Data.LiquidPerTick.Quantity, ValueDisplay.Unit.MolesPerHour), Gene.WaterUsage));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MinGrowTemperature.DisplayString, ValueDisplay.GetUnitValue(GrowTemperatureC.Min(0f), ValueDisplay.Unit.TemperatureC), Gene.LowTemperatureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MaxGrowTemperature.DisplayString, ValueDisplay.GetUnitValue(GrowTemperatureC.Max(0f), ValueDisplay.Unit.TemperatureC), Gene.HighTemperatureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MinIdealGrowTemperature.DisplayString, ValueDisplay.GetUnitValue(GrowTemperatureC.IdealMin(0f), ValueDisplay.Unit.TemperatureC), Gene.LowTemperatureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MaxIdealGrowTemperature.DisplayString, ValueDisplay.GetUnitValue(GrowTemperatureC.IdealMax(0f), ValueDisplay.Unit.TemperatureC), Gene.HighTemperatureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MinGrowPressure.DisplayString, ValueDisplay.GetUnitValue(GrowPressure.Min(0f), ValueDisplay.Unit.PressureKpa), Gene.LowPressureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MaxGrowPressure.DisplayString, ValueDisplay.GetUnitValue(GrowPressure.Max(0f), ValueDisplay.Unit.PressureKpa), Gene.HighPressureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MinIdealGrowPressure.DisplayString, ValueDisplay.GetUnitValue(GrowPressure.IdealMin(0f), ValueDisplay.Unit.PressureKpa), Gene.LowPressureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.MaxIdealGrowPressure.DisplayString, ValueDisplay.GetUnitValue(GrowPressure.IdealMax(0f), ValueDisplay.Unit.PressureKpa), Gene.HighPressureResistance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.LightPerDay.DisplayString, ValueDisplay.GetUnitValue(LightPerDay.Base, ValueDisplay.Unit.Time), Gene.LightPerDay));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.DarknessPerDay.DisplayString, ValueDisplay.GetUnitValue(DarknessPerDay.Base, ValueDisplay.Unit.Time), Gene.DarkPerDay));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilUndesiredGasDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilUndesiredGasDamage.Base, ValueDisplay.Unit.Time), Gene.UndesiredGasTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilDehydrationDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilDehydrationDamage.Base, ValueDisplay.Unit.Time), Gene.DroughtTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilFrozenDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilFrozenDamage.Base, ValueDisplay.Unit.Time), Gene.LowTemperatureTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilOverheatDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilOverHeatedDamage.Base, ValueDisplay.Unit.Time), Gene.HighTemperatureTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilSuffocateDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilSuffocatedDamage.Base, ValueDisplay.Unit.Time), Gene.SuffocationTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilLowPressureDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilLowPressureDamage.Base, ValueDisplay.Unit.Time), Gene.LowPressureTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilHighPressureDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilHighPressureDamage.Base, ValueDisplay.Unit.Time), Gene.HighPressureTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilLightDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilLightDamage.Base, ValueDisplay.Unit.Time), Gene.LightTolerance));
		page.LifeRequirements.Add(new StationLifeRequirement(GameStrings.TimeUntilDarknessDamage.DisplayString, ValueDisplay.GetUnitValue(TimeUntilDarknessDamage.Base, ValueDisplay.Unit.Time), Gene.DarknessTolerance));
	}

	public void UpdateTemperaturePressureCurves()
	{
		TemperatureCurve = new AnimationCurve(new Keyframe(GrowTemperatureC.Min(), 0f), new Keyframe(GrowTemperatureC.IdealMin(), 1f), new Keyframe(GrowTemperatureC.IdealMax(), 1f), new Keyframe(GrowTemperatureC.Max(), 0f));
		PressureCurve = new AnimationCurve(new Keyframe(GrowPressure.Min(), 0f), new Keyframe(GrowPressure.IdealMin(), 1f), new Keyframe(GrowPressure.IdealMax(), 1f), new Keyframe(GrowPressure.Max(), 0f));
	}

	public void SetHarvestQuantityOnMature()
	{
		PlantRecord plantRecord = Plant.PlantRecord;
		float num = 0f;
		num += plantRecord.TimeDehydrated / (float)TimeUntilDehydrationDamage;
		num += plantRecord.TimeFrozen / (float)TimeUntilFrozenDamage;
		num += plantRecord.TimeOverHeated / (float)TimeUntilOverHeatedDamage;
		num += plantRecord.TimeSuffocated / (float)TimeUntilSuffocatedDamage;
		num += plantRecord.TimeLowPressure / (float)TimeUntilLowPressureDamage;
		num += plantRecord.TimeHighPressure / (float)TimeUntilHighPressureDamage;
		num += plantRecord.TimePolluted / (float)TimeUntilUndesiredGasDamage;
		num += plantRecord.LightStress;
		num *= PlantResiliance;
		float num2 = (float)Plant.HarvestQuantityMax + Plant.FertilizerHarvestQuantityBoost - num;
		int num3 = Mathf.FloorToInt(num2);
		float num4 = num2 - (float)num3;
		if (UnityEngine.Random.Range(0f, 1f) < num4)
		{
			num3++;
		}
		Plant.HarvestQuantity = Mathf.Max(num3, 1);
	}

	public float GrowthEfficiency()
	{
		float num = Plant.PlantStatus.BreathingEfficiency * Plant.PlantStatus.LightEfficiency * Plant.PlantStatus.TemperatureEfficiency * Plant.PlantStatus.HydrationEfficiency * Plant.GrowthEfficiencyRNG * Plant.PlantStatus.PressureEfficiency * (float)Plant.lifeRequirements.GrowthSpeedMultiplier;
		if (float.IsNaN(num))
		{
			return 1f;
		}
		return num;
	}

	public void SetOwnerPlant(Plant plant)
	{
		Plant = plant;
		InitPlantStats(plant);
	}

	public float GetMutationBias(Gene gene)
	{
		switch (gene)
		{
		case Gene.None:
			return 0f;
		case Gene.DroughtTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeDehydrated / (float)TimeUntilDehydrationDamage * PlantGeneticResistance);
		case Gene.WaterUsage:
			return Mathf.Lerp(0f, MaxBias * -1f, Plant.PlantRecord.TimeDehydrated / (float)TimeUntilDehydrationDamage * PlantGeneticResistance);
		case Gene.LowPressureResistance:
		case Gene.LowPressureTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeLowPressure / (float)TimeUntilLowPressureDamage * PlantGeneticResistance);
		case Gene.LowTemperatureResistance:
		case Gene.LowTemperatureTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeFrozen / (float)TimeUntilFrozenDamage * PlantGeneticResistance);
		case Gene.HighPressureResistance:
		case Gene.HighPressureTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeHighPressure / (float)TimeUntilHighPressureDamage * PlantGeneticResistance);
		case Gene.HighTemperatureResistance:
		case Gene.HighTemperatureTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeOverHeated / (float)TimeUntilOverHeatedDamage * PlantGeneticResistance);
		case Gene.UndesiredGasTolerance:
		case Gene.UndesiredGasResistance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimePolluted / (float)TimeUntilUndesiredGasDamage * PlantGeneticResistance);
		case Gene.GasProduction:
		case Gene.SuffocationTolerance:
			return Mathf.Lerp(0f, MaxBias, Plant.PlantRecord.TimeSuffocated / (float)TimeUntilSuffocatedDamage * PlantGeneticResistance);
		default:
			return 0f;
		}
	}

	public void PrintDebugInfo()
	{
		try
		{
			ConsoleWindow.Print("");
			ConsoleWindow.Print("Plant Life Requirements:");
			ConsoleWindow.Print($"Growth Efficiency: {GrowthEfficiency()}");
			ConsoleWindow.Print("");
		}
		catch (Exception)
		{
		}
	}
}
