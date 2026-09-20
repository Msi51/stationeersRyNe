using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networks;

public class WirelessNetwork : CableNetwork
{
	private static readonly Func<CableNetwork, WirelessPower, WirelessNetwork> FindWirelessNetworkAction = delegate(CableNetwork myNetwork, WirelessPower findUsing)
	{
		if (myNetwork is WirelessNetwork wirelessNetwork)
		{
			foreach (Device device in wirelessNetwork.DeviceList)
			{
				if (device == findUsing)
				{
					return wirelessNetwork;
				}
			}
		}
		return (WirelessNetwork)null;
	};

	private static WirelessNetwork _foundNetwork;

	public static WirelessNetwork GetWirelessNetwork(WirelessPower parentTransmitter)
	{
		WirelessNetwork wirelessNetwork = CableNetwork.AllCableNetworks.FindUsing(FindWirelessNetworkAction, parentTransmitter);
		if (wirelessNetwork != null)
		{
			return wirelessNetwork;
		}
		if (!GameManager.RunSimulation)
		{
			return null;
		}
		return new WirelessNetwork(parentTransmitter);
	}

	protected override void RefreshPowerAndDataDeviceLists()
	{
		_dataDeviceList.Clear();
		_powerDeviceList.Clear();
		_powerDeviceList.AddRange(DeviceList);
		PowerDeviceListDirty = false;
	}

	public WirelessNetwork(WirelessPower parentTransmitter)
	{
		AddDevice(parentTransmitter);
		CableNetworkType = CableNetworkType.WirelessNetwork;
	}

	public WirelessNetwork(long referenceId)
		: base(referenceId)
	{
		CableNetworkType = CableNetworkType.WirelessNetwork;
	}

	public override void OnPowerTick()
	{
		if (IsNetworkValid())
		{
			base.OnPowerTick();
		}
	}

	public override bool IsNetworkValid()
	{
		return DeviceList.Count > 0;
	}

	public sealed override void AddDevice(Device device)
	{
		HashSet<Cable> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration == null)
		{
			deviceRegistration = new HashSet<Cable>();
			lock (DeviceList)
			{
				if (!DeviceList.Contains(device))
				{
					DeviceList.Add(device);
				}
			}
			DeviceRegister.Add(device, deviceRegistration);
			Battery battery = device as Battery;
			if (battery != null)
			{
				BatteryList.Add(battery);
			}
			device.ConnectedCableNetworks.Add(this);
		}
		RefreshNetworkDevice(device);
		RefreshNetwork();
		base.AddDevice(device);
	}

	public override void RemoveDevice(Device device)
	{
		base.RemoveDevice(device);
		RefreshNetwork();
	}
}
