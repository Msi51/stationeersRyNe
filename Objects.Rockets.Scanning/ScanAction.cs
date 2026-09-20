using System.Collections.Generic;
using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public abstract class ScanAction : RocketAction
{
	public virtual float ProgressSpeedMult => 1f;

	public ScanAction(SpaceMapNodeActionData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override void Start(Rocket rocket)
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

	public override bool ProgressAction(float deltaTime, Rocket rocket, out RocketActionResult result)
	{
		List<RocketScanner> scanners = rocket.GetScanners();
		if (scanners == null || scanners.Count == 0)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketScanner);
		}
		foreach (RocketScanner item in scanners)
		{
			RocketActionResult result2 = item.CanProgressAction(this);
			if (result2.IsSuccess)
			{
				item.Progress(this, deltaTime * ProgressSpeedMult);
			}
			else
			{
				rocket.Report(result2);
			}
		}
		return result = RocketActionResult.Success;
	}

	public override void Close(Rocket rocket, bool clearAction = true)
	{
		base.Close(rocket, clearAction);
		List<RocketScanner> list = rocket?.GetScanners();
		if (list == null)
		{
			return;
		}
		foreach (RocketScanner item in list)
		{
			item.ResetCycleTime();
		}
	}

	public abstract void DoScanCycle(RocketScanner scanner);
}
