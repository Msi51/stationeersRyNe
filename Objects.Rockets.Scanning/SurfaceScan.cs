using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class SurfaceScan : ScanAction
{
	public override float ProgressSpeedMult => 20f;

	public SurfaceScan(SurfaceScanData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (rocket?.GetScanners() == null)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketScanner);
		}
		return result = RocketActionResult.Success;
	}

	public override void DoScanCycle(RocketScanner scanner)
	{
		scanner.DoScanOnLinkedMotherboards();
	}

	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.SurfaceScan;
	}
}
