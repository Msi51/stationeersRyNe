using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterCustomisation;

[Serializable]
public struct KitSets
{
	[FormerlySerializedAs("species")]
	public SpeciesClass SpeciesClass;

	[Tooltip("Can be used to create many different variations of a species. e.g gender or whatever floats your boat")]
	public KitGender[] Genders;

	public bool IsValid()
	{
		if (SpeciesClass != SpeciesClass.None && Genders != null)
		{
			return Genders.Length != 0;
		}
		return false;
	}
}
