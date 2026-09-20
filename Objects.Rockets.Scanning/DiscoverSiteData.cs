using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public class DiscoverSiteData : SpaceMapNodeActionData
{
	[XmlElement("Site")]
	public List<SpaceMapNodeData> SiteGenerations = new List<SpaceMapNodeData>();

	public DiscoverSiteData()
	{
	}

	public override void Initialize(ModAbout mod)
	{
		base.Initialize(mod);
		foreach (SpaceMapNodeData siteGeneration in SiteGenerations)
		{
			siteGeneration.Initialize(mod);
		}
	}

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new Discover(this, node);
	}

	public DiscoverSiteData(RocketBinaryReader reader)
		: base(reader)
	{
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			int idHash = reader.ReadInt32();
			SiteGenerations.Add(DataCollection.Get<SpaceMapNodeData>(idHash));
		}
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (SpaceMapNodeData siteGeneration in SiteGenerations)
		{
			writer.WriteInt32(siteGeneration.IdHash);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}
}
