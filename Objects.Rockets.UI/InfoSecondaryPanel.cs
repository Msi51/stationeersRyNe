using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using UI;
using UI.Tooltips;
using UnityEngine;

namespace Objects.Rockets.UI;

public class InfoSecondaryPanel : UserInterfaceBase
{
	[Header("Info Panel")]
	[SerializeField]
	private LabeledTextField _mass;

	[SerializeField]
	private LabeledTextField _dryMass;

	[SerializeField]
	private LabeledTextField _thrust;

	[SerializeField]
	private LabeledTextField _weight;

	[SerializeField]
	private LabeledTextField _thrustToWeight;

	[SerializeField]
	private LabeledTextField _volatiles;

	[SerializeField]
	private LabeledTextField _oxidizer;

	[SerializeField]
	private LabeledTextField _batteryPercentage;

	[SerializeField]
	private UITooltip _massTooltip;

	[SerializeField]
	private UITooltip _dryMassTooltip;

	public void Refresh(RocketModel rocketModel)
	{
		_mass.SetValue(rocketModel.Mass);
		_dryMass.SetValue(rocketModel.DryMass);
		_thrust.SetValue(rocketModel.Thrust);
		_weight.SetValue(rocketModel.Weight);
		_thrustToWeight.SetValue(rocketModel.ThrustToWeight);
		_volatiles.SetValue(rocketModel.Volatiles);
		_oxidizer.SetValue(rocketModel.Oxidizer);
		_batteryPercentage.SetValue(rocketModel.BatteryPercentage);
		_massTooltip.TooltipText = rocketModel.MassToolTip;
		_dryMassTooltip.TooltipText = rocketModel.DryMassToolTip;
	}
}
