using System.Collections.Generic;
using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class RocketDeploy : RocketAction
{
	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Deploy;
	}

	public RocketDeploy(SpaceMapNodeActionData actionData, SpaceMapNode node)
		: base(actionData, node)
	{
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		return result = RocketActionResult.Success;
	}

	public override void Start(Rocket rocket)
	{
	}

	public override bool ProgressAction(float deltaTime, Rocket rocket, out RocketActionResult result)
	{
		List<RocketPayloadBay> payloadBays = rocket.GetPayloadBays();
		if (payloadBays == null || payloadBays.Count == 0)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketCargoBay);
		}
		foreach (RocketPayloadBay item in payloadBays)
		{
			if (item.CanProgressAction(out var result2))
			{
				item.ProgressDeployAction(deltaTime, this);
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
		List<RocketPayloadBay> list = rocket?.GetPayloadBays();
		if (list == null)
		{
			return;
		}
		foreach (RocketPayloadBay item in list)
		{
			item.ClearAction();
		}
	}
}
