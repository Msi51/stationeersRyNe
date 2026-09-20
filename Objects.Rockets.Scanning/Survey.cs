using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class Survey : ScanAction
{
	public Survey(SurveyData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (rocket?.GetScanners() == null)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketScanner);
		}
		if (SpaceMapNode.SurveyPercent > 1000f)
		{
			return result = RocketActionResult.Failure(GameStrings.SurveyCompleted, SpaceMapNode);
		}
		return result = RocketActionResult.Success;
	}

	public override void DoScanCycle(RocketScanner scanner)
	{
		SpaceMapNode.SurveyPoints += (int)((float)scanner.Points * scanner.SurveyMultiplier);
	}

	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Survey;
	}
}
