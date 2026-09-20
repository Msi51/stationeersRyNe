using Assets.Scripts.Util;

namespace Assets.Scripts.Networks;

public interface ISolarRadiator : IDensePoolable
{
	bool IsBeingDestroyed { get; }

	bool CalculateSolarEfficiency();
}
