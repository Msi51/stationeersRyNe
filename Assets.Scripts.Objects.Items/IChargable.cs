using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IChargable : IReferencable, IEvaluable
{
	float PowerStored { get; set; }

	float PowerRatio { get; }

	float PowerDelta { get; }

	bool IsEmpty { get; }

	bool IsCharged { get; }

	float GetPowerMaximum();

	static void SetPower(IChargable chargable, BatteryCellState state)
	{
		chargable.PowerStored = state switch
		{
			BatteryCellState.Empty => chargable.GetPowerMaximum() * 0.001f, 
			BatteryCellState.Critical => chargable.GetPowerMaximum() * 0.05f, 
			BatteryCellState.Low => chargable.GetPowerMaximum() * 0.1f, 
			BatteryCellState.VeryLow => chargable.GetPowerMaximum() * 0.25f, 
			BatteryCellState.Medium => chargable.GetPowerMaximum() * 0.5f, 
			BatteryCellState.High => chargable.GetPowerMaximum() * 0.75f, 
			BatteryCellState.Full => chargable.GetPowerMaximum() * 0.999f, 
			_ => chargable.PowerStored, 
		};
	}

	static BatteryCellState GetState(IChargable chargable)
	{
		float powerRatio = chargable.PowerRatio;
		if (powerRatio <= 0.001f)
		{
			return BatteryCellState.Empty;
		}
		if (powerRatio <= 0.05f)
		{
			return BatteryCellState.Critical;
		}
		if (powerRatio <= 0.1f)
		{
			return BatteryCellState.Low;
		}
		if (powerRatio <= 0.25f)
		{
			return BatteryCellState.VeryLow;
		}
		if (powerRatio <= 0.5f)
		{
			return BatteryCellState.Medium;
		}
		if (powerRatio <= 0.75f)
		{
			return BatteryCellState.High;
		}
		return BatteryCellState.Full;
	}
}
