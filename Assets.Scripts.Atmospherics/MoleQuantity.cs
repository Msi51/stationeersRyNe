using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct MoleQuantity : IEquatable<MoleQuantity>
{
	private readonly double _value;

	public static readonly MoleQuantity Zero;

	public static readonly MoleQuantity One;

	public static readonly MoleQuantity MaxValue;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator +(MoleQuantity a, MoleQuantity b)
	{
		return new MoleQuantity(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator -(MoleQuantity a)
	{
		return new MoleQuantity(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator -(MoleQuantity a, MoleQuantity b)
	{
		return new MoleQuantity(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator *(MoleQuantity a, MoleQuantity b)
	{
		return new MoleQuantity(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator /(MoleQuantity a, MoleQuantity b)
	{
		return new MoleQuantity(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(MoleQuantity left, MoleQuantity right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(MoleQuantity left, MoleQuantity right)
	{
		return left._value < right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(MoleQuantity left, MoleQuantity right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(MoleQuantity left, MoleQuantity right)
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

	public MoleQuantity(double value)
	{
		_value = value;
	}

	public MoleQuantity(PressurekPa P, VolumeLitres V, TemperatureKelvin T)
	{
		_value = P.ToDouble() * V.ToDouble() / (T.ToDouble() * 8.3144);
	}

	public MoleQuantity(MoleEnergy energy, double latentHeatOfVaporization)
	{
		_value = energy.ToDouble() / latentHeatOfVaporization;
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
	public bool Equals(MoleQuantity other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object obj)
	{
		if (obj is MoleQuantity other)
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
	public static MoleQuantity operator *(MoleQuantity a, double b)
	{
		return new MoleQuantity(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleQuantity operator /(MoleQuantity a, double b)
	{
		return new MoleQuantity(a._value / b);
	}

	static MoleQuantity()
	{
		Zero = new MoleQuantity(0.0);
		One = new MoleQuantity(1.0);
		MaxValue = new MoleQuantity(double.MaxValue);
	}
}
