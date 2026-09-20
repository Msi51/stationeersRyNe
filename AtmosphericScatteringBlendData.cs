using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using UnityEngine;

public class AtmosphericScatteringBlendData : DataCollection
{
	[XmlElement("Gas")]
	public List<Chemistry.GasType> GasTypes = new List<Chemistry.GasType>();

	[XmlArray("RayleighColorRampColorKey")]
	public List<GradientColorKey> RayleighColorRampColorKey = new List<GradientColorKey>();

	[XmlArray("MieColorRampColorKey")]
	public List<GradientColorKey> MieColorRampColorKey = new List<GradientColorKey>();

	[XmlElement]
	public Color HeightRayleighColor = Color.white;

	public static List<AtmosphericScatteringBlendData> AllBlends = new List<AtmosphericScatteringBlendData>();

	public override void Initialize(ModAbout mod)
	{
		AllBlends.Add(this);
	}

	public float GetWeight(Atmosphere globalAtmosphere)
	{
		float num = 0f;
		foreach (Chemistry.GasType gasType in GasTypes)
		{
			num += globalAtmosphere.GetGasTypeRatio(gasType);
		}
		return num;
	}
}
