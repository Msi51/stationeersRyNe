using System;
using System.Xml.Serialization;
using UnityEngine;

[Serializable]
public class OrbitalViewData
{
	[XmlElement]
	public float SurfaceHeight;

	[XmlElement]
	public float AtmosphereDepth;

	[XmlElement]
	public float Curvature;

	[XmlElement]
	public float AtmosphereFalloff;

	[XmlElement]
	public float PlanetLift;

	[XmlElement]
	public Color AtmosphereColor = Color.clear;
}
