using Assets.Scripts.Atmospherics;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IPoweredVent : IReferencable, IEvaluable
{
	Interactable InteractMode { get; }

	Interactable InteractOnOff { get; }

	Interactable InteractLock { get; }

	PressurekPa InternalPressure { get; set; }

	PressurekPa ExternalPressure { get; set; }

	bool IsLocked { get; }

	VentDirection VentDirection { get; }

	void ResetVent();
}
