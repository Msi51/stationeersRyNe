using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class PassiveRadiator : Radiator
{
	public override float ConvectionFactor => 1f;

	public override float RadiationFactor => 0.4f;

	public override float SolarHeatingFactor => 0.2f;

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
