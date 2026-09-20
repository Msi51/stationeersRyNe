using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class MoveToWorldMessage : ProcessedMessage<MoveToWorldMessage>
{
	public long ChildId { get; set; }

	public float Force { get; set; }

	public bool IsPrecisionPlacement { get; set; }

	public Vector3 Position { get; set; }

	public Quaternion Rotation { get; set; }

	public Vector3 Velocity { get; set; }

	public Vector3 AngularVelocity { get; set; }

	public override void Process(long hostId)
	{
		DynamicThing dynamicThing = Thing.Find<DynamicThing>(ChildId);
		if (IsPrecisionPlacement)
		{
			dynamicThing.MoveToWorld(Position, Rotation, Velocity, AngularVelocity, Force);
		}
		else
		{
			dynamicThing.MoveToWorld(Force);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ChildId = reader.ReadInt64();
		Force = reader.ReadSingle();
		IsPrecisionPlacement = reader.ReadBoolean();
		if (IsPrecisionPlacement)
		{
			Position = reader.ReadVector3();
			Rotation = reader.ReadQuaternion();
			Velocity = reader.ReadVector3();
			AngularVelocity = reader.ReadVector3();
		}
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ChildId);
		writer.WriteSingle(Force);
		writer.WriteBoolean(IsPrecisionPlacement);
		if (IsPrecisionPlacement)
		{
			writer.WriteVector3(Position);
			writer.WriteQuaternion(Rotation);
			writer.WriteVector3(Velocity);
			writer.WriteVector3(AngularVelocity);
		}
	}
}
