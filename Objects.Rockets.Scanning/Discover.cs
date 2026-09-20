using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class Discover : ScanAction
{
	public Discover(DiscoverSiteData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Discover;
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (rocket?.GetScanners() == null)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketScanner);
		}
		if (SpaceMapNode.DynamicNodeCount() >= SpaceMapNode.DynamicNodeCapacity)
		{
			return result = RocketActionResult.Failure(GameStrings.SiteCapacityReached, SpaceMapNode);
		}
		return result = RocketActionResult.Success;
	}

	public override void DoScanCycle(RocketScanner scanner)
	{
		SpaceMapNode.DiscoverPoints += (int)((float)scanner.Points * scanner.DiscoveryMultiplier);
	}
}
