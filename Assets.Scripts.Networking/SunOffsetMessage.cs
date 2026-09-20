using System;

namespace Assets.Scripts.Networking;

public class SunOffsetMessage : ProcessedMessage<SunOffsetMessage>
{
	public float TimeOffset;

	public override void Process(long hostId)
	{
		throw new Exception("obsolete method");
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		TimeOffset = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteSingle(TimeOffset);
	}
}
