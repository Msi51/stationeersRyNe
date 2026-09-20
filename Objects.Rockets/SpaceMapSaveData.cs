using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Objects.Rockets;

public class SpaceMapSaveData : SerializedId
{
	[XmlElement("SiteNode", Type = typeof(SiteNodeReference))]
	[XmlElement("StaticNode", Type = typeof(StaticNodeReference))]
	[XmlElement("LaunchPadNode", Type = typeof(LaunchPadNodeReference))]
	public List<MapNodeReference> Nodes;

	public SpaceMapSaveData()
	{
	}

	public SpaceMapSaveData(SpaceMap spaceMap)
	{
		Id = spaceMap.Id;
		if (spaceMap.Nodes == null)
		{
			return;
		}
		Nodes = new List<MapNodeReference>(spaceMap.Nodes.Count);
		foreach (SpaceMapNode node in spaceMap.Nodes)
		{
			Nodes.Add(node.SerializeSave());
		}
	}

	public bool GetSavedStaticNodeReferenceId(string nodeTemplateId, out long referenceId)
	{
		referenceId = 0L;
		if (string.IsNullOrEmpty(nodeTemplateId))
		{
			return false;
		}
		foreach (MapNodeReference node in Nodes)
		{
			if (node is TemplateNodeReference templateNodeReference && string.Equals(templateNodeReference.TemplateId, nodeTemplateId, StringComparison.InvariantCulture))
			{
				referenceId = templateNodeReference.Id;
			}
		}
		return referenceId != 0;
	}
}
