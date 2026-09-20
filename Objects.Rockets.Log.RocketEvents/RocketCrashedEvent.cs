using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace Objects.Rockets.Log.RocketEvents;

public class RocketCrashedEvent : RocketEvent
{
	private Structure _crashedInto;

	public sealed override RocketEventType RocketEventType => RocketEventType.RocketCrashed;

	public override string TextColor => "red";

	private string CrashedIntoName => _crashedInto?.DisplayName ?? GameStrings.TheGround.DisplayString;

	public override string GetText()
	{
		return GameStrings.RocketCrashed.AsString(EventOriginName, CrashedIntoName);
	}

	public RocketCrashedEvent(IReferencable eventOrigin, Structure structure)
		: base(eventOrigin)
	{
		_crashedInto = structure;
		long num = _crashedInto?.ReferenceId ?? 0;
		Hash = (int)(((uint)Hash ^ (uint)RocketEventType) * 41);
		Hash = (Hash ^ (int)num) * 41;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		Network.WritePackedId(writer, _crashedInto);
	}

	public RocketCrashedEvent(RocketBinaryReader reader)
		: base(reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		_crashedInto = Referencable.Find<Structure>(referenceId);
	}
}
