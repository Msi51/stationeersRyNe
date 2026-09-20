namespace Assets.Scripts.Networking;

public static class NetworkUpdateType
{
	public static class Simulation
	{
		public const ushort All = ushort.MaxValue;

		public const ushort None = 0;

		public const ushort TimeScale = 1;

		public const ushort DeltaTime = 2;
	}

	public static class Thing
	{
		public static class IndustrialBurner
		{
			public const ushort StressedToFailure = 1024;

			public const ushort Stress = 2048;

			public const ushort MachineTemperature = 4096;
		}

		public static class RoboticArm
		{
			public const ushort CurrentPosition = 1024;

			public const ushort TargetJunctionIndex = 2048;

			public const ushort ArmPosition = 4096;

			public const ushort ArmRotation = 8192;

			public const ushort ArmObstructed = 16384;

			public const ushort Dock = 32768;
		}

		public static class RocketMiner
		{
			public const ushort NextYield = 1024;

			public const ushort Quantity = 2048;
		}

		public static class Manufacturing
		{
			public const ushort MakingIndex = 16384;
		}

		public static class Speaker
		{
			public const ushort SoundAlert = 16384;
		}

		public static class CelestialTracker
		{
			public const ushort Index = 256;
		}

		public static class Entity
		{
			public const ushort State = 1024;

			public const ushort Cosmetics = 2048;

			public const ushort MedicalEffects = 4096;
		}

		public static class Item
		{
			public static class Egg
			{
				public const ushort Viable = 4096;

				public const ushort Fertilized = 8192;
			}

			public static class Plant
			{
				public const ushort GrowthStage = 4096;

				public const ushort HarvestQuantity = 8192;

				public const ushort FertilizerBoost = 16384;

				public const ushort GrowStatus = 32768;
			}

			public static class Motherboard
			{
				public static class RocketMotherboard
				{
					public const ushort CONNECTED_ROCKETS = 4096;

					public const ushort LOGIC_VALUES = 16384;

					public const ushort ROCKET_SELECTED = 32768;
				}
			}

			public const ushort Quantity = 1024;

			public const ushort Decay = 2048;
		}

		public static class InternalAtmosphere
		{
			public static class AdvancedSuit
			{
				public const ushort Volume = 8192;

				public const ushort SoundAlert = 16384;
			}

			public const ushort Leak = 4096;
		}

		public static class MusicMachines
		{
			public const ushort Bpm = 1024;

			public const ushort Attack = 2048;

			public const ushort Release = 4096;

			public const ushort AudioOutput = 8192;

			public const ushort Volume = 16384;

			public const ushort WaveForm = 32768;
		}

		public static class CircuitHousing
		{
			public const ushort DeviceId0 = 1024;

			public const ushort DeviceId1 = 2048;

			public const ushort DeviceId2 = 4096;

			public const ushort DeviceId3 = 8192;

			public const ushort DeviceId4 = 16384;

			public const ushort DeviceId5 = 32768;
		}

		public static class InputOutputCircuit
		{
			public static class AirConditioner
			{
				public const ushort OperationalEfficiency = 4096;

				public const ushort TemperatureDifferentialEfficiency = 8192;

				public const ushort OptimalPressureEfficiency = 16384;

				public const ushort EnergyMoved = 32768;
			}

			public static class CombustionCentrifuge
			{
				public const ushort Throttle = 4096;

				public const ushort CombustionLimiter = 8192;

				public const ushort Rpm = 16384;

				public const ushort Stress = 32768;
			}

			public static class RocketEngine
			{
				public const ushort Throttle = 4096;

				public const ushort Thrust = 8192;

				public const ushort ExhaustVelocity = 16384;

				public const ushort ExhaustTemperature = 32768;
			}

			public const ushort DeviceId0 = 1024;

			public const ushort DeviceId1 = 2048;
		}

		public static class LogicUnit
		{
			public static class Reader
			{
				public const ushort Method = 8192;

				public const ushort Index = 16384;
			}

			public static class Writer
			{
				public const ushort Input1 = 8192;

				public const ushort Input2 = 16384;

				public const ushort Input3 = 32768;
			}

			public static class Pid
			{
				public const ushort Proportional = 8192;

				public const ushort Integral = 16384;

				public const ushort Derivative = 32768;
			}

			public const ushort CurrentDevice = 1024;

			public const ushort LogicType = 2048;

			public const ushort Hash = 4096;
		}

		public static class RocketLink
		{
			public const ushort Connected = 1024;
		}

		public static class SatelliteDish
		{
			public const ushort InterrogatingContact = 1024;

			public const ushort SignalsUpdated = 2048;
		}

		public static class LandingPad
		{
			public const ushort ParentMotherboard = 1024;

			public const ushort CurrentTradingContact = 2048;

			public const ushort Waypoints = 4096;

			public const ushort VirtualWaypoint = 8192;

			public const ushort ShuttleInfo = 16384;

			public const ushort ShuttleLanded = 32768;
		}

		public class WirelessPower
		{
			public static ushort Receiver = 1024;
		}

		public const ushort All = ushort.MaxValue;

		public const ushort None = 0;

		public const ushort Transform = 1;

		public const ushort Interactable = 2;

		public const ushort Damage = 4;

		public const ushort ReagentMixture = 8;

		public const ushort Burning = 16;

		public const ushort CustomColour = 32;

		public const ushort BuildState = 64;

		public const ushort Thermal = 128;

		public const ushort Setting = 256;

		public const ushort Processing = 512;

		private const ushort GenericFlag0 = 1024;

		private const ushort GenericFlag1 = 2048;

		private const ushort GenericFlag2 = 4096;

		private const ushort GenericFlag3 = 8192;

		private const ushort GenericFlag4 = 16384;

