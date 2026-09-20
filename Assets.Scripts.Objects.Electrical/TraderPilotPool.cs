using System;
using Assets.Scripts.Util;
using CharacterCustomisation;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public sealed class TraderPilotPool : Singleton<TraderPilotPool>
{
	[Tooltip("Prefab references only")]
	[FormerlySerializedAs("_pilots")]
	[SerializeField]
	private TraderPilot[] _pilotsPrefabs;

	private void Awake()
	{
		TraderPilot[] pilotsPrefabs = _pilotsPrefabs;
		for (int i = 0; i < pilotsPrefabs.Length; i++)
		{
			pilotsPrefabs[i].CacheRenderers();
		}
	}

	public TraderPilot ShowPilot(TraderContact contact, Transform parentTransform, Vector3 positionalOffset)
	{
		if (!parentTransform)
		{
			return null;
		}
		SpeciesClass speciesClass = contact?.RequiredPadEnvironment ?? SpeciesClass.None;
		if (speciesClass == SpeciesClass.None)
		{
			speciesClass = SpeciesClass.Robot;
		}
		TraderPilot[] pilotsPrefabs = _pilotsPrefabs;
		foreach (TraderPilot traderPilot in pilotsPrefabs)
		{
			if (traderPilot.PilotType == speciesClass)
			{
				TraderPilot traderPilot2 = UnityEngine.Object.Instantiate(traderPilot, parentTransform);
				Vector3 position = (traderPilot2.CachedTransformPosition = parentTransform.position + positionalOffset);
				traderPilot2.transform.SetPositionAndRotation(position, parentTransform.rotation);
				return traderPilot2;
			}
		}
		throw new Exception(string.Format("{0} '{1}' unavailable", "SpeciesClass", speciesClass));
	}
}
