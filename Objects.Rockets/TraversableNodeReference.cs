using System.Xml.Serialization;

namespace Objects.Rockets;

public abstract class TraversableNodeReference : TemplateNodeReference
{
	public IntReference ChartPoints;

	public IntReference DiscoverPoints;

	[XmlAttribute]
	public bool IsCharted;

	protected TraversableNodeReference()
	{
	}

	protected TraversableNodeReference(SpaceMapNode spaceMapNode)
		: base(spaceMapNode)
	{
		IsCharted = spaceMapNode.IsCharted;
		ChartPoints = new IntReference(spaceMapNode.ChartPoints);
		DiscoverPoints = new IntReference(spaceMapNode.DiscoverPoints);
	}
}
