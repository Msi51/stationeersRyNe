namespace Assets.Scripts.Atmospherics;

public interface IInternalConditionerHandler
{
	MoleQuantity SetGasToTank(MoleQuantity minimumMolesToMove);

	MoleQuantity AirConditioning(Atmosphere selectedAtmosphere);

	MoleQuantity GetGasFromTank();
}
