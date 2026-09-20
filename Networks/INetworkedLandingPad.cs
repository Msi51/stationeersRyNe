using Objects.Electrical;
using Trading;

namespace Networks;

public interface INetworkedLandingPad : INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, INetworkedPad
{
	LandingPadCenter LandingPadCenter { get; }

	int PhaseBucket { get; }

	LandingPadNetwork LandingPadNetwork { get; }

	bool AnimateLights { get; }

	void FlashLights(bool flash);
}
