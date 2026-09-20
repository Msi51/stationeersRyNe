using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct HeatCapacity : IEquatable<HeatCapacity>
{
	private readonly double _value;

	public static readonly HeatCapacity Zero;

	public float ToFloat()
	{
		return (float)_value;
	}

	public double ToDouble()
	{
		return _value;
	}

	public HeatCapacity(double value)
	{
		_value = value;
	}

	public HeatCapacity(SpecificHeat specificHeat, MoleQuantity moleQuantity)
	{
		_value = specificHeat.ToDouble() * moleQuantity.ToDouble();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static HeatCapacity operator +(HeatCapacity a, HeatCapacity b)
	{
		return new HeatCapacity(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static HeatCapacity operator -(HeatCapacity a)
	{
		return new HeatCapacity(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static HeatCapacity operator -(HeatCapacity a, HeatCapacity b)
	{
		return new HeatCapacity(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static HeatCapacity operator *(HeatCapacity a, HeatCapacity b)
	{
		return new HeatCapacity(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static HeatCapacity operator /(HeatCapacity a, HeatCapacity b)
	{
		return new HeatCapacity(a._value / b._value);
	}

	public bool IsDenormalToNegative()
	{
		return _value.IsDenormalToNegative();
	}

	public bool IsNaN()
	{
		return double.IsNaN(_value);
	}

	public bool Equals(HeatCapacity other)
	{
		double value = _value;
		return value.Equals(other._value);
	}

	public override bool Equals(object obj)
	{
		if (obj is HeatCapacity other)
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

	static HeatCapacity()
	{
		Zero = new HeatCapacity(0.0);
	}
}
