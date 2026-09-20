using Trading;

namespace Objects.Rockets.Scanning;

public interface IRocketActionProgressable : IReferencable, IEvaluable
{
	float GetActionProgress { get; }

	string GetActionInfoText();

	bool CanProgressAction(out RocketActionResult result);

	void ClearAction();
}
