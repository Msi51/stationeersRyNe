using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Reagents;

namespace Assets.Scripts.Objects.Pipes;

public class Furnace : FurnaceBase
{
	public static IQuantityRecipeComparable RecipeComparable = new IQuantityRecipeComparable("Furnace");

	public override void HandleGasInput()
	{
		if (OutputNetwork == null)
		{
			return;
		}
		if (OutputNetwork.Atmosphere.PressureGassesAndLiquids > base.InternalAtmosphere.PressureGassesAndLiquids)
		{
			AtmosphereHelper.EqualizeBothWays(OutputNetwork.Atmosphere, base.InternalAtmosphere, AtmosphereHelper.MatterState.Gas, base.PressurePerTick, MoleQuantity.MaxValue);
			return;
		}
		PressurekPa pressurekPa = RocketMath.Min(base.PressurePerTick, base.InternalAtmosphere.PressureGassesAndLiquids - OutputNetwork.Atmosphere.PressureGassesAndLiquids);
		if (!(pressurekPa <= PressurekPa.Zero))
		{
			Atmosphere atmosphere = ((base.InternalAtmosphere.Volume > OutputNetwork.Atmosphere.Volume) ? base.InternalAtmosphere : OutputNetwork.Atmosphere);
			MoleQuantity moleQuantity = IdealGas.Quantity(pressurekPa, RocketMath.Min(base.InternalAtmosphere.Volume, OutputNetwork.Atmosphere.Volume), atmosphere.Temperature);
			if (moleQuantity > MoleQuantity.Zero)
			{
				Mole mole = base.InternalAtmosphere.GasMixture.Pollutant.Remove(moleQuantity);
				OutputNetwork.Atmosphere.GasMixture.Pollutant.Add(mole);
			}
		}
	}

	public override void HandleGasOutput()
	{
		if (OutputNetwork != null)
		{
			MoveToEqualizeGases(base.InternalAtmosphere, OutputNetwork.Atmosphere);
		}
		if (OutputNetwork2 != null)
		{
			MoveToEqualizeLiquids(base.InternalAtmosphere, OutputNetwork2.Atmosphere);
		}
		if (InputNetwork != null)
		{
			MoveToEqualizeLiquids(InputNetwork.Atmosphere, base.InternalAtmosphere);
			MoveToEqualizeGases(InputNetwork.Atmosphere, base.InternalAtmosphere);
		}
	}

	public override IQuantity GetSmelterResult()
	{
		if (ReagentMixture.TotalReagents <= 0.0)
		{
			return null;
		}
		RatioMix = ReagentMixture.GetRatioMixture();
		CurrentRecipe = RecipeComparable.GetCleanRecipe(new Recipe(RatioMix, base.InternalAtmosphere));
		RecipeComparable.Recipes.TryGetValue(CurrentRecipe, out var value);
		return value;
	}

	public override float GetSmelterScale()
	{
		return RecipeComparable.GetOutputScale(CurrentRecipe);
	}
}
