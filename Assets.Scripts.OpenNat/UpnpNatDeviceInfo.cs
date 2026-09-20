using System;
using System.Net;
using UnityEngine;

namespace Assets.Scripts.OpenNat;

public class UpnpNatDeviceInfo
{
	public IPEndPoint HostEndPoint { get; private set; }

	public IPAddress LocalAddress { get; private set; }

	public string ServiceType { get; private set; }

	public Uri ServiceControlUri { get; private set; }

	public UpnpNatDeviceInfo(IPAddress localAddress, Uri locationUri, string serviceControlUrl, string serviceType)
	{
		LocalAddress = localAddress;
		ServiceType = serviceType;
		HostEndPoint = new IPEndPoint(IPAddress.Parse(locationUri.Host), locationUri.Port);
		if (Uri.IsWellFormedUriString(serviceControlUrl, UriKind.Absolute))
		{
			Uri uri = new Uri(serviceControlUrl);
			IPEndPoint hostEndPoint = HostEndPoint;
			serviceControlUrl = uri.PathAndQuery;
			Debug.Log($"{hostEndPoint}: Absolute URI detected. Host address is now: {HostEndPoint}");
			Debug.Log($"{HostEndPoint}: New control url: {serviceControlUrl}");
		}
		UriBuilder uriBuilder = new UriBuilder("http", locationUri.Host, locationUri.Port);
		ServiceControlUri = new Uri(uriBuilder.Uri, serviceControlUrl);
	}
}
