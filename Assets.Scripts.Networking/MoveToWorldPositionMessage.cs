using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class MoveToWorldPositionMessage : ProcessedMessage<MoveToWorldPositionMessage>
{
	public long ChildId;

	public Vector3 WorldPosition;

	public Quaternion WorldRotation;

	public Vector3 WorldVelocity;

	public Vector3 AngularVelocity;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			DynamicThing dynamicThing = Thing.Find<DynamicThing>(ChildId);
			if (null == dynamicThing)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, ChildId, 3f, "MoveToWorldPositionMessage");
			}
			else
			{
				dynamicThing.MoveToWorld(WorldPosition, WorldRotation, WorldVelocity, AngularVelocity);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ChildId = reader.ReadInt64();
		WorldPosition = reader.ReadVector3();
		WorldRotation = reader.ReadQuaternion();
		WorldVelocity = reader.ReadVector3();
		AngularVelocity = reader.ReadVector3();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ChildId);
		writer.WriteVector3(WorldPosition);
		writer.WriteQuaternion(WorldRotation);
		writer.WriteVector3(WorldVelocity);
		writer.WriteVector3(AngularVelocity);
	}
}
