using System;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public class CombustionResult
{
	public readonly MoleQuantity FuelMoleCount;

	public readonly MoleQuantity OxidiserMoleCount;

	public readonly CombustionValue[] Outputs;

	public readonly MoleQuantity OxidiserRatio;

	public readonly MoleQuantity FuelRatio;

	public static readonly CombustionResult Invalid = new CombustionResult(0.0, 0.0, null);

	public CombustionResult(double fuelCount, double oxidiserCount, CombustionValue[] outputs)
	{
		FuelMoleCount = new MoleQuantity(fuelCount);
		OxidiserMoleCount = new MoleQuantity(oxidiserCount);
		Outputs = outputs;
		OxidiserRatio = OxidiserMoleCount / FuelMoleCount;
		FuelRatio = FuelMoleCount / OxidiserMoleCount;
	}

	public bool IsValid()
	{
		return IsValid(this);
	}

	public static bool IsValid(CombustionResult result)
	{
		if (result != null)
		{
			CombustionValue[] outputs = result.Outputs;
			if (outputs != null && outputs.Length != 0 && result.FuelMoleCount > MoleQuantity.Zero)
			{
				return result.OxidiserMoleCount > MoleQuantity.Zero;
			}
		}
		return false;
	}

	public GasMixture RunCombustion(Mole fuel, Mole oxidiser, double combustionRatio, out MoleEnergy combustionEnergy, out MoleQuantity burnedFuel, out float cleanBurnRatio)
	{
		GasMixture result = GasMixtureHelper.Create();
		combustionEnergy = MoleEnergy.Zero;
		MoleQuantity moleQuantity = fuel.Quantity / FuelMoleCount;
		MoleQuantity moleQuantity2 = oxidiser.Quantity / OxidiserMoleCount;
		MoleQuantity moleQuantity3 = RocketMath.Min(moleQuantity, moleQuantity2) * combustionRatio;
		if (moleQuantity3 <= MoleQuantity.Zero)
		{
			result.Add(fuel);
			result.Add(oxidiser);
			burnedFuel = MoleQuantity.Zero;
			cleanBurnRatio = 1f;
			return result;
		}
		MoleQuantity moleQuantity4 = moleQuantity3 * FuelMoleCount;
		MoleQuantity moleQuantity5 = moleQuantity3 * OxidiserMoleCount;
		Mole mole = fuel.Remove(moleQuantity4);
		Mole mole2 = oxidiser.Remove(moleQuantity5);
		result.Add(fuel);
		result.Add(oxidiser);
		CombustionValue[] outputs = Outputs;
		for (int i = 0; i < outputs.Length; i++)
		{
			CombustionValue combustionValue = outputs[i];
			MoleQuantity moleQuantity6 = combustionValue.Quantity * moleQuantity3;
			if (moleQuantity6 > MoleQuantity.Zero)
			{
				result.SetMoleValue(combustionValue.GasType, moleQuantity6, MoleEnergy.Zero);
			}
		}
		combustionEnergy = new MoleEnergy(mole.Enthalpy() * mole2.EnthalpyMultiplier * mole.Quantity.ToDouble());
		if (mole2.Enthalpy() > 0.0)
		{
			combustionEnergy += new MoleEnergy(mole2.Enthalpy() * mole2.EnthalpyMultiplier * mole2.Quantity.ToDouble());
		}
		if (fuel.MatterState() == AtmosphereHelper.MatterState.Liquid)
		{
			combustionEnergy -= IdealGas.Energy(moleQuantity4, fuel.LatentHeatOfVaporization());
		}
		if (oxidiser.MatterState() == AtmosphereHelper.MatterState.Liquid)
		{
			combustionEnergy -= IdealGas.Energy(moleQuantity5, oxidiser.LatentHeatOfVaporization());
		}
		result.TotalEnergy += combustionEnergy + mole.Energy + mole2.Energy;
		burnedFuel = mole.Quantity;
		cleanBurnRatio = Math.Min(1f, (moleQuantity2 / moleQuantity).ToFloat());
		return result;
	}
}
