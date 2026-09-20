namespace Objects.Rockets;

public class StaticNodeReference : TraversableNodeReference
{
	public StaticNodeReference()
	{
	}

	public StaticNodeReference(SpaceMapNode spaceMapNode)
		: base(spaceMapNode)
	{
	}

	public override void Deserialize()
	{
		base.Deserialize();
		SpaceMapNode spaceMapNode = SpaceMapNode.Get(TemplateId);
		if (spaceMapNode != null)
		{
			spaceMapNode.IsCharted = IsCharted;
			spaceMapNode.ChartPoints = ChartPoints;
			spaceMapNode.DiscoverPoints = DiscoverPoints;
		}
	}
}
