using Assets.Scripts.Objects.Pipes;

namespace Objects.Rockets.UI.Models;

public struct DeviceModel
{
	public Device Device;

	public long ReferenceId;

	public string DisplayName;

	public bool CanWriteOnOff;

	public bool OnOff;

	public bool Powered;

	public bool HasPowerState;

	public bool Error;

	public bool Pinned;
}
