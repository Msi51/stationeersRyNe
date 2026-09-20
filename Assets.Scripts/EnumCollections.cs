using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using CharacterCustomisation;
using Networks;
using Objects.Electrical;
using Objects.Items;
using Objects.Rockets;
using TerrainSystem;
using UnityEngine;
using Util.Commands;

namespace Assets.Scripts;

public static class EnumCollections
{
	public static readonly EnumCollection<ExteriorState, byte> ExteriorStates = new EnumCollection<ExteriorState, byte>(toProper: false);

	public static readonly EnumCollection<RocketAvionicsInstruction, byte> RocketAvionicsInstructions = new EnumCollection<RocketAvionicsInstruction, byte>(toProper: false);

	public static readonly EnumCollection<FiltrationMode, byte> FiltrationModes = new EnumCollection<FiltrationMode, byte>(toProper: false);

	public static readonly EnumCollection<SpeciesClass, byte> Species = new EnumCollection<SpeciesClass, byte>(toProper: false);

	public static readonly EnumCollection<DeviceMode, byte> DeviceModes = new EnumCollection<DeviceMode, byte>(toProper: false);

	public static readonly EnumCollection<PowerMode, int> PowerModes = new EnumCollection<PowerMode, int>(toProper: false);

	public static readonly EnumCollection<SimulationSpan, int> SimulationSpans = new EnumCollection<SimulationSpan, int>(toProper: false);

	public static readonly EnumCollection<NodeType, int> NodeTypes = new EnumCollection<NodeType, int>(toProper: false);

	public static readonly EnumCollection<NetworkRole, int> NetworkRoles = new EnumCollection<NetworkRole, int>(toProper: false);

	public static readonly EnumCollection<ScriptCommand, int> ScriptCommands = new EnumCollection<ScriptCommand, int>(toProper: false);

	public static readonly EnumCollection<LogicType, ushort> LogicTypes = new EnumCollection<LogicType, ushort>(toProper: false);

	public static readonly EnumCollection<LogicBatchMethod, int> LogicBatchMethods = new EnumCollection<LogicBatchMethod, int>(toProper: false);

	public static readonly EnumCollection<LogicSlotType, byte> LogicSlotTypes = new EnumCollection<LogicSlotType, byte>(toProper: false);

	public static readonly EnumCollection<HashType, byte> HashTypes = new EnumCollection<HashType, byte>(toProper: false);

	public static readonly EnumCollection<Chemistry.GasType, uint> GasTypes = new EnumCollection<Chemistry.GasType, uint>(toProper: false);

	public static readonly EnumCollection<Chemistry.GasType, uint> GasTypesProper = new EnumCollection<Chemistry.GasType, uint>();

	public static readonly EnumCollection<Pipe.ContentType, int> PipeContentTypes = new EnumCollection<Pipe.ContentType, int>(toProper: false);

	public static readonly EnumCollection<ColorType, byte> ColorTypes = new EnumCollection<ColorType, byte>(toProper: false);

	public static readonly EnumCollection<SortingClass, ushort> SortingClasses = new EnumCollection<SortingClass, ushort>(toProper: false);

	public static readonly EnumCollection<LogType, int> LogTypes = new EnumCollection<LogType, int>(toProper: false);

	public static readonly EnumCollection<SoundAlert, int> SpeakerSounds = new EnumCollection<SoundAlert, int>(toProper: false);

	public static readonly EnumCollection<BlendShapeType, int> BlendShapeTypes = new EnumCollection<BlendShapeType, int>(toProper: false);

	public static readonly EnumCollection<InteractableType, int> InteractableTypes = new EnumCollection<InteractableType, int>(toProper: false);

	public static readonly EnumCollection<Slot.Class, ushort> SlotClasses = new EnumCollection<Slot.Class, ushort>(toProper: false);

	public static readonly EnumCollection<EntityState, byte> EntityStates = new EnumCollection<EntityState, byte>(toProper: false);

	public static readonly EnumCollection<BatteryCellState, int> BatteryCellStates = new EnumCollection<BatteryCellState, int>();

