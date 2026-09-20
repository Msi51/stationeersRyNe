using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Pipes;
using Objects.Rockets.Mining;

namespace Objects.Rockets.Scanning;

public class RocketMine : RocketAction
{
	public override bool IsRocketMode(RocketMode rocketMode)
	{
		return rocketMode == RocketMode.Mine;
	}

	public RocketMine(SpaceMapNodeMineData data, SpaceMapNode node)
		: base(data, node)
	{
	}

	public override bool Evaluate(Rocket rocket, out RocketActionResult result)
	{
		if (SpaceMapNode?.Deposit != null)
		{
			return result = RocketActionResult.Success;
		}
		MineableDeposit mineableDeposit = SpaceMapNode?.Deposit;
		if (mineableDeposit != null && mineableDeposit.IsDepleted)
		{
			return result = RocketActionResult.Failure(GameStrings.DepositIsDepleted);
		}
		return result = RocketActionResult.Failure(GameStrings.MiningFailure);
	}

	public override void Start(Rocket rocket)
	{
	}

	public override bool ProgressAction(float deltaTime, Rocket rocket, out RocketActionResult result)
	{
		List<IRocketMiner> miners = rocket.GetMiners();
		if (miners == null || miners.Count == 0)
		{
			return result = RocketActionResult.Failure(GameStrings.NoRocketMiner);
		}
		if (SpaceMapNode.Deposit.IsDepleted)
		{
			return result = RocketActionResult.Failure(GameStrings.DepositIsDepleted);
		}
		foreach (IRocketMiner item in miners)
		{
			if (item.CanProgressAction(out var result2))
			{
				item.ProgressMineAction(deltaTime, this);
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
		List<IRocketMiner> list = rocket?.GetMiners();
		if (list == null)
		{
			return;
		}
		foreach (IRocketMiner item in list)
		{
			item.ClearAction();
		}
	}
}
