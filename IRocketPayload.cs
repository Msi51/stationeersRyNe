using Objects.Rockets.Scanning;
using Trading;

public interface IRocketPayload : IReferencable, IEvaluable
{
	float MassContribution { get; }

	float TimeToDeploy { get; }

	void OnDeploy();

	RocketActionResult CanDeploy();
}
