using System;
using System.IO;
using Assets.Scripts.Networking;
using UnityEngine;

namespace TerrainSystem;

public readonly struct Minable : IEquatable<Minable>
{
	private const float SCALE = 0.7f;

	public static readonly Vector3 MinableRenderScale = new Vector3(0.7f, 0.7f, 0.7f);

	private const int X_ROTATION_MULTIPLIER = 401;

	private const int Y_ROTATION_MULTIPLIER = 457;

	private const int Z_ROTATION_MULTIPLIER = 467;

	private const float X_OFFSET_MULTIPLIER = 1f / 9f;

	private const float Y_OFFSET_MULTIPLIER = 1f / 7f;

	private const float Z_OFFSET_MULTIPLIER = 1f / 11f;

	public const byte OFFSET = 127;

	public readonly byte X;

	public readonly byte Y;

	public readonly byte Z;

	public readonly byte ParentIndex;

	public readonly bool IsActive;

	public static Minable Invalid = new Minable(0, 0, 0, 0, isActive: false);

	private const float MAX_RAND_OFFSET = 0.3f;

	private const float HALF_RAND_OFFSET = 0.15f;

	private const float MAX_RAND_OFFSET_Y = 0.2f;

	private const float HALF_RAND_OFFSET_Y = 0.1f;

	public bool IsValid => !Equals(Invalid);

	public Vector3 LocalPosition => new Vector3((int)X, (int)Y, (int)Z);

	public Quaternion Rotation => Quaternion.Euler(new Vector3(X * 401 % 360, Y * 457 % 360, Z * 467 % 360));

	private Vector3 RandomOffset => new Vector3((float)(int)X * (1f / 9f) % 0.3f - 0.15f, (float)(int)Y * (1f / 7f) % 0.2f - 0.1f, (float)(int)Z * (1f / 11f) % 0.3f - 0.15f);

	public int GetKey()
	{
		return MakeKey(X, Y, Z);
	}

	public static int MakeKey(byte x, byte y, byte z)
	{
		return x | (y << 8) | (z << 16);
	}

	public Minable(byte x, byte y, byte z, byte parentIndex, bool isActive)
	{
		X = x;
		Y = y;
		Z = z;
		ParentIndex = parentIndex;
		IsActive = isActive;
	}

	public static Minable Mined(Minable minable)
	{
		return SetActive(minable, isActive: false);
	}

	public static Minable SetActive(Minable minable, bool isActive)
	{
		return new Minable(minable.X, minable.Y, minable.Z, minable.ParentIndex, isActive);
	}

	public Vector3 WorldRenderPosition(Vector3Int veinWorldPosition)
	{
		return WorldRenderPosition(X, Y, Z, veinWorldPosition) + RandomOffset;
	}

	public Vector3Int WorldPositionInt(Vector3Int veinWorldPosition)
	{
		return WorldPositionInt(X, Y, Z, veinWorldPosition);
	}

	private static Vector3 WorldRenderPosition(byte x, byte y, byte z, Vector3 veinWorldPosition)
	{
		return new Vector3(veinWorldPosition.x + (float)(int)x - 127f, veinWorldPosition.y + (float)(int)y - 127f, veinWorldPosition.z + (float)(int)z - 127f);
	}

	public static Vector3Int WorldPositionInt(byte x, byte y, byte z, Vector3Int veinWorldPositionInt)
	{
		return new Vector3Int(veinWorldPositionInt.x + x - 127, veinWorldPositionInt.y + y - 127, veinWorldPositionInt.z + z - 127);
	}

	public bool Equals(Minable other)
	{
		if (X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Minable other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y, Z);
	}

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
		writer.Write(ParentIndex);
		writer.Write(IsActive);
	}

	public static Minable Deserialize(BinaryReader reader)
	{
		byte x = reader.ReadByte();
		byte y = reader.ReadByte();
		byte z = reader.ReadByte();
		byte parentIndex = reader.ReadByte();
		bool isActive = reader.ReadBoolean();
		return new Minable(x, y, z, parentIndex, isActive);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteByte(X);
		writer.WriteByte(Y);
		writer.WriteByte(Z);
		writer.WriteByte(ParentIndex);
		writer.WriteBoolean(IsActive);
	}

	public static Minable Read(RocketBinaryReader reader)
	{
		byte x = reader.ReadByte();
		byte y = reader.ReadByte();
		byte z = reader.ReadByte();
		byte parentIndex = reader.ReadByte();
		bool isActive = reader.ReadBoolean();
		return new Minable(x, y, z, parentIndex, isActive);
	}
}
