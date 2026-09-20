using Assets.Scripts.Util;

namespace Assets.Scripts.Objects;

public interface IPerishable : IDensePoolable
{
	bool CanItemDecay();

	void OnDecayServer(float tickSeconds);

	void OnDecayClient(float tickSeconds);

	float GetDecay();
}
