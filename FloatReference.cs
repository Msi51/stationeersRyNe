using System.Xml.Serialization;
using UnityEngine;

[XmlRoot]
public class FloatReference
{
	[XmlAttribute]
	public float Value;

	public FloatReference()
	{
	}

	public FloatReference(float value)
	{
		Value = value;
	}

	public static implicit operator float(FloatReference reference)
	{
		return reference.Value;
	}

	public bool Approximately(FloatReference defaultHungerRate)
	{
		return Mathf.Abs((float)defaultHungerRate - (float)this) <= 0.0001f;
	}

	public bool Approximately(float defaultHungerRate)
	{
		return Mathf.Abs(defaultHungerRate - (float)this) <= 0.0001f;
	}

	public bool IsZero()
	{
		return Mathf.Abs(Value) <= 0.0001f;
	}
}
