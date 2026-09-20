using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects;

public abstract class DevicePart : MonoBehaviour
{
	[SerializeField]
	protected Thing parentThing;

	[SerializeField]
	protected Transform Transform;

	public void Awake()
	{
		if (parentThing == null)
		{
			parentThing = GetComponentInParent<Thing>();
		}
	}

	public void SetParentThing(Thing parent)
	{
		parentThing = parent;
	}

	public abstract void RefreshState(bool skipAnim = false);
}
