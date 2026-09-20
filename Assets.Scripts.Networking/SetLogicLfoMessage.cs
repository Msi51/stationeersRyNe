using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicLfoMessage : ProcessedMessage<SetLogicLfoMessage>
{
	public long LfoId;

	public long OutputDeviceId;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LfoVolume lfoVolume = Thing.Find<LfoVolume>(LfoId);
			IAudioInput audioOutput = Thing.Find<Thing>(OutputDeviceId) as IAudioInput;
			if (!(lfoVolume == null))
			{
				_ = OutputDeviceId;
				lfoVolume.AudioOutput = audioOutput;
			}
			else
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LfoId, OutputDeviceId, 10f, "SetLogicLfoMessage");
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LfoId = reader.ReadInt64();
		OutputDeviceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LfoId);
		writer.WriteInt64(OutputDeviceId);
	}
}
