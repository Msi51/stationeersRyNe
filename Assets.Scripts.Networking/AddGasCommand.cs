using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class AddGasCommand : ProcessedMessage<AddGasCommand>
{
	public long Target;

	public float Oxygen;

	public float Nitrogen;

	public float CarbonDioxide;

	public float Methane;

	public float Pollutant;

	public float Water;

	public float PollutedWater;

	public float NitrousOxide;

	public float LiquidNitrogen;

	public float LiquidOxygen;

	public float LiquidMethane;

	public float Steam;

	public float LiquidCarbonDioxide;

	public float LiquidPollutant;

	public float LiquidNitrousOxide;

	public float Hydrogen;

	public float LiquidHydrogen;

	public float Hydrazine;

	public float LiquidHydrazine;

	public float LiquidAlcohol;

	public float Helium;

	public float SodiumChloride;

	public float Silanol;

	public float LiquidSilanol;

	public float HydrochloricAcid;

	public float LiquidHydrochloricAcid;

	public float Ozone;

	public float LiquidOzone;

	public float Temperature;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		IReferencable referencable = Referencable.Find(Target);
		if (referencable == null)
		{
			return;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Oxygen.Set(new Mole(Chemistry.GasType.Oxygen, new MoleQuantity(Oxygen), MoleEnergy.Zero));
		gasMixture.Nitrogen.Set(new Mole(Chemistry.GasType.Nitrogen, new MoleQuantity(Nitrogen), MoleEnergy.Zero));
		gasMixture.CarbonDioxide.Set(new Mole(Chemistry.GasType.CarbonDioxide, new MoleQuantity(CarbonDioxide), MoleEnergy.Zero));
		gasMixture.Methane.Set(new Mole(Chemistry.GasType.Methane, new MoleQuantity(Methane), MoleEnergy.Zero));
		gasMixture.Pollutant.Set(new Mole(Chemistry.GasType.Pollutant, new MoleQuantity(Pollutant), MoleEnergy.Zero));
		gasMixture.Water.Set(new Mole(Chemistry.GasType.Water, new MoleQuantity(Water), MoleEnergy.Zero));
		gasMixture.PollutedWater.Set(new Mole(Chemistry.GasType.PollutedWater, new MoleQuantity(PollutedWater), MoleEnergy.Zero));
		gasMixture.NitrousOxide.Set(new Mole(Chemistry.GasType.NitrousOxide, new MoleQuantity(NitrousOxide), MoleEnergy.Zero));
		gasMixture.LiquidNitrogen.Set(new Mole(Chemistry.GasType.LiquidNitrogen, new MoleQuantity(LiquidNitrogen), MoleEnergy.Zero));
		gasMixture.LiquidOxygen.Set(new Mole(Chemistry.GasType.LiquidOxygen, new MoleQuantity(LiquidOxygen), MoleEnergy.Zero));
		gasMixture.LiquidMethane.Set(new Mole(Chemistry.GasType.LiquidMethane, new MoleQuantity(LiquidMethane), MoleEnergy.Zero));
		gasMixture.Steam.Set(new Mole(Chemistry.GasType.Steam, new MoleQuantity(Steam), MoleEnergy.Zero));
		gasMixture.LiquidCarbonDioxide.Set(new Mole(Chemistry.GasType.LiquidCarbonDioxide, new MoleQuantity(LiquidCarbonDioxide), MoleEnergy.Zero));
		gasMixture.LiquidPollutant.Set(new Mole(Chemistry.GasType.LiquidPollutant, new MoleQuantity(LiquidPollutant), MoleEnergy.Zero));
		gasMixture.LiquidNitrousOxide.Set(new Mole(Chemistry.GasType.LiquidNitrousOxide, new MoleQuantity(LiquidNitrousOxide), MoleEnergy.Zero));
		gasMixture.Hydrogen.Set(new Mole(Chemistry.GasType.Hydrogen, new MoleQuantity(Hydrogen), MoleEnergy.Zero));
		gasMixture.LiquidHydrogen.Set(new Mole(Chemistry.GasType.LiquidHydrogen, new MoleQuantity(LiquidHydrogen), MoleEnergy.Zero));
		gasMixture.Hydrazine.Set(new Mole(Chemistry.GasType.Hydrazine, new MoleQuantity(Hydrazine), MoleEnergy.Zero));
		gasMixture.LiquidHydrazine.Set(new Mole(Chemistry.GasType.LiquidHydrazine, new MoleQuantity(LiquidHydrazine), MoleEnergy.Zero));
		gasMixture.LiquidAlcohol.Set(new Mole(Chemistry.GasType.LiquidAlcohol, new MoleQuantity(LiquidAlcohol), MoleEnergy.Zero));
		gasMixture.Helium.Set(new Mole(Chemistry.GasType.Helium, new MoleQuantity(Helium), MoleEnergy.Zero));
		gasMixture.LiquidSodiumChloride.Set(new Mole(Chemistry.GasType.LiquidSodiumChloride, new MoleQuantity(SodiumChloride), MoleEnergy.Zero));
		gasMixture.Silanol.Set(new Mole(Chemistry.GasType.Silanol, new MoleQuantity(Silanol), MoleEnergy.Zero));
		gasMixture.LiquidSilanol.Set(new Mole(Chemistry.GasType.LiquidSilanol, new MoleQuantity(LiquidSilanol), MoleEnergy.Zero));
		gasMixture.HydrochloricAcid.Set(new Mole(Chemistry.GasType.HydrochloricAcid, new MoleQuantity(HydrochloricAcid), MoleEnergy.Zero));
		gasMixture.LiquidHydrochloricAcid.Set(new Mole(Chemistry.GasType.LiquidHydrochloricAcid, new MoleQuantity(LiquidHydrochloricAcid), MoleEnergy.Zero));
		gasMixture.Ozone.Set(new Mole(Chemistry.GasType.Ozone, new MoleQuantity(Ozone), MoleEnergy.Zero));
		gasMixture.LiquidOzone.Set(new Mole(Chemistry.GasType.LiquidOzone, new MoleQuantity(LiquidOzone), MoleEnergy.Zero));
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, new TemperatureKelvin(Temperature));
		if (referencable is PipeNetwork pipeNetwork)
		{
			AtmosphericEventInstance.CreateAdd(pipeNetwork.Atmosphere, gasMixture);
		}
		else if (referencable is Thing thing)
		{
			if (thing.InternalAtmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(thing.InternalAtmosphere, gasMixture);
			}
			else if (thing is Pipe pipe)
			{
				AtmosphericEventInstance.CreateAdd(pipe.PipeNetwork.Atmosphere, gasMixture);
			}
		}
		else if (referencable is Atmosphere atmosphere)
		{
			AtmosphericEventInstance.CreateAdd(atmosphere, gasMixture);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Target = reader.ReadInt64();
		Oxygen = reader.ReadSingle();
		Nitrogen = reader.ReadSingle();
		CarbonDioxide = reader.ReadSingle();
		Methane = reader.ReadSingle();
		Pollutant = reader.ReadSingle();
		Water = reader.ReadSingle();
		PollutedWater = reader.ReadSingle();
		NitrousOxide = reader.ReadSingle();
		LiquidNitrogen = reader.ReadSingle();
		LiquidOxygen = reader.ReadSingle();
		LiquidMethane = reader.ReadSingle();
		Steam = reader.ReadSingle();
		LiquidCarbonDioxide = reader.ReadSingle();
		LiquidPollutant = reader.ReadSingle();
		LiquidNitrousOxide = reader.ReadSingle();
		Hydrogen = reader.ReadSingle();
		LiquidHydrogen = reader.ReadSingle();
		Hydrazine = reader.ReadSingle();
		LiquidHydrazine = reader.ReadSingle();
		LiquidAlcohol = reader.ReadSingle();
		Helium = reader.ReadSingle();
		SodiumChloride = reader.ReadSingle();
		Silanol = reader.ReadSingle();
		LiquidSilanol = reader.ReadSingle();
		HydrochloricAcid = reader.ReadSingle();
		LiquidHydrochloricAcid = reader.ReadSingle();
		Ozone = reader.ReadSingle();
		LiquidOzone = reader.ReadSingle();
		Temperature = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(Target);
		writer.WriteSingle(Oxygen);
		writer.WriteSingle(Nitrogen);
		writer.WriteSingle(CarbonDioxide);
		writer.WriteSingle(Methane);
		writer.WriteSingle(Pollutant);
		writer.WriteSingle(Water);
		writer.WriteSingle(PollutedWater);
		writer.WriteSingle(NitrousOxide);
		writer.WriteSingle(LiquidNitrogen);
		writer.WriteSingle(LiquidOxygen);
		writer.WriteSingle(LiquidMethane);
		writer.WriteSingle(Steam);
		writer.WriteSingle(LiquidCarbonDioxide);
		writer.WriteSingle(LiquidPollutant);
		writer.WriteSingle(LiquidNitrousOxide);
		writer.WriteSingle(Hydrogen);
		writer.WriteSingle(LiquidHydrogen);
		writer.WriteSingle(Hydrazine);
		writer.WriteSingle(LiquidHydrazine);
		writer.WriteSingle(LiquidAlcohol);
		writer.WriteSingle(Helium);
		writer.WriteSingle(SodiumChloride);
		writer.WriteSingle(Silanol);
		writer.WriteSingle(LiquidSilanol);
		writer.WriteSingle(HydrochloricAcid);
		writer.WriteSingle(LiquidHydrochloricAcid);
		writer.WriteSingle(Ozone);
		writer.WriteSingle(LiquidOzone);
		writer.WriteSingle(Temperature);
	}
}
