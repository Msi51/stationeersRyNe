using Networks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public interface ITraderDestination : IReferencable, IEvaluable
{
	TraderShuttle Shuttle { get; }

	GameObject Trader { get; }

	bool IsTraderReady { get; set; }

	bool Locked { get; set; }

	TraderContact CurrentTradingContact { get; }

	LandingPadNetwork LandingPadNetwork { get; }

	bool CanTraderLand(TraderContact trader, out string errorMessage);

	void ServerCallTrader(bool isLanding, TraderContact contact);

	bool IsObstructed(TraderContact trader);
}
