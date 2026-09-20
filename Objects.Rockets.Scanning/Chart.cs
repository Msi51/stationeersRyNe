using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class Chart : ScanAction
{
	public Chart(ChartData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Chart;
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (rocket?.GetScanners() == null)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketScanner);
		}
		foreach (NodeConnection childConnection in SpaceMapNode.ChildConnections)
		{
			SpaceMapNode spaceMapNode = childConnection?.Child;
			if (spaceMapNode != null && !spaceMapNode.IsCharted)
			{
				return result = RocketActionResult.Success;
			}
		}
		return result = RocketActionResult.Failure(GameStrings.ChartFailure);
	}

	public override void DoScanCycle(RocketScanner scanner)
	{
		SpaceMapNode.ChartPoints += scanner.Points;
	}
}
