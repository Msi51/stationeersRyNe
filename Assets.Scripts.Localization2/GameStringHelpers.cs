using System;
using Assets.Scripts.Genetics;

namespace Assets.Scripts.Localization2;

public static class GameStringHelpers
{
	public static GameString ToGameString(this Gene gene)
	{
		return gene switch
		{
			Gene.GrowthSpeedMultiplier => GameStrings.GeneGrowthSpeedMultiplier, 
			Gene.DarkPerDay => GameStrings.GeneDarkPerDay, 
			Gene.LightPerDay => GameStrings.GeneLightPerDay, 
			Gene.DroughtTolerance => GameStrings.GeneDroughtTolerance, 
			Gene.WaterUsage => GameStrings.GeneWaterUsage, 
			Gene.LowPressureResistance => GameStrings.GeneLowPressureResistance, 
			Gene.LowTemperatureResistance => GameStrings.GeneLowTemperatureResistance, 
			Gene.UndesiredGasTolerance => GameStrings.GeneUndesiredGasTolerance, 
			Gene.GasProduction => GameStrings.GeneGasProduction, 
			Gene.HighPressureResistance => GameStrings.GeneHighPressureResistance, 
			Gene.HighTemperatureResistance => GameStrings.GeneHighTemperatureResistance, 
			Gene.SuffocationTolerance => GameStrings.GeneSuffocationTolerance, 
			Gene.LowPressureTolerance => GameStrings.GeneLowPressureTolerance, 
			Gene.LowTemperatureTolerance => GameStrings.GeneLowTemperatureTolerance, 
			Gene.HighPressureTolerance => GameStrings.GeneHighPressureTolerance, 
			Gene.HighTemperatureTolerance => GameStrings.GeneHighTemperatureTolerance, 
			Gene.UndesiredGasResistance => GameStrings.GeneUndesiredGasResistance, 
			Gene.LightTolerance => GameStrings.GeneLightTolerance, 
			Gene.DarknessTolerance => GameStrings.GeneDarknessTolerance, 
			Gene.None => throw new ArgumentOutOfRangeException("gene", gene, null), 
			_ => throw new ArgumentOutOfRangeException("gene", gene, null), 
		};
	}
}
