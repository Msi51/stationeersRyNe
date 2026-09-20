using Assets.Scripts.UI;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Pipes;

public class RocketFiltrationMachine : FiltrationMachine, IRocketInternals, IRocketComponent, IRocketMassContributor
{
	public float RocketMass = 20f;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public float MassContribution => RocketMass;

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AtmosDevices);
	}
}
