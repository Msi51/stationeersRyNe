namespace Objects.Rockets.Log;

public enum RocketEventType
{
	None,
	FuelDepleted,
	BatteryDepleted,
	ReachedDestination,
	ActionReport,
	NavPointChart,
	PipeFail,
	RocketLandAborted,
	RocketCrashed,
	CargoStorageFull,
	Deploy
}
