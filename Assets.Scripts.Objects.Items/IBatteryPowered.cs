using Assets.Scripts.Util;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IBatteryPowered : IPowered, IDensePoolable, IReferencable, IEvaluable
{
	Slot BatterySlot { get; }

	BatteryCell Battery { get; }

	void Recharge(float amount);
}
