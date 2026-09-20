using Objects.Rockets;
using Objects.Rockets.Scanning;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IRocketMiner : IRocketComponent, IRocketActionProgressable, IReferencable, IEvaluable
{
	void ProgressMineAction(float deltaTime, RocketMine mineAction);
}
