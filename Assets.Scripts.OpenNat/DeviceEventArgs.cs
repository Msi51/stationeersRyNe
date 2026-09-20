using System;

namespace Assets.Scripts.OpenNat;

internal class DeviceEventArgs : EventArgs
{
	public NatDevice Device { get; private set; }

	public DeviceEventArgs(NatDevice device)
	{
		Device = device;
	}
}
