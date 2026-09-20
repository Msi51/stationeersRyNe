using System.Collections.Generic;
using Trading;

namespace Objects.Rockets.Scanning;

public interface IRocketTransferActionProgressable : IRocketActionProgressable, IReferencable, IEvaluable
{
	IRocketActionProgressableTarget CurrentTarget { get; }

	void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer);

	List<IRocketActionProgressableTarget> GetValidTargets();

	void SetTarget(IRocketActionProgressableTarget selectedTarget);
}
