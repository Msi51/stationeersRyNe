using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networks;

public class PowerProvider
{
	public CableNetwork CableNetwork;

	public Device Device;

	public float Energy;

	private readonly float _originalEnergy;

	public float EnergyUsed => _originalEnergy - Energy;

	public PowerProvider(Device device, CableNetwork cableNetwork)
	{
		Device = device;
		CableNetwork = cableNetwork;
		Energy = device.GetGeneratedPower(cableNetwork);
		_originalEnergy = Energy;
	}

	public void ApplyPower()
	{
		if (!(Device == null) && !(EnergyUsed <= 0f))
		{
			Device.UsePower(CableNetwork, EnergyUsed);
		}
	}
}
