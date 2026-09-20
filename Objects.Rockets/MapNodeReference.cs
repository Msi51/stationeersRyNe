using System.Xml.Serialization;

namespace Objects.Rockets;

[XmlRoot("Node")]
public abstract class MapNodeReference : SerializedReferenceId
{
	[XmlElement("Parent")]
	public SerializedReferenceId ParentId;

	protected MapNodeReference()
	{
	}

	protected MapNodeReference(SpaceMapNode spaceMapNode)
	{
		Id = spaceMapNode.ReferenceId;
		ParentId = SerializedReferenceId.Create(spaceMapNode.ParentConnection?.Parent);
	}

	public virtual void Deserialize()
	{
	}
}
