using System;
using System.Globalization;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Networks;
using Objects.Electrical;
using Objects.Rockets;
using Objects.Rockets.Mining;

namespace Assets.Scripts.Localization2;

public static class LocalizedEnumCollections
{
	private interface ILocalizedEnumCollection
	{
		void OnLanguageChanged();
	}

	public abstract class LocalizedEnumCollection<TEnum, TString> : ILocalizedEnumCollection where TEnum : Enum, IConvertible, new()
	{
		protected readonly string[] _names;

		protected readonly TEnum[] _values;

		protected readonly int[] _valuesAsInts;

		protected readonly TString[] _displayStrings;

		public int Length => _values.Length;

		public string EnumTypeName { get; private set; }

		public string[] Names => _names;

		public TString this[int index] => _displayStrings[index];

		protected LocalizedEnumCollection(bool toProper = true)
		{
			Type typeFromHandle = typeof(TEnum);
			Array enumValues = typeFromHandle.GetEnumValues();
			EnumTypeName = typeFromHandle.Name;
			_names = typeFromHandle.GetEnumNames();
			_values = (TEnum[])enumValues;
			_valuesAsInts = new int[enumValues.Length];
			for (int i = 0; i < enumValues.Length; i++)
			{
				int[] valuesAsInts = _valuesAsInts;
				int num = i;
				ref readonly TEnum reference = ref _values[i];
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				valuesAsInts[num] = reference.ToInt32(invariantCulture);
			}
			_displayStrings = new TString[enumValues.Length];
			for (int j = 0; j < enumValues.Length; j++)
			{
				_displayStrings[j] = CreateDisplayString(j, toProper);
			}
		}

		protected string ProperizedName(int index, bool toProper)
		{
			if (!toProper)
			{
				return _names[index];
			}
			return _names[index].ToProper();
		}

		protected abstract TString CreateDisplayString(int index, bool toProper);

		public abstract void OnLanguageChanged();

		public TString GetDisplayName(TEnum value)
		{
			return GetNameFromIntValue(value.ToInt32(CultureInfo.InvariantCulture));
		}

		public TString GetNameFromIntValue(int value)
		{
			for (int i = 0; i < _values.Length; i++)
			{
				if (_valuesAsInts[i] == value)
				{
					return _displayStrings[i];
				}
			}
			return default(TString);
		}
	}

	public class FixedStringCollection<TEnum> : LocalizedEnumCollection<TEnum, string> where TEnum : Enum, IConvertible, new()
	{
		protected override string CreateDisplayString(int index, bool toProper)
		{
			return ProperizedName(index, toProper);
		}

		public FixedStringCollection(bool toProper = true)
			: base(toProper)
		{
		}

		public override void OnLanguageChanged()
		{
		}
	}

	public class GameStringCollection<TEnum> : LocalizedEnumCollection<TEnum, GameString> where TEnum : Enum, IConvertible, new()
	{
		protected override GameString CreateDisplayString(int index, bool toProper)
		{
			return GameString.GetOrCreate(base.EnumTypeName + _names[index], ProperizedName(index, toProper));
		}

		public GameStringCollection(bool toProper = true)
			: base(toProper)
		{
		}

		public override void OnLanguageChanged()
		{
		}
	}

	public static readonly GameStringCollection<MineableDepositType> MineableDepositTypes = new GameStringCollection<MineableDepositType>(toProper: false);

	public static readonly GameStringCollection<InteractableType> InteractableType = new GameStringCollection<InteractableType>(toProper: false);

	public static readonly GameStringCollection<StructureNetworkType> StructureNetworkType = new GameStringCollection<StructureNetworkType>(toProper: false);

	public static readonly GameStringCollection<SpeciesClass> SpeciesClass = new GameStringCollection<SpeciesClass>(toProper: false);

	public static readonly GameStringCollection<MachineTier> MachineTier = new GameStringCollection<MachineTier>(toProper: false);

	public static readonly FixedStringCollection<LogicType> LogicType = new FixedStringCollection<LogicType>(toProper: false);

