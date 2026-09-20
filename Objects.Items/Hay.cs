using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Objects.Electrical;

namespace Objects.Items;

public class Hay : Stackable, ICompostable, IFermentable
{
	private static readonly SpawnGas[] _spawnGasList = new SpawnGas[2]
	{
		new SpawnGas(Chemistry.GasType.LiquidAlcohol, 3f, TemperatureKelvin.FromCelsius(29f)),
		new SpawnGas(Chemistry.GasType.PollutedWater, 0.15f, TemperatureKelvin.FromCelsius(29f))
	};

	public float BiomassValue => 1f;

	public CompostType CompostType => CompostType.GrowthCycles;

	public SpawnGas[] SpawnGasList => _spawnGasList;
}
