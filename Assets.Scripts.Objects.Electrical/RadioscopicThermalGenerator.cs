using Assets.Scripts.Networks;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Electrical;

public class RadioscopicThermalGenerator : Electrical, IRocketInternals, IRocketComponent
{
	public float PowerGenerated = 50000f;

	public float HeatReleaseOnPowerUse = 1E-07f;

	public float MaxHeat = 320.15f;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		return PowerGenerated;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		base.UsePower(cableNetwork, powerUsed);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
