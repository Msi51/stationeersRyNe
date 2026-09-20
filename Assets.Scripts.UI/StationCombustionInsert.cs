using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.UI;

public class StationCombustionInsert
{
	public Chemistry.GasType FuelType;

	public Chemistry.GasType OxidiserType;

	public StationCombustionInsert()
	{
	}

	public StationCombustionInsert(Chemistry.GasType fuel, Chemistry.GasType oxidiser)
	{
		FuelType = fuel;
		OxidiserType = oxidiser;
	}

	public void ApplyTo(SPDACombustionItem insert)
	{
		if (Combustion.TryGetResult(FuelType, OxidiserType, out var result))
		{
			insert.Populate(FuelType, OxidiserType, result);
		}
	}
}
