using System;
using System.Collections.Generic;

namespace Assets.Scripts.Atmospherics;

public static class Combustion
{
	public static Chemistry.GasType[] Oxidisers = new Chemistry.GasType[6]
	{
		Chemistry.GasType.Oxygen,
		Chemistry.GasType.LiquidOxygen,
		Chemistry.GasType.NitrousOxide,
		Chemistry.GasType.LiquidNitrousOxide,
		Chemistry.GasType.Ozone,
		Chemistry.GasType.LiquidOzone
	};

	public static Chemistry.GasType[] Fuels = new Chemistry.GasType[5]
	{
		Chemistry.GasType.Methane,
		Chemistry.GasType.LiquidMethane,
		Chemistry.GasType.Hydrogen,
		Chemistry.GasType.LiquidHydrogen,
		Chemistry.GasType.LiquidAlcohol
	};

	public static Chemistry.GasType[] Hypergolics = new Chemistry.GasType[2]
	{
		Chemistry.GasType.Hydrazine,
		Chemistry.GasType.LiquidHydrazine
	};

	public static readonly CombustionResult ResultMethaneOxygen = new CombustionResult(2.0, 1.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.Pollutant, 3.0),
		new CombustionValue(Chemistry.GasType.CarbonDioxide, 6.0)
	});

	public static readonly CombustionResult ResultMethaneNitrous = new CombustionResult(1.0, 1.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.CarbonDioxide, 2.0),
		new CombustionValue(Chemistry.GasType.Nitrogen, 2.0)
	});

	public static readonly CombustionResult ResultMethaneOzone = new CombustionResult(3.0, 2.0, new CombustionValue[3]
	{
		new CombustionValue(Chemistry.GasType.Pollutant, 3.0),
		new CombustionValue(Chemistry.GasType.CarbonDioxide, 6.0),
		new CombustionValue(Chemistry.GasType.Steam, 1.0)
	});

	public static readonly CombustionResult ResultHydrogenOxygen = new CombustionResult(2.0, 1.0, new CombustionValue[1]
	{
		new CombustionValue(Chemistry.GasType.Steam, 3.0)
	});

	public static readonly CombustionResult ResultHydrogenNitrous = new CombustionResult(1.0, 1.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.Steam, 1.0),
		new CombustionValue(Chemistry.GasType.Nitrogen, 1.0)
	});

	public static readonly CombustionResult ResultHydrogenOzone = new CombustionResult(3.0, 1.0, new CombustionValue[1]
	{
		new CombustionValue(Chemistry.GasType.Steam, 4.0)
	});

	public static readonly CombustionResult ResultAlcoholOxygen = new CombustionResult(1.0, 3.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.CarbonDioxide, 8.0),
		new CombustionValue(Chemistry.GasType.Steam, 2.0)
	});

	public static readonly CombustionResult ResultAlcoholNitrous = new CombustionResult(1.0, 2.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.Nitrogen, 4.0),
		new CombustionValue(Chemistry.GasType.Steam, 2.0)
	});

	public static readonly CombustionResult ResultAlcoholOzone = new CombustionResult(1.0, 2.0, new CombustionValue[2]
	{
		new CombustionValue(Chemistry.GasType.CarbonDioxide, 1.0),
		new CombustionValue(Chemistry.GasType.Steam, 3.0)
	});

	public static readonly CombustionResult ResultHydrazine = new CombustionResult(1.0, 1.0, new CombustionValue[1]
	{
		new CombustionValue(Chemistry.GasType.Pollutant, 8.0)
	});

	private static readonly CombustionResult[,] Data = new CombustionResult[4, 4]
	{
		{ ResultMethaneOxygen, ResultMethaneNitrous, ResultMethaneOzone, null },
		{ ResultHydrogenOxygen, ResultHydrogenNitrous, ResultHydrogenOzone, null },
		{ ResultAlcoholOxygen, ResultAlcoholNitrous, ResultAlcoholOzone, null },
		{ null, null, null, ResultHydrazine }
	};

	public static bool IsOxidiser(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.LiquidOxygen:
		case Chemistry.GasType.LiquidNitrousOxide:
		case Chemistry.GasType.Ozone:
		case Chemistry.GasType.LiquidOzone:
			return true;
		default:
			return false;
		}
	}

	public static bool IsFuel(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.LiquidMethane:
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.LiquidHydrogen:
		case Chemistry.GasType.LiquidAlcohol:
			return true;
		default:
			return false;
		}
	}

	public static bool IsHypergolic(Chemistry.GasType gasType)
	{
		if (gasType == Chemistry.GasType.Hydrazine || gasType == Chemistry.GasType.LiquidHydrazine)
		{
			return true;
		}
		return false;
	}

	public static GasMixture CombustMoles(Mole fuel, Mole oxidiser, double combustionRatio, out MoleEnergy combustionEnergy, out MoleQuantity combustedFuel, out float cleanBurnRatio)
	{
		CombustionResult combustionResult = ((fuel.IsValid && oxidiser.IsValid) ? Data[FuelIndex(fuel.Type), OxidiserIndex(oxidiser.Type)] : CombustionResult.Invalid);
		if (!CombustionResult.IsValid(combustionResult))
		{
			GasMixture result = GasMixtureHelper.Create();
			result.Add(fuel);
			result.Add(oxidiser);
			combustionEnergy = MoleEnergy.Zero;
			combustedFuel = MoleQuantity.Zero;
			cleanBurnRatio = 1f;
			return result;
		}
		return combustionResult.RunCombustion(fuel, oxidiser, combustionRatio, out combustionEnergy, out combustedFuel, out cleanBurnRatio);
	}

	public static bool TryGetResult(Mole fuel, Mole oxidiser, out CombustionResult result)
	{
		return TryGetResult(fuel.Type, oxidiser.Type, out result);
	}

	public static bool TryGetResult(Chemistry.GasType fuelType, Chemistry.GasType oxidiserType, out CombustionResult result)
	{
		result = CombustionResult.Invalid;
		int num = FuelIndex(fuelType);
		int num2 = OxidiserIndex(oxidiserType);
		if (num == -1 || num2 == -1)
		{
			return false;
		}
		result = Data[FuelIndex(fuelType), OxidiserIndex(oxidiserType)];
		return result.IsValid();
	}

	public static CombustionResult GetResult(Chemistry.GasType fuelType, Chemistry.GasType oxidiserType)
	{
		CombustionResult combustionResult = Data[FuelIndex(fuelType), OxidiserIndex(oxidiserType)];
		if (!combustionResult.IsValid())
		{
			throw new NotImplementedException($"Combustion result for {fuelType} {oxidiserType} is not implemented");
		}
		return combustionResult;
	}

	public static bool TryGetResults(Chemistry.GasType product, out List<CombustionResult> results)
	{
		results = null;
		CombustionResult[,] data = Data;
		foreach (CombustionResult combustionResult in data)
		{
			if (combustionResult == null)
			{
				continue;
			}
			CombustionValue[] outputs = combustionResult.Outputs;
			for (int k = 0; k < outputs.Length; k++)
			{
				if (outputs[k].GasType == product)
				{
					if (results == null)
					{
						results = new List<CombustionResult>(6);
					}
					results.Add(combustionResult);
				}
			}
		}
		return results != null;
	}

	public static int FuelIndex(Chemistry.GasType type)
	{
		switch (type)
		{
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.LiquidMethane:
			return 0;
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.LiquidHydrogen:
			return 1;
		case Chemistry.GasType.LiquidAlcohol:
			return 2;
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.LiquidHydrazine:
			return 3;
		default:
			return -1;
		}
	}

	public static int OxidiserIndex(Chemistry.GasType type)
	{
		switch (type)
		{
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.LiquidOxygen:
			return 0;
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.LiquidNitrousOxide:
			return 1;
		case Chemistry.GasType.Ozone:
		case Chemistry.GasType.LiquidOzone:
			return 2;
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.LiquidHydrazine:
			return 3;
		default:
			return -1;
		}
	}
}
