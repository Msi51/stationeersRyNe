namespace Objects.Rockets.UI.Models;

public struct RocketModel
{
	public long ReferenceId;

	public string DisplayName;

	public bool AutoShutOff;

	public bool AutoLand;

	public float ReEntryAltitude;

	public float AutoLandConfidenceRatio;

	public string AutoLandConfidenceString;

	public string AutoLandConfidenceToolTip;

	public float RequiredThrustToAutoLand;

	public float ExpectedMaxThrustDuringAutoland;

	public string CurrentLocationName;

	public string TargetLocationName;

	public string NextLocationName;

	public string NextLocationEta;

	public string TargetLocationEta;

	public string TargetLocationEtaLabel;

	public string Velocity;

	public string VelocityLabel;

	public string ImpactVelocity;

	public string Acceleration;

	public string EngineAcceleration;

	public string Gravity;

	public string Apex;

	public string Altitude;

	public string TargetVelocity;

	public string FuelTime;

	public string Mass;

	public string DryMass;

	public string MassToolTip;

	public string DryMassToolTip;

	public string Thrust;

	public string Weight;

	public string ThrustToWeight;

	public string Volatiles;

	public string Oxidizer;

	public string BatteryPercentage;

	public bool IsValid => ReferenceId != 0;
}
