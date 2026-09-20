using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace Assets.Scripts.OpenNat;

internal class UpnpSearcher : Searcher
{
	private readonly IIPAddressesProvider _ipprovider;

	private readonly IDictionary<Uri, NatDevice> _devices;

	private readonly Dictionary<IPAddress, DateTime> _lastFetched;

	private static readonly string[] ServiceTypes = new string[4] { "WANIPConnection:2", "WANPPPConnection:2", "WANIPConnection:1", "WANPPPConnection:1" };

	public UpnpSearcher(IIPAddressesProvider ipprovider)
	{
		_ipprovider = ipprovider;
		UdpClients = CreateUdpClients();
		_devices = new Dictionary<Uri, NatDevice>();
		_lastFetched = new Dictionary<IPAddress, DateTime>();
	}

	private List<UdpClient> CreateUdpClients()
	{
		List<UdpClient> list = new List<UdpClient>();
		try
		{
			foreach (IPAddress item in _ipprovider.UnicastAddresses())
			{
				try
				{
					list.Add(new UdpClient(new IPEndPoint(item, 0)));
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
		{
			list.Add(new UdpClient(0));
		}
		return list;
	}

	protected override void Discover(UdpClient client)
	{
		Discover(client, WellKnownConstants.IPv4MulticastAddress);
		if (Socket.OSSupportsIPv6)
		{
			Discover(client, WellKnownConstants.IPv6LinkLocalMulticastAddress);
			Discover(client, WellKnownConstants.IPv6LinkSiteMulticastAddress);
		}
	}

	private void Discover(UdpClient client, IPAddress address)
	{
		if (!IsValidClient(client.Client, address))
		{
			return;
		}
		NextSearch = DateTime.UtcNow.AddSeconds(1.0);
		IPEndPoint endPoint = new IPEndPoint(address, 1900);
		string[] serviceTypes = ServiceTypes;
		for (int i = 0; i < serviceTypes.Length; i++)
		{
			string s = DiscoverDeviceMessage.Encode(serviceTypes[i], address);
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			for (int j = 0; j < 3; j++)
			{
				client.Send(bytes, bytes.Length, endPoint);
			}
		}
	}

	private bool IsValidClient(Socket socket, IPAddress address)
	{
		IPEndPoint iPEndPoint = (IPEndPoint)socket.LocalEndPoint;
		if (socket.AddressFamily != address.AddressFamily)
		{
			return false;
		}
		switch (socket.AddressFamily)
		{
		case AddressFamily.InterNetwork:
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, iPEndPoint.Address.GetAddressBytes());
			return true;
		case AddressFamily.InterNetworkV6:
			if (iPEndPoint.Address.IsIPv6LinkLocal && !object.Equals(address, WellKnownConstants.IPv6LinkLocalMulticastAddress))
			{
				return false;
			}
			if (!iPEndPoint.Address.IsIPv6LinkLocal && !object.Equals(address, WellKnownConstants.IPv6LinkSiteMulticastAddress))
			{
				return false;
			}
			socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, BitConverter.GetBytes((int)iPEndPoint.Address.ScopeId));
			return true;
		default:
			return false;
		}
	}

	public override NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
	{
		try
		{
			DiscoveryResponseMessage discoveryResponseMessage = new DiscoveryResponseMessage(Encoding.UTF8.GetString(response));
			if (!IsValidControllerService(discoveryResponseMessage["ST"]))
			{
				return null;
			}
			Uri uri = new Uri(discoveryResponseMessage["Location"] ?? discoveryResponseMessage["AL"]);
			if (_devices.ContainsKey(uri))
			{
				_devices[uri].Touch();
				return null;
			}
			if (_lastFetched.ContainsKey(endpoint.Address))
			{
				DateTime dateTime = _lastFetched[endpoint.Address];
				if (DateTime.Now - dateTime < TimeSpan.FromSeconds(20.0))
				{
					return null;
				}
			}
			_lastFetched[endpoint.Address] = DateTime.Now;
			UpnpNatDeviceInfo deviceInfo = BuildUpnpNatDeviceInfo(localAddress, uri);
			UpnpNatDevice upnpNatDevice;
			lock (_devices)
			{
				upnpNatDevice = new UpnpNatDevice(deviceInfo);
				if (!_devices.ContainsKey(uri))
				{
					_devices.Add(uri, upnpNatDevice);
				}
			}
			return upnpNatDevice;
		}
		catch (Exception)
		{
		}
		return null;
	}

	private static bool IsValidControllerService(string serviceType)
	{
		return (from serviceName in ServiceTypes
			let serviceUrn = $"urn:schemas-upnp-org:service:{serviceName}"
			where serviceType.ContainsIgnoreCase(serviceUrn)
			select new
			{
				ServiceName = serviceName,
				ServiceUrn = serviceUrn
			}).Any();
	}

	private UpnpNatDeviceInfo BuildUpnpNatDeviceInfo(IPAddress localAddress, Uri location)
	{
		new IPEndPoint(IPAddress.Parse(location.Host), location.Port);
		WebResponse webResponse = null;
		try
		{
			WebRequest webRequest = WebRequest.Create(location);
			webRequest.Headers.Add("ACCEPT-LANGUAGE", "en");
			webRequest.Method = "GET";
			webResponse = webRequest.GetResponse();
			if (webResponse is HttpWebResponse { StatusCode: not HttpStatusCode.OK } httpWebResponse)
			{
				throw new Exception($"Couldn't get services list: {httpWebResponse.StatusCode} {httpWebResponse.StatusDescription}");
			}
			XmlDocument xmlDocument = ReadXmlResponse(webResponse);
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
			foreach (XmlNode item in xmlDocument.SelectNodes("//ns:service", xmlNamespaceManager))
			{
				string xmlElementText = item.GetXmlElementText("serviceType");
				if (IsValidControllerService(xmlElementText))
				{
					string xmlElementText2 = item.GetXmlElementText("controlURL");
					return new UpnpNatDeviceInfo(localAddress, location, xmlElementText2, xmlElementText);
				}
			}
			throw new Exception("No valid control service was found in the service descriptor document");
		}
		catch (WebException ex)
		{
			_ = ex.InnerException is SocketException;
			throw;
		}
		finally
		{
			webResponse?.Close();
		}
	}

	private static XmlDocument ReadXmlResponse(WebResponse response)
	{
		using StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
		string xml = streamReader.ReadToEnd();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		return xmlDocument;
	}
}
