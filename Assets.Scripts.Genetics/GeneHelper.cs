using Assets.Scripts.Localization2;

namespace Assets.Scripts.Genetics;

public static class GeneHelper
{
	public static string DisplayName(Gene gene)
	{
		return gene switch
		{
			Gene.None => string.Empty, 
			Gene.GrowthSpeedMultiplier => GameStrings.GeneGrowthSpeedMultiplier.DisplayString, 
			Gene.DarkPerDay => GameStrings.GeneDarkPerDay.DisplayString, 
			Gene.LightPerDay => GameStrings.GeneLightPerDay.DisplayString, 
			Gene.DroughtTolerance => GameStrings.GeneDroughtTolerance.DisplayString, 
			Gene.WaterUsage => GameStrings.GeneWaterUsage.DisplayString, 
			Gene.LowPressureResistance => GameStrings.GeneLowPressureResistance.DisplayString, 
			Gene.LowTemperatureResistance => GameStrings.GeneLowTemperatureResistance.DisplayString, 
			Gene.UndesiredGasTolerance => GameStrings.GeneUndesiredGasTolerance.DisplayString, 
			Gene.GasProduction => GameStrings.GeneGasProduction.DisplayString, 
			Gene.HighPressureResistance => GameStrings.GeneHighPressureResistance.DisplayString, 
			Gene.HighTemperatureResistance => GameStrings.GeneHighTemperatureResistance.DisplayString, 
			Gene.SuffocationTolerance => GameStrings.GeneSuffocationTolerance.DisplayString, 
			Gene.LowPressureTolerance => GameStrings.GeneLowPressureTolerance.DisplayString, 
			Gene.LowTemperatureTolerance => GameStrings.GeneLowTemperatureTolerance.DisplayString, 
			Gene.HighPressureTolerance => GameStrings.GeneHighPressureTolerance.DisplayString, 
			Gene.HighTemperatureTolerance => GameStrings.GeneHighTemperatureTolerance.DisplayString, 
			Gene.UndesiredGasResistance => GameStrings.GeneUndesiredGasResistance.DisplayString, 
			Gene.LightTolerance => GameStrings.GeneLightTolerance.DisplayString, 
			Gene.DarknessTolerance => GameStrings.GeneDarknessTolerance.DisplayString, 
			_ => string.Empty, 
		};
	}

	public static string Description(Gene gene)
	{
		return gene switch
		{
			Gene.None => string.Empty, 
			Gene.GrowthSpeedMultiplier => GameStrings.GeneDescriptionGrowthSpeedMultiplier.DisplayString, 
			Gene.DarkPerDay => GameStrings.GeneDescriptionDarkPerDay.DisplayString, 
			Gene.LightPerDay => GameStrings.GeneDescriptionLightPerDay.DisplayString, 
			Gene.DroughtTolerance => GameStrings.GeneDescriptionDroughtTolerance.DisplayString, 
			Gene.WaterUsage => GameStrings.GeneDescriptionWaterUsage.DisplayString, 
			Gene.LowPressureResistance => GameStrings.GeneDescriptionLowPressureResistance.DisplayString, 
			Gene.LowTemperatureResistance => GameStrings.GeneDescriptionLowTemperatureResistance.DisplayString, 
			Gene.UndesiredGasTolerance => GameStrings.GeneDescriptionUndesiredGasTolerance.DisplayString, 
			Gene.GasProduction => GameStrings.GeneDescriptionGasProduction.DisplayString, 
			Gene.HighPressureResistance => GameStrings.GeneDescriptionHighPressureResistance.DisplayString, 
			Gene.HighTemperatureResistance => GameStrings.GeneDescriptionHighTemperatureResistance.DisplayString, 
			Gene.SuffocationTolerance => GameStrings.GeneDescriptionSuffocationTolerance.DisplayString, 
			Gene.LowPressureTolerance => GameStrings.GeneDescriptionLowPressureTolerance.DisplayString, 
			Gene.LowTemperatureTolerance => GameStrings.GeneDescriptionLowTemperatureTolerance.DisplayString, 
			Gene.HighPressureTolerance => GameStrings.GeneDescriptionHighPressureTolerance.DisplayString, 
			Gene.HighTemperatureTolerance => GameStrings.GeneDescriptionHighTemperatureTolerance.DisplayString, 
			Gene.UndesiredGasResistance => GameStrings.GeneDescriptionUndesiredGasResistance.DisplayString, 
			Gene.LightTolerance => GameStrings.GeneDescriptionLightTolerance.DisplayString, 
			Gene.DarknessTolerance => GameStrings.GeneDescriptionDarknessTolerance.DisplayString, 
			_ => string.Empty, 
		};
	}
}
