using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class RocketRecordData
{
	[XmlElement]
	public long RocketNetworkId;

	[XmlElement]
	public Vector3 Offset;
}
