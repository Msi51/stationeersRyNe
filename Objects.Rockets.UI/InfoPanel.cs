using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using UI;
using UnityEngine;

namespace Objects.Rockets.UI;

public class InfoPanel : UserInterfaceBase
{
	[Header("Info Panel")]
	[SerializeField]
	private LabeledTextField _currentLocation;

	[SerializeField]
	private LabeledTextField _targetLocation;

	[SerializeField]
	private LabeledTextField _targetLocationEta;

	[SerializeField]
	private LabeledTextField _nextLocation;

	[SerializeField]
	private LabeledTextField _nextLocationEta;

	[SerializeField]
	private LabeledTextField _apex;

	[SerializeField]
	private LabeledTextField _impactVelocity;

	[SerializeField]
	private LabeledTextField _targetVelocity;

	[SerializeField]
	private LabeledTextField _acceleration;

	[SerializeField]
	private LabeledTextField _engineAcceleration;

	[SerializeField]
	private LabeledTextField _gravity;

	[SerializeField]
	private LabeledTextField _velocity;

	[SerializeField]
	private LabeledTextField _altitude;

	[SerializeField]
	private LabeledTextField _fuelTime;

	public void Refresh(RocketModel rocketModel)
	{
		_currentLocation.SetValue(rocketModel.CurrentLocationName);
		_targetLocation.SetValue(rocketModel.TargetLocationName);
		_nextLocation.SetValue(rocketModel.NextLocationName);
		_nextLocationEta.SetValue(rocketModel.NextLocationEta);
		_targetLocationEta.SetValue(rocketModel.TargetLocationEta);
		_targetLocationEta.SetLabel(rocketModel.TargetLocationEtaLabel);
		_velocity.SetValue(rocketModel.Velocity);
		_velocity.SetLabel(rocketModel.VelocityLabel);
		_impactVelocity.SetValue(rocketModel.ImpactVelocity);
		_acceleration.SetValue(rocketModel.Acceleration);
		_engineAcceleration.SetValue(rocketModel.EngineAcceleration);
		_gravity.SetValue(rocketModel.Gravity);
		_apex.SetValue(rocketModel.Apex);
		_altitude.SetValue(rocketModel.Altitude);
		_targetVelocity.SetValue(rocketModel.TargetVelocity);
		_fuelTime.SetValue(rocketModel.FuelTime);
	}
}
