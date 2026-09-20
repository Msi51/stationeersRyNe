using Assets.Scripts.Util;

namespace Assets.Scripts;

public interface IShadowLight : IDensePoolable
{
	bool IsShadowCandidate { get; }

	float ShadowDistanceSquared { get; }

	void SetShadowCasting(bool shouldCastShadows);
}
