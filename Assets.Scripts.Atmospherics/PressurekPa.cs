using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct PressurekPa : IEquatable<PressurekPa>
{
	private readonly double _value;

	public static readonly PressurekPa Zero;

	public static readonly PressurekPa One;

	public static readonly PressurekPa MaxValue;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator +(PressurekPa a, PressurekPa b)
	{
		return new PressurekPa(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator -(PressurekPa a)
	{
		return new PressurekPa(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator -(PressurekPa a, PressurekPa b)
	{
		return new PressurekPa(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator *(PressurekPa a, PressurekPa b)
	{
		return new PressurekPa(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator *(PressurekPa a, double b)
	{
		return new PressurekPa(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator /(PressurekPa a, PressurekPa b)
	{
		return new PressurekPa(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PressurekPa operator /(PressurekPa a, double b)
	{
		return new PressurekPa(a._value / b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(PressurekPa left, PressurekPa right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(PressurekPa left, PressurekPa right)
	{
		return left._value < right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(PressurekPa left, PressurekPa right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(PressurekPa left, PressurekPa right)
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

	public PressurekPa(double value)
	{
		_value = value;
	}

	public PressurekPa(MoleQuantity n, TemperatureKelvin T, VolumeLitres V)
	{
		_value = n.ToDouble() * 8.3144 * T.ToDouble() / V.ToDouble();
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
	public bool Equals(PressurekPa other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object obj)
	{
		if (obj is PressurekPa other)
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

	static PressurekPa()
	{
		Zero = new PressurekPa(0.0);
		One = new PressurekPa(1.0);
		MaxValue = new PressurekPa(double.MaxValue);
	}
}
