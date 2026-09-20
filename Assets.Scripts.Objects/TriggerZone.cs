using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Objects;

public class TriggerZone : MonoBehaviour
{
	public UnityEvent<Thing> OnThingEnter;

	public UnityEvent<Thing> OnThingExit;

	private void OnTriggerEnter(Collider other)
	{
		Thing thing = Thing.Find(other);
		if (thing != null)
		{
			OnThingEnter?.Invoke(thing);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Thing thing = Thing.Find(other);
		if (thing != null)
		{
			OnThingExit?.Invoke(thing);
		}
	}
}
