using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Objects.Electrical;
using Objects.Items;
using Reagents;

public class OrganicMaterial : Ore, ICompostable, IFermentable
{
	private static readonly SpawnGas[] _spawnGasList = new SpawnGas[2]
	{
		new SpawnGas(Chemistry.GasType.LiquidAlcohol, 10f, TemperatureKelvin.FromCelsius(29f)),
		new SpawnGas(Chemistry.GasType.PollutedWater, 1f, TemperatureKelvin.FromCelsius(29f))
	};

	public override bool IsCentrifugeSmelt => false;

	public CompostType CompostType => CompostType.GrowthCycles;

	public SpawnGas[] SpawnGasList => _spawnGasList;

	public override ReagentMixture CentrifugeProcessUnit()
	{
		ReagentMixture reagentMixture = new ReagentMixture();
		if (CreatedReagentMixture.TotalReagents > 0.0)
		{
			if (CreatedReagentMixture.TotalReagents > 1.0)
			{
				CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
			}
			reagentMixture.Add(CreatedReagentMixture);
		}
		base.Quantity--;
		return reagentMixture;
	}
}
