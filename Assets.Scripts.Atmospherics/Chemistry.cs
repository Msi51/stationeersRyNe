using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Atmospherics;

public static class Chemistry
{
	public static class Limits
	{
		private const double PRESSURE_MINIMUM_SAFE = 20.0;

		private const double PRESSURE_MAXIMUM_SAFE = 607.9499816894531;

		public static readonly PressurekPa PressureMinimumSafe = new PressurekPa(20.0);

		public static readonly PressurekPa PressureMaximumSafe = new PressurekPa(607.9499816894531);

		public static readonly PressurekPa MAXPressureGasPipe = new PressurekPa(60794.99816894531);

		public static readonly PressurekPa MAXPressureLiquidPipe = new PressurekPa(6079.499816894531);

		public static readonly PressurekPa MAXPressureGasDuct = new PressurekPa(1215.8999633789062);
	}

	public static class Temperature
	{
		public const double ZERO_DEGREES = 273.15;

		public const double ONE_DEGREE = 274.15;

		public const double TWENTY_DEGREES = 293.15;

		public const double THIRTY_DEGREES = 303.15;

		public const double FIFTY_DEGREES = 323.15;

		public const double MINIMUM = 1.0;

		public const double MAXIMUM = 80000.0;

		private const double MIN_SUIT_SETTING = 273.15;

		private const double MAX_SUIT_SETTING = 333.15;

		public static readonly TemperatureKelvin ZeroDegrees = new TemperatureKelvin(273.15);

		public static readonly TemperatureKelvin OneDegree = new TemperatureKelvin(274.15);

		public static readonly TemperatureKelvin TwentyDegrees = new TemperatureKelvin(293.15);

		public static readonly TemperatureKelvin ThirtyDegrees = new TemperatureKelvin(303.15);

		public static readonly TemperatureKelvin FiftyDegrees = new TemperatureKelvin(323.15);

		public static readonly TemperatureKelvin Minimum = new TemperatureKelvin(1.0);

		public static readonly TemperatureKelvin Maximum = new TemperatureKelvin(80000.0);

		public static readonly TemperatureKelvin MINSuitSetting = new TemperatureKelvin(273.15);

		public static readonly TemperatureKelvin MAXSuitSetting = new TemperatureKelvin(333.15);
	}

	public static class Pressure
	{
		public const double MINIMUM = 0.0;

		public const double MAXIMUM = 1000000.0;

		public static readonly PressurekPa Minimum = new PressurekPa(0.0);

		public static readonly PressurekPa Maximum = new PressurekPa(1000000.0);
	}

	[Flags]
	public enum GasType : uint
	{
		[XmlEnum("Undefined")]
		Undefined = 0u,
		[XmlEnum("Oxygen")]
		Oxygen = 1u,
		[XmlEnum("Nitrogen")]
		Nitrogen = 2u,
		[XmlEnum("CarbonDioxide")]
		CarbonDioxide = 4u,
		[XmlEnum("Volatiles")]
		Methane = 8u,
		[XmlEnum("Pollutant")]
		Pollutant = 0x10u,
		[XmlEnum("Water")]
		Water = 0x20u,
		[XmlEnum("NitrousOxide")]
		NitrousOxide = 0x40u,
		[XmlEnum("LiquidNitrogen")]
		LiquidNitrogen = 0x80u,
		[XmlEnum("LiquidOxygen")]
		LiquidOxygen = 0x100u,
		[XmlEnum("LiquidVolatiles")]
		LiquidMethane = 0x200u,
		[XmlEnum("Steam")]
		Steam = 0x400u,
		[XmlEnum("LiquidCarbonDioxide")]
		LiquidCarbonDioxide = 0x800u,
		[XmlEnum("LiquidPollutant")]
		LiquidPollutant = 0x1000u,
		[XmlEnum("LiquidNitrousOxide")]
		LiquidNitrousOxide = 0x2000u,
		[XmlEnum("Hydrogen")]
		Hydrogen = 0x4000u,
		[XmlEnum("LiquidHydrogen")]
		LiquidHydrogen = 0x8000u,
		[XmlEnum("PollutedWater")]
		PollutedWater = 0x10000u,
		[XmlEnum("Hydrazine")]
		Hydrazine = 0x20000u,
		[XmlEnum("LiquidHydrazine")]
		LiquidHydrazine = 0x40000u,
		[XmlEnum("LiquidAlcohol")]
		LiquidAlcohol = 0x80000u,
		[XmlEnum("Helium")]
		Helium = 0x100000u,
		[XmlEnum("LiquidSodiumChloride")]
		LiquidSodiumChloride = 0x200000u,
		[XmlEnum("Silanol")]
		Silanol = 0x400000u,
		[XmlEnum("LiquidSilanol")]
		LiquidSilanol = 0x800000u,
		[XmlEnum("HydrochloricAcid")]
		HydrochloricAcid = 0x1000000u,
		[XmlEnum("LiquidHydrochloricAcid")]
		LiquidHydrochloricAcid = 0x2000000u,
		[XmlEnum("Ozone")]
		Ozone = 0x4000000u,
		[XmlEnum("LiquidOzone")]
		LiquidOzone = 0x8000000u,
		[XmlEnum("Air")]
		Air = 3u,
		[XmlEnum("Fuel")]
		Fuel = 9u
	}

