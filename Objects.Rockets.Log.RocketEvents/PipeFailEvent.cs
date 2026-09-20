using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Rockets.Log.RocketEvents;

public class PipeFailEvent : RocketEvent
{
	private readonly PipeBurst _damageSource;

	private readonly Pipe _pipe;

	private readonly string _pipeDisplayName;

	public override string TextColor => "red";

	public override RocketEventType RocketEventType => RocketEventType.PipeFail;

	public override string GetText()
	{
		string arg = (((_damageSource & PipeBurst.Pressure) != PipeBurst.None) ? GameStrings.OverPressure.DisplayString : string.Empty) + " " + (((_damageSource & PipeBurst.Liquid) != PipeBurst.None) ? GameStrings.PresenceOfLiquid.DisplayString : string.Empty) + " " + (((_damageSource & PipeBurst.Solid) != PipeBurst.None) ? GameStrings.Frozen.DisplayString : string.Empty);
		return GameStrings.PipeFail.AsString(_pipeDisplayName, arg);
	}

	public PipeFailEvent(IReferencable eventOrigin, PipeBurst damageSource, Pipe pipe)
		: base(eventOrigin)
	{
		_damageSource = damageSource;
		_pipe = pipe;
		_pipeDisplayName = pipe.DisplayName;
		Hash = (Hash ^ (int)(_pipe?.ReferenceId ?? 0)) * 41;
		Hash = (Hash ^ 6) * 41;
	}

	public PipeFailEvent(RocketBinaryReader reader)
		: base(reader)
	{
		_damageSource = (PipeBurst)reader.ReadByte();
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			_pipe = Referencable.Find<Pipe>(referenceId);
			_pipeDisplayName = _pipe?.DisplayName ?? string.Empty;
		}
		else
		{
			_pipeDisplayName = reader.ReadString();
		}
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		bool flag = _pipe != null;
		writer.WriteByte((byte)_damageSource);
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, (_pipe != null) ? _pipe.ReferenceId : 0);
		}
		else
		{
			writer.WriteString(_pipeDisplayName);
		}
	}
}
