using System.Collections.Generic;
using System.Net;
using System.Xml;
using Assets.Scripts.Util;

namespace Assets.Scripts.OpenNat;

public class UpnpNatDevice : NatDevice
{
	public UpnpNatDeviceInfo DeviceInfo;

	private readonly SoapClient _soapClient;

	private IPAddress ExternalIP;

	private List<UPnpReqest> MessageQueue;

	internal UpnpNatDevice(UpnpNatDeviceInfo deviceInfo)
	{
		Touch();
		DeviceInfo = deviceInfo;
		_soapClient = new SoapClient(DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType);
		_soapClient.TimeoutAfter(5);
		MessageQueue = new List<UPnpReqest>();
	}

	public bool IsCompleted()
	{
		return _soapClient.isResponseCompleted;
	}

	public IPAddress GetExternalIP()
	{
		return ExternalIP;
	}

	private void SendNextRequest()
	{
		if (MessageQueue.Count > 0)
		{
			UPnpReqest uPnpReqest = MessageQueue[0];
			MessageQueue.RemoveAt(0);
			Singleton<NatDiscoverer>.Instance.StartCoroutine(_soapClient.InvokeAsync(uPnpReqest.OnComplete, uPnpReqest.Mapping, uPnpReqest.OperationName, uPnpReqest.Args, this));
		}
	}

	private void OnResponseExternalIP(bool Success, Mapping mapping, XmlDocument responseData, MappingException me)
	{
		if (Success)
		{
			GetExternalIPAddressResponseMessage getExternalIPAddressResponseMessage = new GetExternalIPAddressResponseMessage(responseData, DeviceInfo.ServiceType);
			ExternalIP = getExternalIPAddressResponseMessage.ExternalIPAddress;
		}
		SendNextRequest();
	}

	public override void GetExternalIPAsync()
	{
		GetExternalIPAddressRequestMessage getExternalIPAddressRequestMessage = new GetExternalIPAddressRequestMessage();
		if (MessageQueue.Count > 0)
		{
			MessageQueue.Add(new UPnpReqest(OnResponseExternalIP, null, "GetExternalIPAddress", getExternalIPAddressRequestMessage.ToXml()));
		}
		else
		{
			Singleton<NatDiscoverer>.Instance.StartCoroutine(_soapClient.InvokeAsync(OnResponseExternalIP, null, "GetExternalIPAddress", getExternalIPAddressRequestMessage.ToXml(), this));
		}
	}

	private void OnResponseCreatePortMap(bool Success, Mapping mapping, XmlDocument responseData, MappingException me)
	{
		if (Success)
		{
			new AddPortMappingResponseMessage(responseData, DeviceInfo.ServiceType);
			RegisterMapping(mapping);
		}
		SendNextRequest();
	}

	public override void CreatePortMapAsync(Mapping mapping)
	{
		Guard.IsNotNull(mapping, "mapping");
		if (mapping.PrivateIP.Equals(IPAddress.None))
		{
			mapping.PrivateIP = DeviceInfo.LocalAddress;
		}
		CreatePortMappingRequestMessage createPortMappingRequestMessage = new CreatePortMappingRequestMessage(mapping);
		if (MessageQueue.Count > 0)
		{
			MessageQueue.Add(new UPnpReqest(OnResponseCreatePortMap, mapping, "AddPortMapping", createPortMappingRequestMessage.ToXml()));
		}
		else
		{
			Singleton<NatDiscoverer>.Instance.StartCoroutine(_soapClient.InvokeAsync(OnResponseCreatePortMap, mapping, "AddPortMapping", createPortMappingRequestMessage.ToXml(), this));
		}
	}

	private void OnResponseDeletePortMap(bool Success, Mapping mapping, XmlDocument responseData, MappingException me)
	{
		if (Success)
		{
			new DeletePortMappingResponseMessage(responseData, DeviceInfo.ServiceType);
			UnregisterMapping(mapping);
		}
		SendNextRequest();
	}

	public override void DeletePortMapAsync(Mapping mapping)
	{
		Guard.IsNotNull(mapping, "mapping");
		if (mapping.PrivateIP.Equals(IPAddress.None))
		{
			mapping.PrivateIP = DeviceInfo.LocalAddress;
		}
		DeletePortMappingRequestMessage deletePortMappingRequestMessage = new DeletePortMappingRequestMessage(mapping);
		if (MessageQueue.Count > 0)
		{
			MessageQueue.Add(new UPnpReqest(OnResponseDeletePortMap, mapping, "DeletePortMapping", deletePortMappingRequestMessage.ToXml()));
		}
		else
		{
			Singleton<NatDiscoverer>.Instance.StartCoroutine(_soapClient.InvokeAsync(OnResponseDeletePortMap, mapping, "DeletePortMapping", deletePortMappingRequestMessage.ToXml(), this));
		}
	}

	public void GetGenericMappingAsync(int index, List<Mapping> mappings)
	{
		new GetGenericPortMappingEntry(index);
	}

	public override void GetAllMappingsAsync()
	{
		GetGenericMappingAsync(0, new List<Mapping>());
	}

	public override Mapping GetSpecificMappingAsync(Protocol protocol, int publicPort)
	{
		Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
		Guard.IsInRange(publicPort, 0, 65535, "port");
		return null;
	}

	public override string ToString()
	{
		return $"EndPoint: {DeviceInfo.HostEndPoint}\nControl Url: {DeviceInfo.ServiceControlUri}\nService Type: {DeviceInfo.ServiceType}\nLast Seen: {base.LastSeen}";
	}
}
