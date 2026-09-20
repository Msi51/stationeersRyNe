using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Objects.Electrical;

namespace Assets.Scripts.Networking;

public class SetLogicStepSequencerMessage : ProcessedMessage<SetLogicStepSequencerMessage>
{
	public long[] StepUnitIds;

	public long StepSequencerId;

	public long OutputDeviceId;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		AudioSequencer audioSequencer = Thing.Find<AudioSequencer>(StepSequencerId);
		StepUnit[] array = new StepUnit[StepUnitIds.Length];
		for (int i = 0; i < StepUnitIds.Length; i++)
		{
			array[i] = Thing.Find<StepUnit>(StepUnitIds[i]);
		}
		IAudioInput audioOutput = Thing.Find<Thing>(OutputDeviceId) as IAudioInput;
		if (!(audioSequencer == null))
		{
			_ = OutputDeviceId;
			for (int j = 0; j < array.Length; j++)
			{
				if (!(array[j] == null))
				{
					if (audioSequencer.StepUnits[j] != null)
					{
						audioSequencer.StepUnits[j].OnPlayStepManual -= audioSequencer.PlayStep;
					}
					audioSequencer.StepUnits[j] = array[j];
					audioSequencer.StepUnits[j].OnPlayStepManual += audioSequencer.PlayStep;
				}
			}
			audioSequencer.AudioOutput = audioOutput;
		}
		else
		{
			DeferredMessageQueue.DeferUntilExists(this, hostId, StepSequencerId, OutputDeviceId, 10f, "SetLogicStepSequencerMessage");
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		int num = reader.ReadInt32();
		StepUnitIds = new long[num];
		for (int i = 0; i < num; i++)
		{
			StepUnitIds[i] = reader.ReadInt64();
		}
		StepSequencerId = reader.ReadInt64();
		OutputDeviceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(StepUnitIds.Length);
		long[] stepUnitIds = StepUnitIds;
		foreach (long value in stepUnitIds)
		{
			writer.WriteInt64(value);
		}
		writer.WriteInt64(StepSequencerId);
		writer.WriteInt64(OutputDeviceId);
	}
}
