using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Assets.Scripts.OpenNat;

internal abstract class Searcher
{
	public delegate void DelegateEvent(NatDevice device);

	private readonly List<NatDevice> _devices = new List<NatDevice>();

	protected List<UdpClient> UdpClients;

	public DelegateEvent OnDeviceFound;

	internal DateTime NextSearch = DateTime.UtcNow;

	public void Discover()
	{
		if (DateTime.UtcNow < NextSearch)
		{
			return;
		}
		foreach (UdpClient udpClient in UdpClients)
		{
			try
			{
				Discover(udpClient);
			}
			catch (Exception)
			{
			}
		}
	}

	public bool Receive()
	{
		bool result = false;
		foreach (UdpClient item in UdpClients.Where((UdpClient x) => x.Available > 0))
		{
			result = true;
			IPAddress address = ((IPEndPoint)item.Client.LocalEndPoint).Address;
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.None, 0);
			byte[] response = item.Receive(ref remoteEP);
			NatDevice natDevice = AnalyseReceivedResponse(address, response, remoteEP);
			if (natDevice != null)
			{
				RaiseDeviceFound(natDevice);
			}
		}
		return result;
	}

	protected abstract void Discover(UdpClient client);

	public abstract NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint);

	public void CloseUdpClients()
	{
		foreach (UdpClient udpClient in UdpClients)
		{
			udpClient.Close();
		}
	}

	private void RaiseDeviceFound(NatDevice device)
	{
		OnDeviceFound(device);
	}
}
