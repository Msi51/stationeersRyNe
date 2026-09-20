using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Items;
using Trading;

namespace Assets.Scripts.Objects.Clothing;

public interface ISuit : IReferencable, IEvaluable
{
	bool Indestructable { get; }

	Atmosphere InternalAtmosphere { get; }

	List<SkinnedMeshRendererInstance> SkinnedMeshes { get; }

	float HygieneReductionMultiplier { get; }

	float SuitVelocityLeakRatio { get; }

	float SuitVelocityAbsorbed { get; }

	float SuitVelocityScale { get; }

	PressurekPa WasteMaxPressure { get; }

	TemperatureKelvin OutputTemperature { get; set; }

	float OutputSetting { get; set; }

	float LeakRatio { get; set; }

	Thing AsThing { get; }

	IRepairable AsRepairable { get; }

	BatteryCell Battery { get; }

	GasCanister WasteTank { get; }

	GasCanister CoolantTank { get; }

	GasCanister AirTank { get; }

	Slot AirTankSlot { get; }

	Slot WasteTankSlot { get; }

	Slot BatterySlot { get; }

	bool HasWasteTankSlot { get; }

	bool HasFilters { get; }

	bool EmptyFilter { get; }

	bool LowFilter { get; }

	float MovementSpeedMultiplier { get; }

	void RefreshSkinnedMeshCustomColor();

	void UpdateEmptyFilter();

	void ForceClothingVisible(bool show);

	List<Slot> GetFilterSlots();

	string ToTooltip();
}
