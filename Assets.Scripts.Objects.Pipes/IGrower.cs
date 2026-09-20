using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Electrical;
using Objects.Electrical;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IGrower : IReferencable, IEvaluable
{
	float CurrentLightExposure { get; }

	Atmosphere WaterAtmosphere { get; }

	Atmosphere BreathingAtmosphere { get; }

	string CustomName { get; }

	bool IsLitByGrowLight { get; }

	List<GrowLight> LinkedGrowLights { get; }

	PlantToFertiliserSlotMapping PlantToFertiliserSlotMapping(InteractableType interactableType);
}
