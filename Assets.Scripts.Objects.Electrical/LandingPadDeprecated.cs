using Networks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LandingPadDeprecated : Electrical, ITraderDestination, IReferencable, IEvaluable
{
	public TraderShuttle Shuttle { get; }

	public GameObject Trader { get; }

	public bool IsTraderReady { get; set; }

	public bool Locked { get; set; }

	public TraderContact CurrentTradingContact { get; }

	public LandingPadNetwork LandingPadNetwork { get; }

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LandingPadSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
	}

	public bool CanTraderLand(TraderContact trader, out string errorMessage)
	{
		errorMessage = "This Landing-pad has been deprecated";
		return false;
	}

	public void ServerCallTrader(bool isLanding, TraderContact contact)
	{
	}

	public bool IsObstructed(TraderContact trader)
	{
		return false;
	}
}
