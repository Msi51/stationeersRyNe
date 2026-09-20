using CharacterCustomisation;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class StationContactData
{
	public Vector3 Angle;

	public string ContactName;

	public float WattsToResolve = 100f;

	public float MinimumWattsToResolve = 10f;

	public float MinimumWattsToContact = 180f;

	public float SecondsRequiredToContact = 60f;

	public bool Contacted;

	public float Lifetime;

	public long ReferenceId;

	public TraderInstanceSaveData InstanceSaveData;

	public int ContactSlotId;

	public ShuttleType ShuttleType;

	public SpeciesClass RequiredPadEnvironment;
}
