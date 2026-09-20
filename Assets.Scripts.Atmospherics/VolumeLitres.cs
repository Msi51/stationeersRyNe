using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct VolumeLitres : IEquatable<VolumeLitres>
{
	private readonly double _value;

	public static readonly VolumeLitres Zero;

	public static readonly VolumeLitres One;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator +(VolumeLitres a, VolumeLitres b)
	{
		return new VolumeLitres(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator -(VolumeLitres a)
	{
		return new VolumeLitres(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator -(VolumeLitres a, VolumeLitres b)
	{
		return new VolumeLitres(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator *(VolumeLitres a, VolumeLitres b)
	{
		return new VolumeLitres(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator /(VolumeLitres a, VolumeLitres b)
	{
		return new VolumeLitres(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(VolumeLitres left, VolumeLitres right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(VolumeLitres left, VolumeLitres right)
	{
		return left._value < right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(VolumeLitres left, VolumeLitres right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(VolumeLitres left, VolumeLitres right)
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

	public VolumeLitres(double value)
	{
		_value = value;
	}

	public VolumeLitres(MoleQuantity n, TemperatureKelvin T, PressurekPa p)
	{
		_value = n.ToDouble() * 8.3144 * T.ToDouble() / p.ToDouble();
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
	public bool Equals(VolumeLitres other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object obj)
	{
		if (obj is VolumeLitres other)
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
	public static VolumeLitres operator *(VolumeLitres a, double b)
	{
		return new VolumeLitres(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static VolumeLitres operator /(VolumeLitres a, double b)
	{
		return new VolumeLitres(a._value / b);
	}

	static VolumeLitres()
	{
		Zero = new VolumeLitres(0.0);
		One = new VolumeLitres(1.0);
	}
}
