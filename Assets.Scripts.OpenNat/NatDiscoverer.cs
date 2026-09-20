using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.OpenNat;

public class NatDiscoverer : Singleton<NatDiscoverer>
{
	private static readonly Dictionary<string, NatDevice> Devices = new Dictionary<string, NatDevice>();

	public int GamePort = 7777;

	public int QueryPort = 7778;

	private Coroutine _discoverCoroutine;

	private List<NatDevice> searcherTasks = new List<NatDevice>();

	public void DiscoverDeviceAsync()
	{
		_discoverCoroutine = StartCoroutine(DiscoverDevicesAsync(PortMapper.Upnp, 3f));
	}

	public void DisableDeviceAsync()
	{
		if (_discoverCoroutine != null)
		{
			StopCoroutine(_discoverCoroutine);
		}
	}

	public IEnumerator DiscoverDevicesAsync(PortMapper portMapper, float timeout)
	{
		yield return Yielders.EndOfFrame;
		Guard.IsTrue(portMapper.HasFlag(PortMapper.Upnp), "portMapper");
		ConsoleWindow.PrintAction("Start UPNP Discovery", aged: true);
		if (portMapper.HasFlag(PortMapper.Upnp))
		{
			UpnpSearcher upnpSearcher = new UpnpSearcher(new IPAddressesProvider());
			upnpSearcher.OnDeviceFound = (Searcher.DelegateEvent)Delegate.Combine(upnpSearcher.OnDeviceFound, new Searcher.DelegateEvent(OnDeviceFound));
			upnpSearcher.Discover();
			float elipsed = 0f;
			while (!upnpSearcher.Receive() && elipsed < timeout)
			{
				elipsed += Time.deltaTime;
				yield return Yielders.EndOfFrame;
			}
			upnpSearcher.CloseUdpClients();
		}
	}

	public void OnDeviceFound(NatDevice device)
	{
		searcherTasks.Add(device);
		UpnpNatDevice upnpNatDevice = (UpnpNatDevice)device;
		ConsoleWindow.PrintAction($"discovery: ip:{upnpNatDevice.DeviceInfo.LocalAddress}, ports:{GamePort}, {QueryPort}");
		device.CreatePortMapAsync(new Mapping(Protocol.Tcp, upnpNatDevice.DeviceInfo.LocalAddress, GamePort, GamePort, 0, "Stationeers(Gameport-tcp)"));
		device.CreatePortMapAsync(new Mapping(Protocol.Udp, upnpNatDevice.DeviceInfo.LocalAddress, GamePort, GamePort, 0, "Stationeers(Gameport-udp)"));
		device.CreatePortMapAsync(new Mapping(Protocol.Udp, upnpNatDevice.DeviceInfo.LocalAddress, QueryPort, QueryPort, 0, "Stationeers(QueryPort)"));
	}

	public static void ReleaseAll()
	{
		foreach (NatDevice searcherTask in Singleton<NatDiscoverer>.Instance.searcherTasks)
		{
			searcherTask.ReleaseAll();
		}
	}

	internal static void ReleaseSessionMappings()
	{
		foreach (NatDevice searcherTask in Singleton<NatDiscoverer>.Instance.searcherTasks)
		{
			searcherTask.ReleaseSessionMappings();
		}
	}

	private static void RenewMappings(object state)
	{
		foreach (NatDevice searcherTask in Singleton<NatDiscoverer>.Instance.searcherTasks)
		{
			searcherTask.RenewMappings();
		}
	}
}
