using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Objects.Electrical;

namespace Objects.Items;

public class DecayedFood : Stackable, ICompostable, IFermentable
{
	private static readonly SpawnGas[] SpawnGases = new SpawnGas[2]
	{
		new SpawnGas(Chemistry.GasType.LiquidAlcohol, 1f, TemperatureKelvin.FromCelsius(29f)),
		new SpawnGas(Chemistry.GasType.PollutedWater, 0.05f, TemperatureKelvin.FromCelsius(29f))
	};

	public float BiomassValue => 1f;

	public CompostType CompostType => CompostType.GrowthSpeed;

	public float ProcessTime => 1f;

	public SpawnGas[] SpawnGasList => SpawnGases;
}
