using System;
using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Util;

public struct StringReferenceInt : IEquatable<StringReferenceInt>, IComparable<StringReferenceInt>
{
	public Unit Unit;

	public int Value;

	private static string _invalidDegreesCelcius = "NaN";

	private static string _invalidDegreesKelvin = "NaN";

	private static EnumCollection<Unit, int> _unitTypes = new EnumCollection<Unit, int>();

	public static void Initialize()
	{
		_invalidDegreesCelcius = Localization.GetInterface("InvalidDegrees");
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, (Action)delegate
		{
			_invalidDegreesCelcius = Localization.GetInterface("InvalidDegrees");
		});
	}

	public string MakeString()
	{
		switch (Unit)
		{
		case Unit.None:
			return StringManager.Get(Value);
		case Unit.Stack:
			return "x" + StringManager.Get(Value);
		case Unit.ProgrammableChip:
			return StringManager.Get(Value) + " <size=75%> bytes</size>";
		case Unit.Degrees:
			return StringManager.Get(Value) + " <size=75%>°</size>";
		case Unit.Watts:
			return StringManager.Get(Value) + "<size=75%>W</size>";
		case Unit.kPa:
			if (Value == int.MaxValue)
			{
				return "∞Pa";
			}
			return StringManager.Get(Value) + "kPa";
		case Unit.Canister:
			if (Value == int.MaxValue)
			{
				return "∞<size=75%>Pa</size>";
			}
			return StringManager.Get(Value) + "<size=75%>kPa</size>";
		case Unit.CanisterLiquid:
		{
			if (Value == int.MaxValue)
			{
				return "∞<size=75%>L</size>";
			}
			float num = (float)Value / 1000f;
			if (num < 1f)
			{
				return StringManager.Get(Value) + "<size=75%>ml</size>";
			}
			return StringManager.Get(num) + "<size=75%>L</size>";
		}
		case Unit.g:
			return StringManager.Get(Value) + "g";
		case Unit.Percent:
			return StringManager.Get(Value) + "<size=75%>%</size>";
		case Unit.ThingNameSlot:
			return Localization.GetName(Value) ?? "";
		case Unit.Relay:
			return StringManager.Get(Value) + " " + ((Value == 1) ? "RELAY" : "RELAYS");
		case Unit.DegreesCelcius:
			if ((float)Value < (Chemistry.Temperature.Minimum - Chemistry.Temperature.ZeroDegrees).ToFloat())
			{
				return _invalidDegreesCelcius;
			}
			return Value.ToString();
		case Unit.DegreesKelvin:
			if (Value < 0)
			{
				return _invalidDegreesCelcius;
			}
			return $"{Value}K";
		case Unit.Credits:
			return "€" + StringManager.Get(Value);
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public StringReferenceInt(int value, Unit unit)
	{
		Value = value;
		Unit = unit;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is StringReferenceInt))
		{
			return false;
		}
		return Equals((StringReferenceInt)obj);
	}

	public bool Equals(StringReferenceInt other)
	{
		if (Value.Equals(other.Value))
		{
			return Unit == other.Unit;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int hashCode = Value.GetHashCode();
		int unit = (int)Unit;
		return hashCode ^ unit.GetHashCode();
	}

	public int CompareTo(StringReferenceInt other)
	{
		int unit = (int)Unit;
		int num = unit.CompareTo((int)other.Unit);
		if (num != 0)
		{
			return num;
		}
		return Value.CompareTo(other.Value);
	}

	public new string ToString()
	{
		return $"value: {Value} unit: {Unit}";
	}
}
