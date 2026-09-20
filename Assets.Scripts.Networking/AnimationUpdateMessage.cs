using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public struct AnimationUpdateMessage : IRocketReaderWriter
{
	public long EntityId;

	public float VFloat;

	public float HFloat;

	public float VelocityFloat;

	public byte JetPackFloatCompressed;

	public byte ActiveHand;

	public byte ControlMode;

	public byte HandGrip;

	public byte CastingAnimation;

	public bool HasItemBool;

	public bool IdleBool;

	public bool FlyUpBool;

	public bool FlyDownBool;

	public bool GroundedBool;

	public bool JumpBool;

	public bool CastingBool;

	public bool Throwing;

	public bool VerticalClimb;

	public override string ToString()
	{
		return JsonUtility.ToJson(this, prettyPrint: true);
	}

	private ushort PackBoolValues()
	{
		ushort num = 0;
		if (HasItemBool)
		{
			num |= 1;
		}
		if (IdleBool)
		{
			num |= 2;
		}
		if (FlyUpBool)
		{
			num |= 4;
		}
		if (FlyDownBool)
		{
			num |= 8;
		}
		if (GroundedBool)
		{
			num |= 0x10;
		}
		if (JumpBool)
		{
			num |= 0x20;
		}
		if (CastingBool)
		{
			num |= 0x40;
		}
		if (Throwing)
		{
			num |= 0x80;
		}
		if (VerticalClimb)
		{
			num |= 0x100;
		}
		return num;
	}

	private void UnpackBoolValues(ushort packed)
	{
		HasItemBool = (packed & 1) != 0;
		IdleBool = (packed & 2) != 0;
		FlyUpBool = (packed & 4) != 0;
		FlyDownBool = (packed & 8) != 0;
		GroundedBool = (packed & 0x10) != 0;
		JumpBool = (packed & 0x20) != 0;
		CastingBool = (packed & 0x40) != 0;
		Throwing = (packed & 0x80) != 0;
		VerticalClimb = (packed & 0x100) != 0;
	}

	public void Read(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		EntityId = referenceId;
		VFloat = reader.ReadFloatHalf();
		HFloat = reader.ReadFloatHalf();
		VelocityFloat = reader.ReadFloatHalf();
		JetPackFloatCompressed = reader.ReadByte();
		ActiveHand = reader.ReadByte();
		ControlMode = reader.ReadByte();
		HandGrip = reader.ReadByte();
		CastingAnimation = reader.ReadByte();
		UnpackBoolValues(reader.ReadUInt16());
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, EntityId);
		writer.WriteFloatHalf(VFloat);
		writer.WriteFloatHalf(HFloat);
		writer.WriteFloatHalf(VelocityFloat);
		writer.WriteByte(JetPackFloatCompressed);
		writer.WriteByte(ActiveHand);
		writer.WriteByte(ControlMode);
		writer.WriteByte(HandGrip);
		writer.WriteByte(CastingAnimation);
		writer.WriteUInt16(PackBoolValues());
	}
}
