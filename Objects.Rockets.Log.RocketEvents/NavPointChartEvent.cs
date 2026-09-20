using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public class NavPointChartEvent : RocketEvent
{
	private readonly SpaceMapNode _chartedNode;

	public override RocketEventType RocketEventType => RocketEventType.NavPointChart;

	public override string TextColor => "white";

	public override string GetText()
	{
		return GameStrings.RocketLogCharted.AsString(_chartedNode?.DisplayName ?? string.Empty);
	}

	public NavPointChartEvent(IReferencable eventOrigin, SpaceMapNode chartedNode)
		: base(eventOrigin)
	{
		_chartedNode = chartedNode;
		Hash = (Hash ^ 5) * 41;
		Hash = (Hash ^ (int)_chartedNode.ReferenceId) * 41;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		Network.WritePackedId(writer, _chartedNode);
	}

	public NavPointChartEvent(RocketBinaryReader reader)
		: base(reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		_chartedNode = Referencable.Find<SpaceMapNode>(referenceId);
	}
}
