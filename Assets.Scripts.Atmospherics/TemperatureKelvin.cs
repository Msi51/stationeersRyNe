using System;
using System.Runtime.CompilerServices;
using System.Text;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct TemperatureKelvin : IEquatable<TemperatureKelvin>
{
	private readonly double _value;

	public static readonly TemperatureKelvin Zero;

	public static readonly TemperatureKelvin One;

	public static readonly TemperatureKelvin MaxValue;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator +(TemperatureKelvin a, TemperatureKelvin b)
	{
		return new TemperatureKelvin(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator -(TemperatureKelvin a)
	{
		return new TemperatureKelvin(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator -(TemperatureKelvin a, TemperatureKelvin b)
	{
		return new TemperatureKelvin(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator *(TemperatureKelvin a, TemperatureKelvin b)
	{
		return new TemperatureKelvin(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator /(TemperatureKelvin a, TemperatureKelvin b)
	{
		return new TemperatureKelvin(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(TemperatureKelvin left, TemperatureKelvin right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(TemperatureKelvin left, TemperatureKelvin right)
	{
		return left._value < right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(TemperatureKelvin left, TemperatureKelvin right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(TemperatureKelvin left, TemperatureKelvin right)
	{
		return left._value >= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float ToFloat()
	{
		return (float)_value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public double ToDouble()
	{
		return _value;
	}

	public TemperatureKelvin(double value)
	{
		_value = value;
	}

	public TemperatureKelvin(MoleEnergy energy, HeatCapacity heatCapacity)
	{
		_value = energy.ToDouble() / heatCapacity.ToDouble();
	}

	public TemperatureKelvin(PressurekPa p, VolumeLitres V, MoleQuantity n)
	{
		_value = p.ToDouble() * V.ToDouble() / (n.ToDouble() * 8.3144);
	}

	public bool IsDenormal()
	{
		return _value.IsDenormal();
	}

	public bool IsDenormalOrNegative()
	{
		return _value.IsDenormalOrNegative();
	}

	public bool IsDenormalOrZero()
	{
		return _value.IsDenormalOrZero();
	}

	public bool IsNaN()
	{
		return double.IsNaN(_value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsDenormalToNegative()
	{
		return _value.IsDenormalToNegative();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(TemperatureKelvin other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object obj)
	{
		if (obj is TemperatureKelvin other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		double value = _value;
		return value.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator *(TemperatureKelvin a, double b)
	{
		return new TemperatureKelvin(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemperatureKelvin operator /(TemperatureKelvin a, double b)
	{
		return new TemperatureKelvin(a._value / b);
	}

	public double ToCelsius()
	{
		return _value - Chemistry.Temperature.ZeroDegrees.ToDouble();
	}

	public static TemperatureKelvin FromCelsius(float celsius)
	{
		return new TemperatureKelvin(273.15 + (double)celsius);
	}

	public string ToString(string color)
	{
		double value = _value;
		return value.ToString(color);
	}

	public string ToString(string unit, string color)
	{
		return _value.ToStringPrefix(unit, color);
	}

	public void Append(StringBuilder sb, bool includeCelsius = true)
	{
		sb.Append(_value.ToStringPrefix("K", "yellow"));
		if (includeCelsius)
		{
			sb.Append(' ');
			sb.Append(ToCelsius().ToStringPrefix("°C", "#585858"));
		}
	}

	public void AppendLine(StringBuilder sb, bool includeCelsius = true)
	{
		Append(sb, includeCelsius);
		sb.AppendLine();
	}

	public string AsStringUnit()
	{
		StringBuilder stringBuilder = new StringBuilder();
		Append(stringBuilder);
		return stringBuilder.ToString();
	}

	static TemperatureKelvin()
	{
		Zero = new TemperatureKelvin(0.0);
		One = new TemperatureKelvin(1.0);
		MaxValue = new TemperatureKelvin(double.MaxValue);
	}
}