	public static readonly GameStringCollection<RoomType> RoomType = new GameStringCollection<RoomType>(toProper: false);

	public static readonly GameStringCollection<VentDirection> VentDirection = new GameStringCollection<VentDirection>(toProper: false);

	public static readonly GameStringCollection<FlowIndicatorState> FlowIndicatorState = new GameStringCollection<FlowIndicatorState>(toProper: false);

	public static readonly GameStringCollection<AdvancedAirlockState> AdvancedAirlockState = new GameStringCollection<AdvancedAirlockState>(toProper: false);

	public static readonly FixedStringCollection<AirlockControlState> AirlockControlState = new FixedStringCollection<AirlockControlState>(toProper: false);

	public static readonly GameStringCollection<IngotType> IngotType = new GameStringCollection<IngotType>(toProper: false);

	public static readonly GameStringCollection<ConnectionRole> ConnectionRole = new GameStringCollection<ConnectionRole>(toProper: false);

	public static readonly GameStringCollection<NetworkType> NetworkType = new GameStringCollection<NetworkType>(toProper: false);

	public static readonly GameStringCollection<DaylightSensor.DaylightSensorMode> DaylightSensorMode = new GameStringCollection<DaylightSensor.DaylightSensorMode>(toProper: false);

	public static readonly GameStringCollection<BatteryCellState> BatteryCellState = new GameStringCollection<BatteryCellState>(toProper: false);

	public static readonly GameStringCollection<ReEntryProfile> ReEntryProfile = new GameStringCollection<ReEntryProfile>(toProper: false);

	private static readonly ILocalizedEnumCollection[] AllCollections = new ILocalizedEnumCollection[16]
	{
		InteractableType, StructureNetworkType, SpeciesClass, MachineTier, LogicType, RoomType, VentDirection, FlowIndicatorState, AdvancedAirlockState, AirlockControlState,
		IngotType, ConnectionRole, NetworkType, DaylightSensorMode, BatteryCellState, ReEntryProfile
	};

	public static string GetName(this InteractableType value)
	{
		return InteractableType.GetDisplayName(value);
	}

	public static string GetName(this StructureNetworkType value)
	{
		return StructureNetworkType.GetDisplayName(value);
	}

	public static string GetName(this SpeciesClass value)
	{
		return SpeciesClass.GetDisplayName(value);
	}

	public static string GetName(this MachineTier value)
	{
		return MachineTier.GetDisplayName(value);
	}

	public static string GetName(this LogicType value)
	{
		return LogicType.GetDisplayName(value);
	}

	public static string GetName(this RoomType value)
	{
		return RoomType.GetDisplayName(value);
	}

	public static string GetName(this VentDirection value)
	{
		return VentDirection.GetDisplayName(value);
	}

	public static string GetName(this FlowIndicatorState value)
	{
		return FlowIndicatorState.GetDisplayName(value);
	}

	public static string GetName(this AdvancedAirlockState value)
	{
		return AdvancedAirlockState.GetDisplayName(value);
	}

	public static string GetName(this AirlockControlState value)
	{
		return AirlockControlState.GetDisplayName(value);
	}

	public static string GetName(this IngotType value)
	{
		return IngotType.GetDisplayName(value);
	}

	public static string GetName(this ConnectionRole value)
	{
		return ConnectionRole.GetDisplayName(value);
	}

	public static string GetName(this NetworkType value)
	{
		return NetworkType.GetDisplayName(value);
	}

	public static string GetName(this DaylightSensor.DaylightSensorMode value)
	{
		return DaylightSensorMode.GetDisplayName(value);
	}

	public static string GetName(this BatteryCellState value)
	{
		return BatteryCellState.GetDisplayName(value);
	}

	public static string GetName(this ReEntryProfile value)
	{
		return ReEntryProfile.GetDisplayName(value);
	}

	public static void CreateIfNeeded()
	{
	}

	public static void OnLanguageChanged()
	{
		ILocalizedEnumCollection[] allCollections = AllCollections;
		for (int i = 0; i < allCollections.Length; i++)
		{
			allCollections[i].OnLanguageChanged();
		}
	}
}