	public const double StefanBoltzConstant = 5.6703E-08;

	public const double IdealGasConstant = 8.3144;

	public const double R = 8.3144;

	public const float ONE_ATMOSPHERE = 101.325f;

	public const double MAX_PIPE_PRESSURE = 1013249.9694824219;

	public static readonly PressurekPa OneAtmosphere = new PressurekPa(101.32499694824219);

	public const float EARTH_STANDARD_GRAVITY = 9.8f;

	public const float SMALLEST_NORMAL_NUMBER = 1.1754944E-38f;

	private const double MIN_GAS_VOLUME = 0.10000000149011612;

	public const double GRID_VOLUME = 8000.0;

	private const int VOXELS_PER_GRID = 8;

	public const double LIQUID_PIPE_VOLUME = 20.0;

	public const double PIPE_VOLUME = 10.0;

	public const double GLOBAL_GRIDS = 5000000.0;

	public const double DEFAULT_GLOBAL_VOLUME = 40000000000.0;

	public static readonly VolumeLitres GridVolume = new VolumeLitres(8000.0);

	public static readonly VolumeLitres VoxelVolume = new VolumeLitres(1000.0);

	public static readonly VolumeLitres PipeVolume = new VolumeLitres(10.0);

	public static readonly VolumeLitres LiquidPipeVolume = new VolumeLitres(20.0);

	public static readonly VolumeLitres MinimumGasVolume = new VolumeLitres(0.10000000149011612);

	public const float DEFAULT_LIQUID_VOLUME_SAFETY_RATIO = 0.99f;

	public const byte VOXEL_FACE_OPEN_RATIO_BYTE = 127;

	public const float VOXEL_FACE_OPEN_RATIO = 0.49803922f;

	public const double MIN_WORLD_ATMOSPHERE_VOLUME = 498.03921580314636;

	public static readonly VolumeLitres MinWorldAtmopshereVolume = new VolumeLitres(498.03921580314636);

	public static readonly MoleQuantity MINIMUM_QUANTITY_MOLES = new MoleQuantity(1E-05);

	public static readonly MoleQuantity MINIMUM_VALID_TOTAL_MOLES = new MoleQuantity(0.001);

	public static readonly MoleQuantity MINIMUM_WORLD_VALID_TOTAL_MOLES = new MoleQuantity(0.1);

	private const double MELT_ICE_PRESSURE_THRESHOLD = 1.0;

	public const double ARMSTRONG_LIMIT = 6.3;

	private const double PRESSURE_FULLY_APPLY_VELOCITY = 90.0;

	private const double MINIMUM_OXYGEN_PARTIAL_PRESSURE = 16.0;

	public static readonly PressurekPa MeltIcePressureThreshold = new PressurekPa(1.0);

	public static readonly PressurekPa ArmstrongLimit = new PressurekPa(6.3);

	public static readonly PressurekPa PressureFullyApplyVelocity = new PressurekPa(90.0);

	public static readonly PressurekPa MinimumOxygenPartialPressure = new PressurekPa(16.0);

	private const double RESET_THRESHOLD = 0.001;

	public static readonly PressurekPa ResetThreshold = new PressurekPa(0.001);

	public static readonly double MonatomicDegreesOfFreedom = 1.666666;

	public static readonly double DiatomicDegreesOfFreedom = 1.4;

