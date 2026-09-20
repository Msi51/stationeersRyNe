using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class CallTrader : ProcessedMessage<CallTrader>
{
	public bool IsLanding;

	public long LandingPadReferenceId;

	public long TraderReferenceId;

	public override void Process(long hostId)
	{
		ITraderDestination traderDestination = Referencable.Find<ITraderDestination>(LandingPadReferenceId);
		TraderContact contact = Referencable.Find<TraderContact>(TraderReferenceId);
		traderDestination?.ServerCallTrader(IsLanding, contact);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		IsLanding = reader.ReadBoolean();
		LandingPadReferenceId = reader.ReadInt64();
		TraderReferenceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(IsLanding);
		writer.WriteInt64(LandingPadReferenceId);
		writer.WriteInt64(TraderReferenceId);
	}
}
