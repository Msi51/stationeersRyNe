using System.Runtime.CompilerServices;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public readonly struct SpecificHeat(double value)
{
	private readonly double _value = value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator +(SpecificHeat a, SpecificHeat b)
	{
		return new SpecificHeat(a._value + b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator -(SpecificHeat a)
	{
		return new SpecificHeat(0.0 - a._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator -(SpecificHeat a, SpecificHeat b)
	{
		return new SpecificHeat(a._value - b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator *(SpecificHeat a, SpecificHeat b)
	{
		return new SpecificHeat(a._value * b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator /(SpecificHeat a, SpecificHeat b)
	{
		return new SpecificHeat(a._value / b._value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(SpecificHeat left, SpecificHeat right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(SpecificHeat left, SpecificHeat right)
	{
		return left._value > right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(SpecificHeat left, SpecificHeat right)
	{
		return left._value <= right._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(SpecificHeat left, SpecificHeat right)
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
	public static SpecificHeat operator *(SpecificHeat a, double b)
	{
		return new SpecificHeat(a._value * b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpecificHeat operator /(SpecificHeat a, double b)
	{
		return new SpecificHeat(a._value / b);
	}
}
