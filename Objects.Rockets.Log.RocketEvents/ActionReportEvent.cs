using Assets.Scripts.Networking;
using Objects.Rockets.Scanning;

namespace Objects.Rockets.Log.RocketEvents;

public class ActionReportEvent : RocketEvent
{
	private readonly RocketActionResult _actionResult;

	public override RocketEventType RocketEventType => RocketEventType.ActionReport;

	public override string TextColor => "red";

	public override string GetText()
	{
		return RocketActionResult.GetText(_actionResult);
	}

	public ActionReportEvent(IReferencable eventOrigin, RocketActionResult actionResult)
		: base(eventOrigin)
	{
		_actionResult = actionResult;
		Hash = (Hash ^ _actionResult.FailureInfo?.Key).GetValueOrDefault() * 41;
		Hash = (Hash ^ (int)_actionResult.ReferencableId) * 41;
		Hash = (Hash ^ 4) * 41;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		RocketActionResult.Write(writer, _actionResult);
	}

	public ActionReportEvent(RocketBinaryReader reader)
		: base(reader)
	{
		_actionResult = RocketActionResult.Create(reader);
	}
}
