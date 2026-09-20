using CharacterCustomisation;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public abstract class TraderPilot : MonoBehaviour
{
	[SerializeField]
	private SpeciesClass pilotType;

	public bool IsBeingDestroyed;

	public Vector3 CachedTransformPosition;

	public NonThingOcclusionHandler OcclusionHandler;

	public SpeciesClass PilotType => pilotType;

	public void CacheRenderers()
	{
		OcclusionHandler.CacheRenderers();
	}
}
