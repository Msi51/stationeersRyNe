using System.Xml.Serialization;
using Assets.Scripts;

namespace UnityEngine;

[XmlRoot("DeepMinablesRegionData")]
public class DeepMinablesRegionData : DataCollection
{
	[XmlElement("RegionSet")]
	public RegionSet RegionSet;

	public override void Initialize(ModAbout mod)
	{
	}
}
