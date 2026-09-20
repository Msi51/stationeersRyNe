namespace Objects.Electrical;

public struct StepData
{
	public bool Enabled;

	public sbyte Pitch;

	public sbyte Velocity;

	public void ClearData()
	{
		Enabled = false;
		Pitch = 0;
		Velocity = 0;
	}

	public void SetData(StepData stepData)
	{
		Enabled = stepData.Enabled;
		Pitch = stepData.Pitch;
		Velocity = stepData.Velocity;
	}
}
