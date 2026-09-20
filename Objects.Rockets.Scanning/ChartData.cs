using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public class ChartData : SpaceMapNodeActionData
{
	public ChartData()
	{
	}

	public ChartData(RocketBinaryReader reader)
		: base(reader)
	{
	}

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new Chart(this, node);
	}
}
