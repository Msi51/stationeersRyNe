using System.Collections.Generic;
using Assets.Scripts.Localization2;

namespace Objects.Rockets.Scanning;

public class RocketTransfer : RocketAction
{
	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Transfer;
	}

	public RocketTransfer(SpaceMapNodeActionData actionData, SpaceMapNode node)
		: base(actionData, node)
	{
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (SpaceMapNode != null && SpaceMapNode.HasAction(RocketMode.Transfer))
		{
			return result = RocketActionResult.Success;
		}
		return result = RocketActionResult.Failure(GameStrings.None);
	}

	public override void Start(Rocket rocket)
	{
	}

	public override bool ProgressAction(float deltaTime, Rocket rocket, out RocketActionResult result)
	{
		List<IUmbilical> list = rocket?.GetUmbilicals();
		if (list == null || list.Count == 0)
		{
			return result = RocketActionResult.Failure(GameStrings.NoUmbilicalForTransfer);
		}
		foreach (IUmbilical item in list)
		{
			if (item is IRocketTransferActionProgressable rocketTransferActionProgressable)
			{
				if (rocketTransferActionProgressable.CanProgressAction(out var result2))
				{
					rocketTransferActionProgressable.ProgressTransferAction(deltaTime, this);
				}
				else
				{
					rocket.Report(result2);
				}
			}
		}
		return result = RocketActionResult.Success;
	}

	public override void Close(Rocket rocket, bool clearAction = true)
	{
		base.Close(rocket, clearAction);
		List<IUmbilical> list = rocket?.GetUmbilicals();
		if (list == null)
		{
			return;
		}
		foreach (IUmbilical item in list)
		{
			if (item is IRocketTransferActionProgressable rocketTransferActionProgressable)
			{
				rocketTransferActionProgressable.ClearAction();
			}
		}
	}
}
