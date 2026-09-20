using System;

namespace Objects.Items;

[Serializable]
public class InstrumentData
{
	public string Name;

	public bool PitchShiftSamples;

	public SampleData[] SampleData = new SampleData[128];
}
