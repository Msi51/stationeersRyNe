using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Objects.RoboticArm;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PassiveVent : Pipe
{
	public RoboticArmDockAtmos DockedAtmosArm { get; set; }

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void OnAtmosphericTick()
	{
		if (!base.HasOpenGrid)
		{
			return;
		}
		bool flag;
		switch (base.RocketNetwork?.Rocket?.RocketState)
		{
		case RocketState.Launching:
		case RocketState.Landing:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			RegisterCurrentGrids();
		}
		else if ((object)_crewModule == null)
		{
			_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
		}
		if (!(DockedAtmosArm != null) || DockedAtmosArm.ArmState != ArmState.Down)
		{
			Atmosphere workingAtmosphere = GetWorkingAtmosphere();
			if (AtmosphereHelper.IsSubmerged(base.Position, workingAtmosphere))
			{
				AtmosphereHelper.Mix(base.PipeNetwork?.Atmosphere, workingAtmosphere, AtmosphereHelper.MatterState.All);
			}
			else
			{
				AtmosphereHelper.Mix(base.PipeNetwork?.Atmosphere, workingAtmosphere, AtmosphereHelper.MatterState.Gas);
			}
		}
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
