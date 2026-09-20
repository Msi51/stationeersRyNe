using Assets.Scripts.Objects.Motherboards;

namespace Objects.Rockets;

public interface IRocketEngine : IRocketInternals, IRocketComponent
{
	float Force { get; }

	float MaxThrust { get; }

	float MaxExhaustVelocity { get; }

	float MaxFuelFlowRate { get; }

	float SpecificImpulse { get; }

	float EfficiencyPercent { get; }

	float ExhaustVelocity { get; }

	void SetLogicValue(LogicType logicType, double value);

	double GetLogicValue(LogicType logicType);
}
