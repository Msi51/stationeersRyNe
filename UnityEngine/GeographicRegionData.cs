using System.Xml.Serialization;
using Assets.Scripts;

namespace UnityEngine;

[XmlRoot("GeographicRegionData")]
public class GeographicRegionData : DataCollection
{
	[XmlElement("DefaultRegionName")]
	public LocalizedStringReference DefaultRegionName;

	[XmlElement("RegionSet")]
	public RegionSet RegionSet;

	public override void Initialize(ModAbout mod)
	{
	}
}
