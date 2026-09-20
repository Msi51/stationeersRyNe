using System.Xml.Serialization;

namespace Objects.Rockets;

public abstract class TemplateNodeReference : MapNodeReference
{
	[XmlAttribute("TemplateId")]
	public string TemplateId;

	protected TemplateNodeReference()
	{
	}

	protected TemplateNodeReference(SpaceMapNode spaceMapNode)
		: base(spaceMapNode)
	{
		TemplateId = spaceMapNode.Data?.Id;
	}
}
