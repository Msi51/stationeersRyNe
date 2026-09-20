using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public class RocketLandAbortedEvent : RocketEvent
{
	public override string TextColor => "red";

	public override RocketEventType RocketEventType => RocketEventType.RocketLandAborted;

	public override string GetText()
	{
		return GameStrings.RocketLandAborted.DisplayString;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
	}

	public RocketLandAbortedEvent(IReferencable eventOrigin)
		: base(eventOrigin)
	{
		Hash = (Hash ^ 7) * 41;
	}

	public RocketLandAbortedEvent(RocketBinaryReader reader)
		: base(reader)
	{
	}
}
