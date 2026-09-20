using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("CelestialBody")]
public class CelestialBodyTemplate : CelestialReference
{
	[XmlIgnore]
	private int _hash;

	[XmlElement("Orbit")]
	public OrbitData OrbitData = new OrbitData();

	public float LongitudeAtEpoch;

	[XmlElement]
	public ColorRGB Color = new ColorRGB();

	[XmlElement("SunriseOffset")]
	public FloatReference SunriseOffset;

	private static Dictionary<int, CelestialBodyTemplate> _hashLookup = new Dictionary<int, CelestialBodyTemplate>();

	private static List<CelestialBodyTemplate> _bodies = new List<CelestialBodyTemplate>();

	public static CelestialBodyTemplate Find(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		int key = Animator.StringToHash(id);
		_hashLookup.TryGetValue(key, out var value);
		if (value == null)
		{
			throw new NullReferenceException("celestial body " + id + " does not exist");
		}
		return value;
	}

	public void Register()
	{
		_hash = Animator.StringToHash(Id);
		if (_hashLookup.ContainsKey(_hash))
		{
			Debug.LogError("Celestial body with name " + Id + " already exists");
			return;
		}
		_hashLookup.Add(_hash, this);
		_bodies.Add(this);
	}
}
