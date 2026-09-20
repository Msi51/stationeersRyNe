using System;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;

namespace Util.Commands;

public class PrintPhaseChangeInfoCommand : CommandBase
{
	public override string HelpText => "Prints the triple point and critical point (temperature in Kelvin and pressure in kPa) for every gas, derived from each mole's coefficients.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		Mole[] array = GasMixtureHelper.ReadOnlyMoles(GasMixtureHelper.Create());
		for (int i = 0; i < array.Length; i++)
		{
			Mole mole = array[i];
			ConsoleWindow.Print($"{mole.Type} - TriplePoint: {MoleHelper.EvaporationTemperature(mole.Type, mole.MinLiquidPressure())}K at {mole.MinLiquidPressure()}kPa");
			ConsoleWindow.Print($"{mole.Type} - CriticalPoint: {MoleHelper.EvaporationTemperature(mole.Type, mole.MinimumLiquidPressureAtMaxTemperature())}K at {mole.MinimumLiquidPressureAtMaxTemperature()}kPa");
		}
		return null;
	}
}
