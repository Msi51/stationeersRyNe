using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public abstract class PressureFedEngine : RocketEngineBase
{
	protected virtual PressurekPa MAXPressurePerTick => new PressurekPa(5000.0);

	protected void MoveGas(Atmosphere input, Atmosphere output, float throttle, PressurekPa pressureMovedPerTick, Pipe.ContentType pipeType)
	{
		MoleQuantity moleQuantity = LimitedMolesPerTick(input, pressureMovedPerTick, pipeType);
		moleQuantity *= (double)(throttle / 100f);
		if (!(moleQuantity <= MoleQuantity.Zero))
		{
			GasMixture gasMixture = input.Remove(moleQuantity, AtmosphereHelper.MatterState.All);
			output.Add(gasMixture);
		}
	}

	private PressurekPa MaxPressurePerTick(PressurekPa inputPressure, PressurekPa maxPipePressure)
	{
		float num = RocketMath.Clamp(inputPressure / maxPipePressure, PressurekPa.Zero, PressurekPa.One).ToFloat();
		float num2 = 3f + -2.999f / (1f + Mathf.Pow(num / 2f, 0.7f));
		return MAXPressurePerTick * num2;
	}

	private MoleQuantity LimitedMolesPerTick(Atmosphere input, PressurekPa pressureMovedPerTick, Pipe.ContentType pipeType)
	{
		PressurekPa maxPipePressure = ((pipeType == Pipe.ContentType.Gas) ? Chemistry.Limits.MAXPressureGasPipe : Chemistry.Limits.MAXPressureLiquidPipe);
		return IdealGas.Quantity(RocketMath.MapToScale(PressurekPa.Zero, Chemistry.Limits.MAXPressureGasPipe, pressureMovedPerTick, MaxPressurePerTick(input.PressureGasses, maxPipePressure), input.PressureGasses), Chemistry.PipeVolume, input.Temperature);
	}
}
