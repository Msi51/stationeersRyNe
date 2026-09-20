using Assets.Scripts.UI;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Electrical;

public class RocketCircuitHousing : CircuitHousing, IRocketInternals, IRocketComponent
{
	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicIntegratedCircuitsCategory);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
