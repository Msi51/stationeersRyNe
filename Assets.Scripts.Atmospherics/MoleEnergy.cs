using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct MoleEnergy : IEquatable<MoleEnergy>
{
	private readonly double _value;

	public static readonly MoleEnergy Zero;

	public static readonly MoleEnergy MaxValue;

	public float ToFloat()
	{
		return (float)_value;
	}

	public double ToDouble()
	{
		return _value;
	}

	public MoleEnergy(double value)
	{
		_value = value;
	}

	public MoleEnergy(HeatCapacity heatCapacity, TemperatureKelvin T)
	{
		_value = heatCapacity.ToDouble() * T.ToDouble();
	}

	public MoleEnergy(TemperatureKelvin T, SpecificHeat specificHeat, MoleQuantity n)
	{
		_value = T.ToDouble() * specificHeat.ToDouble() * n.ToDouble();
	}

	public MoleEnergy(MoleQuantity quantity, double latentHeatOfVaporization)
	{
		_value = quantity.ToDouble() * latentHeatOfVaporization;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator +(MoleEnergy a, MoleEnergy b)
	{
		return new MoleEnergy(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator -(MoleEnergy a)
	{
		return new MoleEnergy(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator -(MoleEnergy a, MoleEnergy b)
	{
		return new MoleEnergy(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator *(MoleEnergy a, MoleEnergy b)
	{
		return new MoleEnergy(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator /(MoleEnergy a, MoleEnergy b)
	{
		return new MoleEnergy(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(MoleEnergy left, MoleEnergy right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(MoleEnergy left, MoleEnergy right)
	{
		return left._value < right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(MoleEnergy left, MoleEnergy right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(MoleEnergy left, MoleEnergy right)
	{
		return left._value >= right._value;
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

	public bool IsInfinity()
	{
		return double.IsInfinity(_value);
	}

	public bool Equals(MoleEnergy other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	public override bool Equals(object obj)
	{
		if (obj is MoleEnergy other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		double value = _value;
		return value.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator *(MoleEnergy a, double b)
	{
		return new MoleEnergy(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MoleEnergy operator /(MoleEnergy a, double b)
	{
		return new MoleEnergy(a._value / b);
	}

	static MoleEnergy()
	{
		Zero = new MoleEnergy(0.0);
		MaxValue = new MoleEnergy(double.MaxValue);
	}
}
