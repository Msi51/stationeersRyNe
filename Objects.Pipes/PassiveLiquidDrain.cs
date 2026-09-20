using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Objects.Pipes;

public class PassiveLiquidDrain : DevicePipeMounted, IRocketInternals, IRocketComponent
{
	public new RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public new bool StrictlyInternal => false;

	public new RocketNetwork RocketNetwork { get; set; }

	public override void OnAtmosphericTick()
	{
		if (base.NetworkAtmosphere != null && !(base.NetworkAtmosphere.GasMixture.GetTotalMolesLiquids <= MoleQuantity.Zero) && base.HasOpenGrid)
		{
			Atmosphere toAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			VolumeLitres maxVolumeToMove = RocketMath.Min(Chemistry.PipeVolume, base.NetworkAtmosphere.TotalVolumeLiquids / 2.0);
			AtmosphereHelper.DrainLiquids(base.NetworkAtmosphere, toAtmos, maxVolumeToMove);
		}
	}

	public new void OnLaunch(bool immediate = false)
	{
	}

	public new void OnLanded(bool immediate = false)
	{
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (base.HasOpenGrid && !Cell.IsInCrewModule(base.WorldGrid, out var _))
		{
			return passiveTooltip;
		}
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		return passiveTooltip;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!base.GridController.CanContainAtmos(base.WorldGrid))
		{
			extendedText.AppendLine(GameStrings.DeviceWorldGridBlocked.AsString(ToTooltip()));
			return extendedText;
		}
		if (Cell.IsInCrewModule(base.WorldGrid, out var crewModule))
		{
			extendedText.AppendLine(GameStrings.DeviceOutputCrewModule.AsString(crewModule.ToTooltip()));
		}
		return extendedText;
	}
}
