using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Atmospherics;

public interface IInternalConditioner
{
	int Exporting { get; }

	int Importing { get; }

	bool OnOff { get; }

	BatteryCell Battery { get; }

	GasCanister WasteTank { get; }

	GasCanister AirTank { get; }

	float Efficiency { get; }

	Atmosphere GetInternalAtmosphere();

	void SetInternalAtmosphere(Atmosphere atmosphere);

	float GetOutputSetting();

	TemperatureKelvin GetOutputTemperature();

	PressurekPa GetPressurePerTick();

	PressurekPa GetWasteMaxPressure();

	MoleEnergy GetMaxEnergy();

	List<Slot> GetFilterSlots();

	void SetConditioningHandler(IInternalConditionerHandler handler);
}
