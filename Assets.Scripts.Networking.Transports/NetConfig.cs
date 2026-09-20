using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Networking.Transports;

public class NetConfig
{
	[XmlIgnore]
	private const string PATH = "NetConfig/NetConfig.xml";

	public string IP { get; set; }

	public ushort Port { get; set; }

	public string GetUrl(EndPoint endPoint)
	{
		return $"http://{IP}:{Port}/{endPoint}";
	}

	public static NetConfig Load()
	{
		return XmlSerialization.Deserialize<NetConfig>(Application.streamingAssetsPath + "/NetConfig/NetConfig.xml");
	}

	public void Save()
	{
		this.SaveXml(Application.streamingAssetsPath + "/NetConfig/NetConfig.xml");
	}

	public override string ToString()
	{
		return $"{IP}:{Port}";
	}
}
