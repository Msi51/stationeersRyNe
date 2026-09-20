using System;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Reagents;

[XmlRoot]
public struct Temperature : IEquatable<Temperature>
{
	public float Start;

	public float Stop;

	public bool IsValid
	{
		get
		{
			if (Mathf.Approximately(Start, Chemistry.Temperature.Minimum.ToFloat()))
			{
				return !Mathf.Approximately(Stop, Chemistry.Temperature.Maximum.ToFloat());
			}
			return true;
		}
	}

	public bool Equals(Temperature other)
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
		if (obj is Temperature)
		{
			return Equals((Temperature)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return 1;
	}

	public static bool operator ==(Temperature left, Temperature right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Temperature left, Temperature right)
	{
		return !left.Equals(right);
	}

	public Temperature(TemperatureKelvin start, TemperatureKelvin stop)
	{
		Start = start.ToFloat();
		Stop = stop.ToFloat();
		Check();
	}

	public void Check()
	{
		if (Mathf.Approximately(Start, 0f) && Mathf.Approximately(Stop, 0f))
		{
			Start = Chemistry.Temperature.Minimum.ToFloat();
			Stop = Chemistry.Temperature.Maximum.ToFloat();
		}
		else if (Start < Chemistry.Temperature.Minimum.ToFloat())
		{
			Start = Chemistry.Temperature.Minimum.ToFloat();
		}
		if (Start > Stop)
		{
			Stop = Chemistry.Temperature.Maximum.ToFloat();
		}
	}
}
