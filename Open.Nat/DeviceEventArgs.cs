using System;

namespace Open.Nat;

internal class DeviceEventArgs : EventArgs
{
	public NatDevice Device { get; private set; }

	public DeviceEventArgs(NatDevice device)
	{
		Device = device;
	}
}
