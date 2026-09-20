using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public class RocketBinaryReader : RocketBinaryCore
{
	private readonly Stream _stream;

	public RocketBinaryReader(Stream stream)
	{
		_stream = stream;
	}

	protected virtual void PostRead(NetworkDataType networkDataType, object value)
	{
	}

	public Type ReadMessageType()
	{
		return MessageFactory.GetTypeFromIndex(ReadByte());
	}

	private void ReadExactly(Span<byte> dest)
	{
		int num2;
		for (int i = 0; i < dest.Length; i += num2)
		{
			Stream stream = _stream;
			Span<byte> span = dest;
			int num = i;
			num2 = stream.Read(span.Slice(num, span.Length - num));
			if (num2 <= 0)
			{
				throw new EndOfStreamException("Unexpected end of stream.");
			}
		}
	}

	public virtual bool ReadBoolean()
	{
		return ReadByte() == 1;
	}

	public virtual byte ReadByte()
	{
		int num = _stream.ReadByte();
		if (num < 0)
		{
			throw new EndOfStreamException("Unexpected end of stream while reading Byte.");
		}
		return (byte)num;
	}

	public virtual sbyte ReadSByte()
	{
		int num = _stream.ReadByte();
		if (num < 0)
		{
			throw new EndOfStreamException("Unexpected end of stream while reading SByte.");
		}
		return (sbyte)(byte)num;
	}

	public virtual void ReadBytes(byte[] dest, int count)
	{
		int num;
		for (int i = 0; i < count; i += num)
		{
			num = _stream.Read(dest, i, count - i);
			if (num <= 0)
			{
				throw new EndOfStreamException($"Unexpected end of stream while reading {count} bytes.");
			}
		}
	}

	public virtual int ReadInt32()
	{
		Span<byte> span = stackalloc byte[4];
		ReadExactly(span);
		return BinaryPrimitives.ReadInt32LittleEndian(span);
	}

	public virtual uint ReadUInt32()
	{
		Span<byte> span = stackalloc byte[4];
		ReadExactly(span);
		return BinaryPrimitives.ReadUInt32LittleEndian(span);
	}

	public virtual short ReadInt16()
	{
		Span<byte> span = stackalloc byte[2];
		ReadExactly(span);
		return BinaryPrimitives.ReadInt16LittleEndian(span);
	}

	public virtual ushort ReadUInt16()
	{
		Span<byte> span = stackalloc byte[2];
		ReadExactly(span);
		return BinaryPrimitives.ReadUInt16LittleEndian(span);
	}

	public virtual long ReadInt64()
	{
		Span<byte> span = stackalloc byte[8];
		ReadExactly(span);
		return BinaryPrimitives.ReadInt64LittleEndian(span);
	}

	public virtual ulong ReadUInt64()
	{
		Span<byte> span = stackalloc byte[8];
		ReadExactly(span);
		return BinaryPrimitives.ReadUInt64LittleEndian(span);
	}

	public virtual float ReadSingle()
	{
		Span<byte> span = stackalloc byte[4];
		ReadExactly(span);
		return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(span));
	}

	public virtual float ReadFloatHalf()
	{
		return RocketMath.HalfToFloat(ReadUInt16());
	}

	public virtual double ReadDouble()
	{
		Span<byte> span = stackalloc byte[8];
		ReadExactly(span);
		return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(span));
	}

	public virtual Quaternion ReadQuaternion()
	{
		return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
	}

	public virtual Quaternion ReadQuaternionHalf()
	{
		return new Quaternion(RocketMath.HalfToFloat(ReadUInt16()), RocketMath.HalfToFloat(ReadUInt16()), RocketMath.HalfToFloat(ReadUInt16()), RocketMath.HalfToFloat(ReadUInt16())).normalized;
	}

	public virtual Color ReadColour()
	{
		return new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
	}

	public virtual AnimationCurve ReadAnimationCurve()
	{
		AnimationCurve animationCurve = new AnimationCurve();
		ushort num = ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Keyframe key = new Keyframe
			{
				time = ReadSingle(),
				value = ReadSingle(),
				inTangent = ReadSingle(),
				inWeight = ReadSingle(),
				outTangent = ReadSingle(),
				outWeight = ReadSingle(),
				weightedMode = (WeightedMode)ReadByte()
			};
			animationCurve.AddKey(key);
		}
		return animationCurve;
	}

	public virtual Vector3 ReadVector3()
	{
		return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
	}

	public virtual Vector3d ReadVector3d()
	{
		return new Vector3d(ReadDouble(), ReadDouble(), ReadDouble());
	}

	public virtual Vector3 ReadVector3Half()
	{
		return new Vector3(RocketMath.HalfToFloat(ReadUInt16()), RocketMath.HalfToFloat(ReadUInt16()), RocketMath.HalfToFloat(ReadUInt16()));
	}

	public virtual WorldGrid ReadWorldGrid()
	{
		return new WorldGrid(ReadInt16() * 10, ReadInt16() * 10, ReadInt16() * 10);
	}

	public virtual Grid3 ReadGrid3()
	{
		return new Grid3(ReadInt32(), ReadInt32(), ReadInt32());
	}

	public virtual string ReadString()
	{
		Span<byte> span = stackalloc byte[4];
		int num2;
		for (int i = 0; i < 4; i += num2)
		{
			Stream stream = _stream;
			Span<byte> span2 = span;
			int num = i;
			num2 = stream.Read(span2.Slice(num, span2.Length - num));
			if (num2 == 0)
			{
				throw new EndOfStreamException("Unexpected end of stream while reading string length.");
			}
		}
		int num3 = BinaryPrimitives.ReadInt32LittleEndian(span);
		if (num3 == -1 || num3 == 0)
		{
			return string.Empty;
		}
		if (num3 < 0)
		{
			throw new InvalidDataException($"Invalid string length: {num3}");
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(num3);
		try
		{
			int num4;
			for (int j = 0; j < num3; j += num4)
			{
				num4 = _stream.Read(array, j, num3 - j);
				if (num4 == 0)
				{
					throw new EndOfStreamException("Unexpected end of stream while reading string data.");
				}
			}
			return Encoding.UTF8.GetString(array, 0, num3);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual ushort ReadNetworkUpdateType()
	{
		return ReadUInt16();
	}

	public AsciiString ReadAscii()
	{
		int num = ReadInt32();
		Span<byte> span = stackalloc byte[num];
		for (int i = 0; i < num; i++)
		{
			span[i] = ReadByte();
		}
		return new AsciiString(span.ToArray());
	}
}
