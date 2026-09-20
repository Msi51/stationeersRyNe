using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;

namespace Objects.Rockets;

public class SpaceMapData : DataCollection
{
	[XmlAttribute("OrbitDistance")]
	public float DistanceToOrbit = 400f;

	[XmlAttribute("FallBack")]
	public bool FallBack;

	[XmlElement("Entry")]
	public SpaceMapNodeData EntryNodeData;

	[XmlElement("LowOrbitHub")]
	public SpaceMapNodeData LowOrbitHubNodeData;

	[XmlElement("Node")]
	public List<SpaceMapNodeData> NodeDatas = new List<SpaceMapNodeData>();

	[XmlElement("Sprite")]
	public List<SpaceMapSpriteData> SpriteData = new List<SpaceMapSpriteData>();

	public override bool IsValid()
	{
		if (EntryNodeData != null)
		{
			return NodeDatas.Count > 0;
		}
		return false;
	}

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		DataCollection.Register(this, mod);
		if (EntryNodeData.IsValid())
		{
			EntryNodeData.Initialize(mod);
		}
		if (LowOrbitHubNodeData != null)
		{
			LowOrbitHubNodeData.Initialize(mod);
		}
		foreach (SpaceMapSpriteData spriteDatum in SpriteData)
		{
			spriteDatum.Initialize(mod);
		}
		foreach (SpaceMapNodeData nodeData in NodeDatas)
		{
			if (nodeData.IsValid())
			{
				nodeData.Initialize(mod);
			}
		}
	}
}
