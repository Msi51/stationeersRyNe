namespace Assets.Scripts.Atmospherics;

public readonly struct CombustionValue(Chemistry.GasType gasType, double quantity)
{
	public readonly Chemistry.GasType GasType = gasType;

	public readonly MoleQuantity Quantity = new MoleQuantity(quantity);
}
