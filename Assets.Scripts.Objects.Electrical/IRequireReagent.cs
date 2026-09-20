using Reagents;

namespace Assets.Scripts.Objects.Electrical;

public interface IRequireReagent
{
	bool IsReagentUser { get; }

	Recipe RequiredReagents { get; }

	Recipe CurrentRecipe { get; }

	int GetPrefabHashFromReagentHash(int reagentHash);
}
