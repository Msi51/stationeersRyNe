using UnityEngine;

namespace Assets.Scripts.Objects;

public class TriggerPoint : Thing
{
	public delegate void OnThingEnterTrigger(Thing thing);

	public delegate void OnThingExitTrigger(Thing thing);

	public event OnThingEnterTrigger OnThingEnter;

	public event OnThingExitTrigger OnThingExit;

	private void OnTriggerEnter(Collider col)
	{
		DynamicThing component = col.GetComponent<DynamicThing>();
		if (!(component == null))
		{
			this.OnThingEnter?.Invoke(component);
		}
	}

	private void OnTriggerExit(Collider col)
	{
		DynamicThing component = col.GetComponent<DynamicThing>();
		if (!(component == null))
		{
			this.OnThingExit?.Invoke(component);
		}
	}
}
