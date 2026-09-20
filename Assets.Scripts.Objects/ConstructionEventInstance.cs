using UnityEngine;

namespace Assets.Scripts.Objects;

public class ConstructionEventInstance
{
	public Thing Parent;

	public Vector3 Position;

	public Quaternion Rotation;

	public ulong SteamId;

	public Slot OtherHandSlot;

	public ConstructionEventInstance()
	{
	}

	public ConstructionEventInstance(Thing thing)
	{
		Parent = thing;
		Position = thing.ThingTransformPosition + thing.ThingTransform.rotation * thing.Bounds.center;
		Rotation = thing.ThingTransform.rotation;
		SteamId = thing.OwnerClientId;
	}
}