	public static readonly double TriatomicDegreesOfFreedom = 1.333333;

	public static readonly double PolyatomicDegreesOfFreedom = 1.26;

	public const double SPECIFIC_HEAT_CARBON_DIOXIDE = 28.2;

	public const float TRIPLE_POINT_PRESSURE_CARBON_DIOXIDE = 517f;

	public const float LATENT_HEAT_CARBON_DIOXIDE = 600f;

	public const float CRITICAL_PRESSURE_CARBON_DIOXIDE = 6000f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_CARBON_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.CarbonDioxide, new PressurekPa(517.0));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_CARBON_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.CarbonDioxide, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_POLLUTANT = 24.8;

	public const float TRIPLE_POINT_PRESSURE_SULFUR_DIOXIDE = 1800f;

	public const float LATENT_HEAT_SULFUR_DIOXIDE = 2000f;

	public const float CRITICAL_PRESSURE_SULFUR_DIOXIDE = 6000f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_SULFUR_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.Pollutant, new PressurekPa(1800.0));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_SULFUR_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.Pollutant, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_NITROGEN_DIOXIDE = 37.2;

	public const float TRIPLE_POINT_PRESSURE_NITROGEN_DIOXIDE = 800f;

	public const float LATENT_HEAT_NITROGEN_DIOXIDE = 4000f;

	public const float CRITICAL_PRESSURE_NITROGEN_DIOXIDE = 2000f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_NITROGEN_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.NitrousOxide, new PressurekPa(800.0));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_NITROGEN_DIOXIDE_K = MoleHelper.EvaporationTemperature(GasType.NitrousOxide, new PressurekPa(2000.0));

	public const double ENTHALPY_MULTIPLIER_NITROGEN_DIOXIDE = 2.0;

	public const double AUTO_IGNITION_OFFSET_NITROGEN_DIOXIDE = -250.0;

	public static readonly TemperatureKelvin AutoIgnitionOffsetNitrogenDioxide = new TemperatureKelvin(-250.0);

	public const double SPECIFIC_HEAT_NITROGEN = 20.6;

	public const float TRIPLE_POINT_PRESSURE_NITROGEN = 6.3f;

	public const float LATENT_HEAT_NITROGEN = 500f;

	public const float CRITICAL_PRESSURE_NITROGEN = 6000f;

	public static readonly TemperatureKelvin FREEZING_TEMPERATURE_NITROGEN_K = MoleHelper.EvaporationTemperature(GasType.Nitrogen, new PressurekPa(6.300000190734863));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_NITROGEN_K = MoleHelper.EvaporationTemperature(GasType.Nitrogen, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_OXYGEN = 21.1;

	public const float TRIPLE_POINT_PRESSURE_OXYGEN = 6.3f;

	public const float LATENT_HEAT_OXYGEN = 800f;

	public const float CRITICAL_PRESSURE_OXYGEN = 6000f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_OXYGEN_K = MoleHelper.EvaporationTemperature(GasType.Oxygen, new PressurekPa(6.300000190734863));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_OXYGEN_K = MoleHelper.EvaporationTemperature(GasType.Oxygen, new PressurekPa(6000.0));

	public const double ENTHAPLY_MULTILPIER_OXYGEN = 1.0;

	public const double SPECIFIC_HEAT_METHANE = 20.4;

	public const float TRIPLE_POINT_PRESSURE_METHANE = 6.3f;

	public const float LATENT_HEAT_METHANE = 1000f;

	public const float CRITICAL_PRESSURE_METHANE = 6000f;

	public const float ENTHALPY_METHANE = 286000f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_METHANE_K = MoleHelper.EvaporationTemperature(GasType.Methane, new PressurekPa(6.300000190734863));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_METHANE_K = MoleHelper.EvaporationTemperature(GasType.Methane, new PressurekPa(6000.0));

	private const double METHANE_AUTOIGNITION = 573.15;

	public static readonly TemperatureKelvin AutoIgnitionMethane = new TemperatureKelvin(573.15);

	public const double SPECIFIC_HEAT_WATER = 72.0;

	public const float LATENT_HEAT_WATER = 8000f;

	public const float CRITICAL_PRESSURE_WATER = 6000f;

	public const float TRIPLE_POINT_PRESSURE_WATER = 6.3f;

	private const float _TRIPLE_POINT_TEMPERATURE_WATER_K = 273.15f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_WATER_K = new TemperatureKelvin(273.1499938964844);

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_WATER_K = MoleHelper.EvaporationTemperature(GasType.Water, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_POLLUTED_WATER = 72.0;

	public const float TRIPLE_POINT_PRESSURE_POLLUTED_WATER = 6.3f;

	public const float LATENT_HEAT_POLLUTED_WATER = 8000f;

	public const float CRITICAL_PRESSURE_POLLUTED_WATER = 6000f;

	private const float _TRIPLE_POINT_TEMPERATURE_POLLUTED_WATER_K = 276.15f;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_POLLUTED_WATER_K = new TemperatureKelvin(276.1499938964844);

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_POLLUTED_WATER_K = MoleHelper.EvaporationTemperature(GasType.PollutedWater, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_HYDROGEN = 20.4;

	public const float TRIPLE_POINT_PRESSURE_HYDROGEN = 6.3f;

	public const float LATENT_HEAT_HYDROGEN = 200f;

	public const float CRITICAL_PRESSURE_HYDROGEN = 6000f;

	public const double MOLAR_VOLUME_LIQUID_HYDROGEN = 0.028;

	public const double ENTHALPY_HYDROGEN = 306000.0;

	public static readonly TemperatureKelvin TRIPLE_POINT_TEMPERATURE_HYDROGEN_K = MoleHelper.EvaporationTemperature(GasType.Hydrogen, new PressurekPa(6.300000190734863));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_HYDROGEN_K = MoleHelper.EvaporationTemperature(GasType.Hydrogen, new PressurekPa(6000.0));

	private const double HYDROGEN_AUTOIGNITION = 573.15;

	public static readonly TemperatureKelvin AutoIgnitionHydrogen = new TemperatureKelvin(573.15);

	public const double SPECIFIC_HEAT_HYDRAZINE = 48.4;

	public const double MINIMUM_LIQUID_PRESSURE_HYDRAZINE = 6.3;

	public const double LATENT_HEAT_HYDRAZINE = 4000.0;

	public const double CRITICAL_PRESSURE_HYDRAZINE = 6000.0;

	public const double MOLAR_VOLUME_HYDRAZINE = 0.03;

	public const double ENTHALPY_HYDRAZINE = 306000.0;

	public const double COEFFIENT_A_HYDRAZINE = 8E-22;

	public const double COEFFIENT_B_HYDRAZINE = 9.15642808045339;

	public static readonly TemperatureKelvin FREEZING_POINT_HYDRAZINE = MoleHelper.EvaporationTemperature(GasType.Hydrazine, new PressurekPa(6.3));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_HYDRAZINE_K = MoleHelper.EvaporationTemperature(GasType.Hydrazine, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_ETHANOL = 33.0;

	public const double MINIMUM_LIQUID_PRESSURE_ETHANOL = 6.3;

	public const double LATENT_HEAT_ETHANOL = 2000.0;

	public const double CRITICAL_PRESSURE_ETHANOL = 1000.0;

	public const double MOLAR_VOLUME_ETHANOL = 0.058;

	public const double ENTHALPY_ETHANOL = 566000.0;

	public const double COEFFICIENT_A_ETHANOL = 9E-20;

	public const double COEFFICIENT_B_ETHANOL = 8.391884446078986;

	private const double AUTOIGNITION_ALCOHOL = 673.15;

	public static readonly TemperatureKelvin FREEZING_POINT_ETHANOL = MoleHelper.EvaporationTemperature(GasType.LiquidAlcohol, new PressurekPa(6.3));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_ETHANOL_K = MoleHelper.EvaporationTemperature(GasType.LiquidAlcohol, new PressurekPa(1000.0));

	public static readonly TemperatureKelvin AutoIgnitionAlcohol = new TemperatureKelvin(673.15);

	public const double SPECIFIC_HEAT_HELIUM = 20.8;

	public const double MINIMUM_LIQUID_PRESSURE_HELIUM = 0.0;

	public const double LATENT_HEAT_HELIUM = 0.0;

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_HELIUM_K = TemperatureKelvin.Zero;

	public static readonly TemperatureKelvin FREEZING_POINT_HELIUM = TemperatureKelvin.Zero;

	public const double SPECIFIC_HEAT_SODIUM_CHLORIDE = 130.0;

	public const double MINIMUM_LIQUID_PRESSURE_SODIUM_CHLORIDE = 6.3;

	public const double CRITICAL_PRESSURE_SODIUM_CHLORIDE = 515.0;

	public const double LATENT_HEAT_SODIUM_CHLORIDE = 16000.0;

	public const double MOLAR_VOLUME_SODIUM_CHLORIDE = 0.04;

	public static readonly TemperatureKelvin FREEZING_POINT_SODIUMCHLORIDE = MoleHelper.EvaporationTemperature(GasType.LiquidSodiumChloride, new PressurekPa(6.3));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_SODIUMCHLORIDE = MoleHelper.EvaporationTemperature(GasType.LiquidSodiumChloride, new PressurekPa(515.0));

	public const double COEFFICIENT_A_SODIUMCHLORIDE = 6.211737044295E-08;

	public const double COEFFICIENT_B_SODIUMCHLORIDE = 2.8774143233482707;

	public const double SPECIFIC_HEAT_COOLANT = 101.0;

	public const double MINIMUM_LIQUID_PRESSURE_SILANOL = 516.0;

	public const double LATENT_HEAT_COOLANT = 10000.0;

	public const double CRITICAL_PRESSURE_COOLANT = 6000.0;

	public const double MOLAR_VOLUME_SILANOL = 0.16;

	public const double COEFFICIENT_A_SILANOL = 0.48388429552357676;

	public const double COEFFICIENT_B_SILANOL = 1.4041336082044964;

	public static readonly TemperatureKelvin FREEZING_POINT_SILANOL = MoleHelper.EvaporationTemperature(GasType.Silanol, new PressurekPa(516.0));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_SILANOL_K = MoleHelper.EvaporationTemperature(GasType.Silanol, new PressurekPa(6000.0));

	public const double SPECIFIC_HEAT_HYDROCHLORIC_ACID = 37.0;

	public const double MINIMUM_LIQUID_PRESSURE_HYDROCHLORIC_ACID = 6.3;

	public const double LATENT_HEAT_HYDROCHLORIC_ACID = 1000.0;

	public const double CRITICAL_PRESSURE_HYDROCHLORIC_ACID = 1000.0;

	public const double MOLAR_VOLUME_HYDROCHLORIC_ACID = 0.028;

	public const double COEFFICIENT_A_HYDROCHLORIC_ACID = 1E-21;

	public const double COEFFICIENT_B_HYDROCHLORIC_ACID = 9.108844460789863;

	public static readonly TemperatureKelvin FREEZING_POINT_HYDROCHLORIC_ACID = MoleHelper.EvaporationTemperature(GasType.HydrochloricAcid, new PressurekPa(6.3));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_HYDROCHLORIC_ACID_K = MoleHelper.EvaporationTemperature(GasType.HydrochloricAcid, new PressurekPa(1000.0));

	public const double SPECIFIC_HEAT_OZONE = 38.6;

	public const double MINIMUM_LIQUID_PRESSURE_OZONE = 250.0;

	public const double LATENT_HEAT_OZONE = 1000.0;

	public const double CRITICAL_PRESSURE_OZONE = 6000.0;

	public const double MOLAR_VOLUME_OZONE = 0.026;

	public const double COEFFICIENT_A_OZONE = 0.22905767012828845;

	public const double COEFFICIENT_B_OZONE = 1.7787987515586163;

	public static readonly TemperatureKelvin FREEZING_POINT_OZONE = MoleHelper.EvaporationTemperature(GasType.Ozone, new PressurekPa(250.0));

	public static readonly TemperatureKelvin CRITICAL_TEMPERATURE_OZONE_K = MoleHelper.EvaporationTemperature(GasType.Ozone, new PressurekPa(6000.0));

	public const double AUTO_IGNITION_OFFSET_OZONE = -150.0;

	public static readonly TemperatureKelvin AutoIgnitionOffsetOzone = new TemperatureKelvin(-150.0);

	public const double ENTHALPY_MULTIPLIER_OZONE = 2.0;

	public const double MOLAR_MASS_NITROGEN = 64.0;

	public const double MOLAR_MASS_WATER = 108.0;

	public const double MOLAR_MASS_POLLUTED_WATER = 108.0;

	public const double MOLAR_MASS_VOLATILES = 16.0;

	public const double MOLAR_MASS_OXYGEN = 16.0;

	public const double MOLAR_MASS_CARBON_DIOXIDE = 44.0;

	public const double MOLAR_MASS_NITROGEN_DIOXIDE = 46.0;

	public const double MOLAR_MASS_POLLUTANT = 28.0;

	public const double MOLAR_MASS_OZONE = 24.0;

	public const double MOLAR_MASS_HYDROCHLORIC_ACID = 36.0;

	public const double MOLAR_MASS_SILANOL = 166.0;

	public const double MOLAR_MASS_SODIUM_CHLORIDE = 101.0;

	public const double MOLAR_MASS_HELIUM = 4.0;

	public const double MOLAR_MASS_ETHANOL = 18.0;

	public const double MOLAR_MASS_HYDRAZINE = 32.0;

	public const double MOLAR_MASS_HYDROGEN = 2.0;

	public const double MOLAR_VOLUME_WATER = 0.018;

	public const double MOLAR_VOLUME_POLLUTED_WATER = 0.018;

	public const double MOLAR_VOLUME_LIQUID_NITROGEN = 0.0348;

	public const double MOLAR_VOLUME_LIQUID_OXYGEN = 0.03;

	public const double MOLAR_VOLUME_LIQUID_VOLATILES = 0.04;

	public const double MOLAR_VOLUME_NITROGEN_DIOXIDE = 0.026;

	public const double MOLAR_VOLUME_CARBON_DIOXIDE = 0.04;

	public const double MOLAR_VOLUME_POLLUTANT = 0.04;

	public const double EVAPORATION_RATIO_LIQUID_ALCOHOL = 34.0 / 55.0;

	public const double EVAPORATION_RATIO_SODIUM = 62.0 / 325.0;

	public const float CELSIUS_TO_KELVIN = 273.15f;

	public static List<Mole> Gases = new List<Mole>
	{
		Mole.Create(GasType.Oxygen),
		Mole.Create(GasType.Nitrogen),
		Mole.Create(GasType.CarbonDioxide),
		Mole.Create(GasType.Methane),
		Mole.Create(GasType.Pollutant),
		Mole.Create(GasType.Water),
		Mole.Create(GasType.PollutedWater),
		Mole.Create(GasType.NitrousOxide),
		Mole.Create(GasType.LiquidNitrogen),
		Mole.Create(GasType.LiquidOxygen),
		Mole.Create(GasType.LiquidMethane),
		Mole.Create(GasType.Steam),
		Mole.Create(GasType.LiquidCarbonDioxide),
		Mole.Create(GasType.LiquidPollutant),
		Mole.Create(GasType.LiquidNitrousOxide),
		Mole.Create(GasType.Hydrogen),
		Mole.Create(GasType.LiquidHydrogen),
		Mole.Create(GasType.Hydrazine),
		Mole.Create(GasType.LiquidHydrazine),
		Mole.Create(GasType.LiquidAlcohol),
		Mole.Create(GasType.Helium),
		Mole.Create(GasType.LiquidSodiumChloride),
		Mole.Create(GasType.Silanol),
		Mole.Create(GasType.LiquidSilanol),
		Mole.Create(GasType.HydrochloricAcid),
		Mole.Create(GasType.LiquidHydrochloricAcid),
		Mole.Create(GasType.Ozone),
		Mole.Create(GasType.LiquidOzone)
	};

	private static Dictionary<int, string> _gasSymbols = new Dictionary<int, string>
	{
		{ 1, "O2" },
		{ 2, "N2" },
		{ 4, "CO2" },
		{ 8, "CH4" },
		{ 16, "X" },
		{ 32, "H2O" },
		{ 65536, "XH2O" },
		{ 64, "NOS" },
		{ 128, "LN2" },
		{ 256, "LO2" },
		{ 512, "LCH4" },
		{ 1024, "STM" },
		{ 2048, "LCO2" },
		{ 4096, "LX" },
		{ 8192, "LNOS" },
		{ 16384, "H2" },
		{ 32768, "LH2" },
		{ 131072, "HZ" },
		{ 262144, "LHZ" },
		{ 524288, "Al" },
		{ 1048576, "HE" },
		{ 2097152, "NaCl" },
		{ 4194304, "Sil" },
		{ 8388608, "LSil" },
		{ 16777216, "HCl" },
		{ 33554432, "LHCl" },
		{ 67108864, "O3" },
		{ 134217728, "LO3" }
	};

	public static string PascalUnit = "Pa";

	public static string MoleUnit = "mol";

	public static string LitreUnit = "L";

	public static TemperatureKelvin AutoIgnitionHydrazine => CRITICAL_TEMPERATURE_HYDRAZINE_K;

	public static string GetSymbol(GasType gasType)
	{
		return GetSymbol((int)gasType);
	}

	public static string GetSymbol(int gasTypeInt)
	{
		string value = string.Empty;
		_gasSymbols.TryGetValue(gasTypeInt, out value);
		return value;
	}

	public static double SpecificHeat(GasType gasType)
	{
		switch (gasType)
		{
		case GasType.Oxygen:
		case GasType.LiquidOxygen:
			return 21.1;
		case GasType.Nitrogen:
		case GasType.LiquidNitrogen:
			return 20.6;
		case GasType.CarbonDioxide:
		case GasType.LiquidCarbonDioxide:
			return 28.2;
		case GasType.Methane:
		case GasType.LiquidMethane:
			return 20.4;
		case GasType.Pollutant:
		case GasType.LiquidPollutant:
			return 24.8;
		case GasType.Water:
		case GasType.Steam:
			return 72.0;
		case GasType.PollutedWater:
			return 72.0;
		case GasType.NitrousOxide:
		case GasType.LiquidNitrousOxide:
			return 37.2;
		case GasType.Hydrogen:
		case GasType.LiquidHydrogen:
			return 20.4;
		case GasType.Hydrazine:
		case GasType.LiquidHydrazine:
			return 48.4;
		case GasType.LiquidAlcohol:
			return 33.0;
		case GasType.Helium:
			return 20.8;
		case GasType.LiquidSodiumChloride:
			return 130.0;
		case GasType.Silanol:
		case GasType.LiquidSilanol:
			return 101.0;
		case GasType.HydrochloricAcid:
		case GasType.LiquidHydrochloricAcid:
			return 37.0;
		case GasType.Ozone:
		case GasType.LiquidOzone:
			return 38.6;
		default:
			return 12.0;
		}
	}

	public static VolumeLitres MolarVolumeLiquid(GasType gasType)
	{
		if (MatterState(gasType) != AtmosphereHelper.MatterState.Liquid)
		{
			return VolumeLitres.Zero;
		}
		return new VolumeLitres(gasType switch
		{
			GasType.Water => 0.018, 
			GasType.PollutedWater => 0.018, 
			GasType.LiquidNitrogen => 0.0348, 
			GasType.LiquidOxygen => 0.03, 
			GasType.LiquidMethane => 0.04, 
			GasType.LiquidCarbonDioxide => 0.04, 
			GasType.LiquidPollutant => 0.04, 
			GasType.LiquidNitrousOxide => 0.026, 
			GasType.LiquidHydrogen => 0.028, 
			GasType.LiquidHydrazine => 0.03, 
			GasType.LiquidAlcohol => 0.058, 
			GasType.LiquidSodiumChloride => 0.04, 
			GasType.LiquidSilanol => 0.16, 
			GasType.LiquidHydrochloricAcid => 0.028, 
			GasType.LiquidOzone => 0.026, 
			_ => 0.0, 
		});
	}

	public static AtmosphereHelper.MatterState MatterState(GasType gasType)
	{
		switch (gasType)
		{
		case GasType.Water:
		case GasType.LiquidNitrogen:
		case GasType.LiquidOxygen:
		case GasType.LiquidMethane:
		case GasType.LiquidCarbonDioxide:
		case GasType.LiquidPollutant:
		case GasType.LiquidNitrousOxide:
		case GasType.LiquidHydrogen:
		case GasType.PollutedWater:
		case GasType.LiquidHydrazine:
		case GasType.LiquidAlcohol:
		case GasType.LiquidSodiumChloride:
		case GasType.LiquidSilanol:
		case GasType.LiquidHydrochloricAcid:
		case GasType.LiquidOzone:
			return AtmosphereHelper.MatterState.Liquid;
		default:
			return AtmosphereHelper.MatterState.Gas;
		}
	}

	public static bool IsMolecularlyZero(MoleQuantity required)
	{
		return required < MINIMUM_QUANTITY_MOLES;
	}
}
