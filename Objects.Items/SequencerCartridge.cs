using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace Objects.Items;

public class SequencerCartridge : Item
{
	public InstrumentData[] InstrumentData = new InstrumentData[0];

	public static readonly int UpperPitchOffsetRange = 19;

	public string[] InstrumentStrings;

	public override string[] ModeStrings => InstrumentStrings;

	public override void Awake()
	{
		base.Awake();
		InstrumentStrings = new string[InstrumentData.Length];
		for (int i = 0; i < InstrumentData.Length; i++)
		{
			InstrumentStrings[i] = InstrumentData[i].Name;
		}
	}

	public SampleData GetSampleData(int pitch)
	{
		if (pitch < 0 || pitch > StepUnit.MaxMidiValue)
		{
			return null;
		}
		if (Mode >= InstrumentData.Length)
		{
			return null;
		}
		if (!InstrumentData[Mode].SampleData[pitch].IsEnabled)
		{
			return null;
		}
		return InstrumentData[Mode].SampleData[pitch];
	}
}
