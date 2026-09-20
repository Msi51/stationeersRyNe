using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class InterrogateTrader : ProcessedMessage<InterrogateTrader>
{
	public long TraderReferenceId;

	public long SatelliteDishReferenceId;

	public override void Process(long hostId)
	{
		TraderContact traderContact = Referencable.Find<TraderContact>(TraderReferenceId);
		SatelliteDish satelliteDish = Thing.Find<SatelliteDish>(SatelliteDishReferenceId);
		if (traderContact == null)
		{
			ConsoleWindow.PrintError($"non-fatal error interrogating trader from client, as trader #{TraderReferenceId} does not exist", suppressStacktrace: true);
		}
		if ((object)satelliteDish == null)
		{
			ConsoleWindow.PrintError($"non-fatal error interrogating trader from client, as dish #{TraderReferenceId} does not exist", suppressStacktrace: true);
		}
		if ((object)satelliteDish != null && traderContact != null)
		{
			traderContact.InterrogatingDish = satelliteDish;
			satelliteDish.InterrogatingContact = traderContact;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		TraderReferenceId = reader.ReadInt64();
		SatelliteDishReferenceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(TraderReferenceId);
		writer.WriteInt64(SatelliteDishReferenceId);
	}
}
