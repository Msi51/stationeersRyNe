using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Pipes;

public class LiquidFiltrationMachine : FiltrationMachine
{
	protected const float VOLUME_PER_TICK = 0.25f;

	public VolumeLitres VolumePerTick => new VolumeLitres(0.25);

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered || Mode == 0 || !IsOperable)
		{
			_powerUsedDuringTick = 0f;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		_powerUsedDuringTick = Mathf.Lerp(0f, FiltrationMachineBase.EnergyPerAtmosphere, (InputNetwork.Atmosphere.TotalVolumeLiquids / VolumePerTick).ToFloat());
		float outputLiquidVolumeRatio = Mathf.Max(OutputNetwork.Atmosphere.LiquidVolumeRatio, OutputNetwork2.Atmosphere.LiquidVolumeRatio);
		MoleQuantity transferMoles;
		GasMixture newGasMix = AtmosphereHelper.TakeNormalisedLiquidVolumeScaled(InputNetwork.Atmosphere, VolumePerTick, outputLiquidVolumeRatio, out transferMoles);
		PressurekPa inputPressureDelta = InputNetwork.Atmosphere.PressureGasses - RocketMath.Max(OutputNetwork.Atmosphere.PressureGasses, OutputNetwork2.Atmosphere.PressureGasses);
		MoleQuantity transferMoles2;
		GasMixture fromMix = AtmosphereHelper.TakeNormalisedGasPressureScaled(InputNetwork.Atmosphere, base.PressurePerTick, inputPressureDelta, out transferMoles2);
		fromMix.Add(newGasMix);
		foreach (Slot slot in Slots)
		{
			if (!fromMix.IsValid)
			{
				return;
			}
			if (slot.Type == Slot.Class.GasFilter && slot.Contains<GasFilter>(out var occupant) && !occupant.IsEmpty)
			{
				occupant.FilterGas(ref fromMix, ref OutputNetwork.Atmosphere.GasMixture, InputNetwork.Atmosphere, FiltrationMachineBase.MinimumRatioToFilterAll);
			}
		}
		base.ProcessedMoles = transferMoles + transferMoles2;
		OutputNetwork2.Atmosphere.Add(fromMix);
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder stringBuilder = new StringBuilder(Localization.GetName(base.InteractMode));
		stringBuilder.Append(" ");
		stringBuilder.AppendLine(ModeStrings[Mode].AsColor("green"));
		stringBuilder.AppendLine(GameStrings.ProcessedMoles.AsString(base.ProcessedMoles.ToFloat().ToStringPrefix("mol", "yellow")));
		return stringBuilder;
	}
}
