namespace Assets.Scripts.Atmospherics;

public static class IdealGas
{
	public static TemperatureKelvin Temperature(MoleEnergy moleEnergy, HeatCapacity heatCapacity)
	{
		if (heatCapacity.Equals(HeatCapacity.Zero))
		{
			return TemperatureKelvin.Zero;
		}
		return new TemperatureKelvin(moleEnergy, heatCapacity);
	}

	public static MoleEnergy Energy(MoleQuantity quantity, double latentHeat)
	{
		return new MoleEnergy(quantity, latentHeat);
	}

	public static MoleQuantity Quantity(MoleEnergy energy, double latentHeat)
	{
		return new MoleQuantity(energy, latentHeat);
	}

	public static MoleQuantity Quantity(VolumeLitres volume, VolumeLitres volumePerMole)
	{
		return new MoleQuantity(volume.ToDouble() / volumePerMole.ToDouble());
	}

	public static MoleEnergy Energy(TemperatureKelvin temperature, SpecificHeat specificHeat, MoleQuantity quantity)
	{
		return new MoleEnergy(temperature, specificHeat, quantity);
	}

	public static MoleEnergy EnergyPerMole(TemperatureKelvin temperature, SpecificHeat specificHeat)
	{
		return Energy(temperature, specificHeat, MoleQuantity.One);
	}

	public static MoleEnergy Energy(HeatCapacity heatCapacity, TemperatureKelvin temperatureKelvin)
	{
		return new MoleEnergy(heatCapacity, temperatureKelvin);
	}

	public static TemperatureKelvin Temperature(PressurekPa pressure, VolumeLitres volume, MoleQuantity quantity)
	{
		return new TemperatureKelvin(pressure, volume, quantity);
	}

	public static PressurekPa Pressure(MoleQuantity quantity, TemperatureKelvin temperature, VolumeLitres volume)
	{
		return new PressurekPa(quantity, temperature, volume);
	}

	public static MoleQuantity Quantity(PressurekPa pressure, VolumeLitres volume, TemperatureKelvin temperature)
	{
		return new MoleQuantity(pressure, volume, temperature);
	}

	public static VolumeLitres Volume(MoleQuantity quantity, TemperatureKelvin temperature, PressurekPa pressure)
	{
		return new VolumeLitres(quantity, temperature, pressure);
	}

	public static PressurekPa PressurePerMole(TemperatureKelvin temperature, VolumeLitres volume)
	{
		return Pressure(new MoleQuantity(1.0), temperature, volume);
	}

	public static double GetMilliMolesPerLitre(VolumeLitres volume, MoleQuantity quantity)
	{
		quantity *= 1000.0;
		if (volume <= VolumeLitres.Zero || quantity <= MoleQuantity.Zero)
		{
			return 0.0;
		}
		return quantity.ToDouble() / volume.ToDouble();
	}
}
