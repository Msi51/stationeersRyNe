using System;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Reagents;

[XmlRoot]
public struct Pressure : IEquatable<Pressure>
{
	public float Start;

	public float Stop;

	public bool IsValid
	{
		get
		{
			if (Mathf.Approximately(Start, Chemistry.Pressure.Minimum.ToFloat()))
			{
				return !Mathf.Approximately(Stop, Chemistry.Pressure.Maximum.ToFloat());
			}
			return true;
		}
	}

	public bool Equals(Pressure other)
	{
		if (Start <= other.Start)
		{
			return Stop >= other.Stop;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is Pressure other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return 1;
	}

	public static bool operator ==(Pressure left, Pressure right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Pressure left, Pressure right)
	{
		return !left.Equals(right);
	}

	public Pressure(PressurekPa start, PressurekPa stop)
	{
		Start = start.ToFloat();
		Stop = stop.ToFloat();
		Check();
	}

	public void Check()
	{
		if (Mathf.Approximately(Start, 0f) && Mathf.Approximately(Stop, 0f))
		{
			Start = Chemistry.Pressure.Minimum.ToFloat();
			Stop = Chemistry.Pressure.Maximum.ToFloat();
		}
		if (Start > Stop)
		{
			Stop = Chemistry.Temperature.Maximum.ToFloat();
		}
	}
}