	public static readonly EnumCollection<IngotType, byte> IngotTypes = new EnumCollection<IngotType, byte>();

	public static readonly EnumCollection<FlowIndicatorState, int> FlowIndicatorStates = new EnumCollection<FlowIndicatorState, int>();

	public static readonly EnumCollection<ConnectionRole, int> ConnectionRole = new EnumCollection<ConnectionRole, int>();

	public static readonly EnumCollection<NetworkType, int> NetworkType = new EnumCollection<NetworkType, int>();

	public static readonly EnumCollection<RocketMode, byte> RocketMode = new EnumCollection<RocketMode, byte>();

	public static readonly EnumCollection<ReEntryProfile, int> ReEntryProfile = new EnumCollection<ReEntryProfile, int>();

	public static readonly EnumCollection<Gene, int> Genes = new EnumCollection<Gene, int>();

	public static readonly EnumCollection<SorterInstruction, byte> SorterInstructions = new EnumCollection<SorterInstruction, byte>(toProper: false);

	public static readonly EnumCollection<LogicOperator, byte> LogicOperators = new EnumCollection<LogicOperator, byte>(toProper: false);

	public static readonly EnumCollection<CelestialTracking, byte> CelestialTracking = new EnumCollection<CelestialTracking, byte>(toProper: false);

	public static readonly EnumCollection<OccupancyInstruction, byte> InventoryInstructions = new EnumCollection<OccupancyInstruction, byte>(toProper: false);

	public static readonly EnumCollection<SiloInstruction, byte> SiloInstructions = new EnumCollection<SiloInstruction, byte>(toProper: false);

	public static readonly EnumCollection<SuitStorageInstruction, byte> SuitStorageInstructions = new EnumCollection<SuitStorageInstruction, byte>(toProper: false);

	public static readonly EnumCollection<PrinterInstruction, byte> PrinterInstructions = new EnumCollection<PrinterInstruction, byte>(toProper: false);

	public static readonly EnumCollection<StructureNetworkType, byte> StructureNetworkType = new EnumCollection<StructureNetworkType, byte>(toProper: false);

	public static readonly EnumCollection<MachineTier, int> MachineTier = new EnumCollection<MachineTier, int>();

	public static readonly EnumCollection<EntitySurvivalProperty, int> EntitySurvivalProperty = new EnumCollection<EntitySurvivalProperty, int>();

	public static readonly EnumCollection<CompareOperator, int> CompareOperator = new EnumCollection<CompareOperator, int>();

	public static readonly EnumCollection<AtmosphereHelper.AtmosphereMode, byte> AtmosphereMode = new EnumCollection<AtmosphereHelper.AtmosphereMode, byte>();

	public static readonly EnumCollection<VentDirection, int> VentDirection = new EnumCollection<VentDirection, int>();

	public static readonly EnumCollection<WeightedMode, int> WeightedModes = new EnumCollection<WeightedMode, int>();

	public static readonly EnumCollection<TraderInstruction, byte> TraderInstructions = new EnumCollection<TraderInstruction, byte>(toProper: false);

	public static readonly EnumCollection<Achievements.Kind, byte> Achievements = new EnumCollection<Achievements.Kind, byte>();

	public static readonly EnumCollection<Achievements.Stat, byte> AchievementStats = new EnumCollection<Achievements.Stat, byte>(toProper: false);

	public static readonly EnumCollection<MinableType, byte> MinableTypes = new EnumCollection<MinableType, byte>(toProper: false);

	public static readonly EnumCollection<DiggingMode, byte> DiggingModes = new EnumCollection<DiggingMode, byte>(toProper: false);

	public static readonly EnumCollection<PositionMode, byte> PositionModes = new EnumCollection<PositionMode, byte>(toProper: false);

	public static readonly EnumCollection<SizeMode, byte> SizeModes = new EnumCollection<SizeMode, byte>(toProper: false);

	public static readonly EnumCollection<KeyCode, int> KeyCodes = new EnumCollection<KeyCode, int>(toProper: false);

	public static readonly EnumCollection<VoxelNodeType, byte> VoxelNodeTypes = new EnumCollection<VoxelNodeType, byte>(toProper: false);
}
