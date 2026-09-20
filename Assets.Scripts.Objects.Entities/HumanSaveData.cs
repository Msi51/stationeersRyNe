using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;
using CharacterCustomisation;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

[Obsolete("I think could used PlayerCosmetics.cs serialised data class instead")]
[XmlInclude(typeof(DynamicThingSaveData))]
public class HumanSaveData : EntitySaveData
{
	public float Currency;

	public PlayerCosmetics Cosmetics;

	public int SkinIndex;

	public int GenderIndex;

	public int PotatoDays;

	public Vector2 LastValidPlayablePosition;

	public StringReference StartLocation;

	public List<MedicalEffectBase> MedicalEffects = new List<MedicalEffectBase>(5);
}
