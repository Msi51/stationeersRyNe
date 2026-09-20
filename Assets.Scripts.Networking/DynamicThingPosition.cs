using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public struct DynamicThingPosition : IRocketReaderWriter
{
	public long DynamicThingId;

	public Vector3 WorldPosition;

	public Quaternion WorldRotation;

	public Vector3 AngularVelocity;

	public Vector3 Velocity;

	public Vector3 ChildPosition;

	public Vector3 LocalPosition;

	public bool UseLocal;

	public bool ForceUpdate;

	public bool IsSleeping;

	public bool Snap;

	public byte UpdateFlags;

	private static readonly float Tolerance = 0.01f;

	private static readonly float FineTolerance = 0.001f;

	private static readonly float PlayerToleranceMultiplier = 0.5f;

	public void SetUpdateFlags(in DynamicThingPosition lastSentValues, bool isPlayer = false)
	{
		UpdateFlags = 0;
		float num = (isPlayer ? PlayerToleranceMultiplier : 1f);
		if (!RocketMath.Approximately(WorldPosition, lastSentValues.WorldPosition, FineTolerance * num) || isPlayer)
		{
			UpdateFlags |= 1;
		}
		if (!RocketMath.Approximately(WorldRotation, lastSentValues.WorldRotation, FineTolerance * num))
		{
			UpdateFlags |= 2;
		}
		if (!RocketMath.Approximately(AngularVelocity, lastSentValues.AngularVelocity, Tolerance))
		{
			UpdateFlags |= 4;
		}
		if (!RocketMath.Approximately(Velocity, lastSentValues.Velocity, Tolerance))
		{
			UpdateFlags |= 8;
		}
		if (!RocketMath.Approximately(ChildPosition, lastSentValues.ChildPosition, FineTolerance))
		{
			UpdateFlags |= 16;
		}
		if (UseLocal && !RocketMath.Approximately(LocalPosition, lastSentValues.LocalPosition, FineTolerance))
		{
			UpdateFlags |= 32;
		}
		if (ForceUpdate != lastSentValues.ForceUpdate)
		{
			UpdateFlags |= 64;
		}
		if (IsSleeping != lastSentValues.IsSleeping)
		{
			UpdateFlags |= 131;
		}
	}

	public void Read(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		DynamicThingId = referenceId;
		WorldPosition = reader.ReadVector3();
		WorldRotation = reader.ReadQuaternion();
		AngularVelocity = reader.ReadVector3();
		Velocity = reader.ReadVector3();
		ChildPosition = reader.ReadVector3();
		UseLocal = reader.ReadBoolean();
		LocalPosition = reader.ReadVector3();
		ForceUpdate = reader.ReadBoolean();
		IsSleeping = reader.ReadBoolean();
		Snap = reader.ReadBoolean();
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, DynamicThingId);
		writer.WriteVector3(WorldPosition);
		writer.WriteQuaternion(WorldRotation);
		writer.WriteVector3(AngularVelocity);
		writer.WriteVector3(Velocity);
		writer.WriteVector3(ChildPosition);
		writer.WriteBoolean(UseLocal);
		writer.WriteVector3(LocalPosition);
		writer.WriteBoolean(ForceUpdate);
		writer.WriteBoolean(IsSleeping);
		writer.WriteBoolean(Snap);
	}

	public void ReadLite(RocketBinaryReader reader, DynamicThing dynamicThing)
	{
		UpdateFlags = reader.ReadByte();
		DynamicThingId = dynamicThing.ReferenceId;
		Snap = reader.ReadBoolean();
		if ((UpdateFlags & 1) != 0)
		{
			WorldPosition = reader.ReadVector3();
		}
		if ((UpdateFlags & 2) != 0)
		{
			WorldRotation = reader.ReadQuaternion();
		}
		if ((UpdateFlags & 4) != 0)
		{
			AngularVelocity = reader.ReadVector3();
		}
		if ((UpdateFlags & 8) != 0)
		{
			Velocity = reader.ReadVector3();
		}
		if ((UpdateFlags & 0x10) != 0)
		{
			ChildPosition = reader.ReadVector3();
		}
		if ((UpdateFlags & 0x20) != 0)
		{
			UseLocal = reader.ReadBoolean();
			LocalPosition = reader.ReadVector3();
		}
		if ((UpdateFlags & 0x40) != 0)
		{
			ForceUpdate = reader.ReadBoolean();
		}
		if ((UpdateFlags & 0x80) != 0)
		{
			IsSleeping = reader.ReadBoolean();
		}
	}

	public void WriteLite(RocketBinaryWriter writer, ref DynamicThingPosition lastValue)
	{
		writer.WriteByte(UpdateFlags);
		writer.WriteBoolean(Snap);
		lastValue.Snap = Snap;
		if ((UpdateFlags & 1) != 0)
		{
			writer.WriteVector3(WorldPosition);
			lastValue.WorldPosition = WorldPosition;
		}
		if ((UpdateFlags & 2) != 0)
		{
			writer.WriteQuaternion(WorldRotation);
			lastValue.WorldRotation = WorldRotation;
		}
		if ((UpdateFlags & 4) != 0)
		{
			writer.WriteVector3(AngularVelocity);
			lastValue.AngularVelocity = AngularVelocity;
		}
		if ((UpdateFlags & 8) != 0)
		{
			writer.WriteVector3(Velocity);
			lastValue.Velocity = Velocity;
		}
		if ((UpdateFlags & 0x10) != 0)
		{
			writer.WriteVector3(ChildPosition);
			lastValue.ChildPosition = ChildPosition;
		}
		if ((UpdateFlags & 0x20) != 0)
		{
			writer.WriteBoolean(UseLocal);
			lastValue.UseLocal = UseLocal;
			writer.WriteVector3(LocalPosition);
			lastValue.LocalPosition = LocalPosition;
		}
		if ((UpdateFlags & 0x40) != 0)
		{
			writer.WriteBoolean(ForceUpdate);
			lastValue.ForceUpdate = ForceUpdate;
		}
		if ((UpdateFlags & 0x80) != 0)
		{
			writer.WriteBoolean(IsSleeping);
			lastValue.IsSleeping = IsSleeping;
		}
	}
}
