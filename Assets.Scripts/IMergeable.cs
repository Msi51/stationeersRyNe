using Trading;

namespace Assets.Scripts;

public interface IMergeable : IReferencable, IEvaluable
{
	bool IsStackFull { get; }

	bool CanStack(IMergeable targetStack);

	int GetPrefabHash();

	void Merge(IMergeable stackable);

	string ToTooltip();
}