		private const ushort GenericFlag5 = 32768;
	}

	public static class Rocket
	{
		public const ushort NONE = 0;

		public const ushort TARGET_NODE = 1;

		public const ushort CURRENT_TRANSIT = 4;

		public const ushort PROGRESS = 8;

		public const ushort ACCELERATION = 16;

		public const ushort CURRENT_NODE = 32;

		public const ushort DISTANCE_TO_TARGET = 64;

		public const ushort STATE = 128;

		public const ushort PARK_LOCATION = 256;

		public const ushort ANIMATION_POSITION = 512;

		public const ushort CUSTOM_NAME = 1024;

		public const ushort REMAINING_BURN_TIME = 2048;

		public const ushort AUTO_SHUT_OFF = 4096;

		public const ushort AUTOMATED_LANDING = 8192;

		public const ushort VELOCITY = 16384;

		public const ushort MODE = 32768;
	}

	public static class ScannedContactData
	{
		public const byte None = 0;

		public const byte ContactId = 1;

		public const byte LastScannedDegreeOffset = 2;

		public const byte CurrentTimeTillResolve = 4;

		public const byte StartTimeTillResolve = 8;

		public const byte LastResolvedRandomAngleOffset = 16;

		public const byte All = byte.MaxValue;
	}

	public static class Atmosphere
	{
		public const byte None = 0;

		public const byte ParentId = 1;

		public const byte Grid = 2;

		public const byte Temperature = 4;

		public const byte Direction = 8;

		public const byte Inflamed = 16;

		public const byte Volume = 32;

		public const byte Moles = 64;

		public const byte StateChange = 128;

		public const byte All = byte.MaxValue;
	}

	public static class PlantGrowStatus
	{
		public const ushort None = 0;

		public const ushort GrowthEfficiency = 1;

		public const ushort BreathingEfficiency = 2;

		public const ushort TemperatureEfficiency = 4;

		public const ushort LightEfficiency = 8;

		public const ushort PressureEfficiency = 16;

		public const ushort HydrationEfficiency = 32;

		public const ushort LightExposure = 64;

		public const ushort LightStress = 128;

		public const ushort Light = 256;

		public const ushort Darkness = 512;

		public const ushort StateArray = 1024;

		public const ushort All = 255;
	}

	public static class GasType
	{
		public const uint None = 0u;

		public const uint Oxygen = 1u;

		public const uint Nitrogen = 2u;

		public const uint CarbonDioxide = 4u;

		public const uint Methane = 8u;

		public const uint Pollutant = 16u;

		public const uint Water = 32u;

		public const uint PollutedWater = 65536u;

		public const uint NitrousOxide = 64u;

		public const uint LiquidNitrogen = 128u;

		public const uint LiquidOxygen = 256u;

		public const uint LiquidMethane = 512u;

		public const uint Steam = 1024u;

		public const uint LiquidCarbonDioxide = 2048u;

		public const uint LiquidPollutant = 4096u;

		public const uint LiquidNitrousOxide = 8192u;

		public const uint Hydrogen = 16384u;

		public const uint LiquidHydrogen = 32768u;

		public const uint Hydrazine = 131072u;

		public const uint LiquidHydrazine = 262144u;

		public const uint LiquidAlcohol = 524288u;

		public const uint Helium = 1048576u;

		public const uint LiquidSodiumChloride = 2097152u;

		public const uint Silanol = 4194304u;

		public const uint LiquidSilanol = 8388608u;

		public const uint HydrochloricAcid = 16777216u;

		public const uint LiquidHydrochloricAcid = 33554432u;

		public const uint Ozone = 67108864u;

		public const uint LiquidOzone = 134217728u;

		public const uint All = uint.MaxValue;
	}

	public static class SequencerInput
	{
		public const byte None = 0;

		public const byte Input0 = 1;

		public const byte Input1 = 2;

		public const byte Input2 = 4;

		public const byte Input3 = 8;

		public const byte Input4 = 16;

		public const byte Input5 = 32;

		public const byte Input6 = 64;

		public const byte Input7 = 128;

		public const byte All = byte.MaxValue;
	}

	public static class CircuitHousingProcessing
	{
		public const byte None = 0;

		public const byte DeviceLabel0 = 1;

		public const byte DeviceLabel1 = 2;

		public const byte DeviceLabel2 = 4;

		public const byte DeviceLabel3 = 8;

		public const byte DeviceLabel4 = 16;

		public const byte DeviceLabel5 = 32;

		public const byte IOLight = 64;

		public const byte AllLabels = 63;
	}

	public static class DynamicThingPosition
	{
		public const byte None = 0;

		public const byte WorldPosition = 1;

		public const byte WorldRotation = 2;

		public const byte AngularVelocity = 4;

		public const byte Velocity = 8;

		public const byte ChildPosition = 16;

		public const byte UseLocal = 32;

		public const byte ForceUpdate = 64;

		public const byte IsSleeping = 128;

		public const byte All = byte.MaxValue;
	}

	public static class TraderContact
	{
		public const byte None = 0;

		public const byte Contacted = 1;

		public const byte HumanTradingId = 2;

		public const byte InventoryDirty = 4;

		public const byte PadInfo = 8;

		public const byte InterrogatingDish = 16;

		public const byte NormalizedSecondsConnected = 32;
	}

	public static class SpaceMapNode
	{
		public const ushort NONE = 0;

		public const ushort SURVEY_POINTS = 1;

		public const ushort CHARTED = 4;

		public const ushort DEPOSIT = 8;

		public const ushort CHART_POINTS = 16;

		public const ushort DISCOVER_POINTS = 32;

		public const ushort ALL = ushort.MaxValue;
	}

	public const int NONE = 0;
}
