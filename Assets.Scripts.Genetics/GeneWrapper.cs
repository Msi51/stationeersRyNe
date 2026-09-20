using System;
using System.Xml.Serialization;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Genetics;

[Serializable]
[XmlRoot("Gene")]
public class GeneWrapper : IRocketReaderWriter
{
	public Gene Gene;

	public float Value;

	public float Stability;

	private sbyte StabilitySbyte
	{
		get
		{
			return (sbyte)(Stability * 127f);
		}
		set
		{
			Stability = (float)Math.Round((double)value / 127.0, 2);
		}
	}

	public GeneWrapper()
	{
	}

	public GeneWrapper(GeneWrapper other)
	{
		Gene = other.Gene;
		Value = other.Value;
		Stability = other.Stability;
	}

	public GeneWrapper(Gene gene, float value, float stability)
	{
		Gene = gene;
		Value = value;
		Stability = stability;
	}

	public void Stabilise(float amount)
	{
		Stability = Mathf.Clamp(Stability + amount, -1f, 1f);
	}

	public void Destabilise(float amount)
	{
		Stability = Mathf.Clamp(Stability - amount, -1f, 1f);
	}

	public void Read(RocketBinaryReader reader)
	{
		Gene = (Gene)reader.ReadByte();
		Value = reader.ReadSingle();
		StabilitySbyte = reader.ReadSByte();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)Gene);
		writer.WriteSingle(Value);
		writer.WriteSByte(StabilitySbyte);
	}
}
