using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Cysharp.Threading.Tasks;
using Open.Nat;

namespace Util.Commands;

public class UpnpCommand : CommandBase
{
	public override string HelpText => "Queries Universal Plug and Play (UPnP) devices on the local network and prints any active port mappings. Requires UPnP to be enabled in settings.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (!Settings.CurrentData.UPNPEnabled)
		{
			ConsoleWindow.PrintError("UPnP is disabled.", suppressStacktrace: true);
			return null;
		}
		UPnP().Forget();
		return null;
	}

	private static async UniTaskVoid UPnP()
	{
		ConsoleWindow.Print("Querying UPnP devices.");
		NatDiscoverer natDiscoverer = new NatDiscoverer();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(5000);
		IEnumerable<NatDevice> enumerable = await natDiscoverer.DiscoverDevicesAsync(PortMapper.Upnp, cancellationTokenSource);
		int devicesFound = 0;
		foreach (NatDevice device in enumerable)
		{
			if (!string.IsNullOrEmpty(Settings.CurrentData.LocalIpAddress))
			{
				if (device.LocalAddress.ToString() != Settings.CurrentData.LocalIpAddress)
				{
					ConsoleWindow.Print($"Skipping {device.LocalAddress} as local ip does not match setting.");
					continue;
				}
			}
			else if (device.LocalAddress.AddressFamily != AddressFamily.InterNetwork)
			{
				continue;
			}
			IPAddress arg = await device.GetExternalIPAsync();
			ConsoleWindow.Print($"Device '{device.LocalAddress}' via '{arg}'.");
			devicesFound++;
			IEnumerable<Mapping> obj = await device.GetAllMappingsAsync();
			int num = 0;
			foreach (Mapping item in obj)
			{
				ConsoleWindow.Print($"{num}.\t{item.Protocol}\t{item.PublicPort}\t{item.Description}");
				num++;
			}
		}
		if (devicesFound == 0)
		{
			ConsoleWindow.PrintError("No UPnP compatible devices found.", suppressStacktrace: true);
		}
		ConsoleWindow.PrintAction($"Finished UPnP device query, '{devicesFound}' devices found.");
	}
}
