using Assets.Scripts.Objects;

namespace Objects.Electrical;

public interface IFermentable
{
	float SecondsToProcess => 20f;

	SpawnGas[] SpawnGasList { get; }
}
