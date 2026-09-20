using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public class RocketBinaryWriter : RocketBinaryCore
{
	private readonly byte[] _buffer;

	private int _position;

	private readonly ArrayPool<byte> _pool;

	private bool _returned;

	public int Position
	{
		get
		{
			return _position;
		}
		set
		{
			if ((uint)value > (uint)_buffer.Length)
			{
				throw new OverflowException("Attempted to set position outside of buffer.");
			}
			Length = Math.Max(Length, value);
			_position = value;
		}
	}

	public int Length { get; private set; }

	public RocketBinaryWriter(int bufferSize)
	{
		_pool = ArrayPool<byte>.Shared;
		_buffer = _pool.Rent(bufferSize);
		Position = 0;
	}

	protected virtual void PreWrite(NetworkDataType networkDataType, object value)
	{
	}

	private void Ensure(int count)
	{
		if ((uint)(Position + count) > (uint)_buffer.Length)
		{
			throw new OverflowException("Buffer too small for message.");
		}
	}

	public void WriteMessageType(Type value)
	{
		Ensure(1);
		WriteByte(MessageFactory.GetIndexFromType(value));
	}

	public void WriteBoolean(bool value)
	{
		Ensure(1);
		_buffer[Position++] = (byte)(value ? 1 : 0);
	}

	public void WriteByte(byte value)
	{
		Ensure(1);
		_buffer[Position++] = value;
	}

	public void WriteSByte(sbyte value)
	{
		Ensure(1);
		_buffer[Position++] = (byte)value;
	}

	public void WriteBytes(byte[] src, int count)
	{
		if (count > 0)
		{
			Ensure(count);
			Buffer.BlockCopy(src, 0, _buffer, Position, count);
			Position += count;
		}
	}

	public void WriteInt32(int value)
	{
		Ensure(4);
		BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(Position, 4), value);
		Position += 4;
	}

	public void WriteUInt32(uint value)
	{
		Ensure(4);
		BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(Position, 4), value);
		Position += 4;
	}

	public void WriteInt16(short value)
	{
		Ensure(2);
		BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(Position, 2), value);
		Position += 2;
	}

	public void WriteUInt16(ushort value)
	{
		Ensure(2);
		BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(Position, 2), value);
		Position += 2;
	}

	public void WriteInt64(long value)
	{
		Ensure(8);
		BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(Position, 8), value);
		Position += 8;
	}

	public void WriteUInt64(ulong value)
	{
		Ensure(8);
		BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(Position, 8), value);
		Position += 8;
	}

	public void WriteSingle(float value)
	{
		Ensure(4);
		int value2 = BitConverter.SingleToInt32Bits(value);
		BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(Position, 4), value2);
		Position += 4;
	}

	public void WriteDouble(double value)
	{
		Ensure(8);
		long value2 = BitConverter.DoubleToInt64Bits(value);
		BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(Position, 8), value2);
		Position += 8;
	}

	public void WriteQuaternion(Quaternion value)
	{
		WriteSingle(value.x);
		WriteSingle(value.y);
		WriteSingle(value.z);
		WriteSingle(value.w);
	}

	public void WriteQuaternionHalf(Quaternion value)
	{
		WriteUInt16(Mathf.FloatToHalf(value.x));
		WriteUInt16(Mathf.FloatToHalf(value.y));
		WriteUInt16(Mathf.FloatToHalf(value.z));
		WriteUInt16(Mathf.FloatToHalf(value.w));
	}

	public void WriteColour(Color value)
	{
		WriteSingle(value.r);
		WriteSingle(value.g);
		WriteSingle(value.b);
		WriteSingle(value.a);
	}

	public void WriteAnimationCurve(AnimationCurve value)
	{
		WriteUInt16((ushort)value.length);
		Keyframe[] keys = value.keys;
		for (int i = 0; i < keys.Length; i++)
		{
			Keyframe keyframe = keys[i];
			WriteSingle(keyframe.time);
			WriteSingle(keyframe.value);
			WriteSingle(keyframe.inTangent);
			WriteSingle(keyframe.inWeight);
			WriteSingle(keyframe.outTangent);
			WriteSingle(keyframe.outWeight);
			WriteByte((byte)keyframe.weightedMode);
		}
	}

	public void WriteVector3(Vector3 value)
	{
		WriteSingle(value.x);
		WriteSingle(value.y);
		WriteSingle(value.z);
	}

	public void WriteVector3d(Vector3d value)
	{
		WriteDouble(value.x);
		WriteDouble(value.y);
		WriteDouble(value.z);
	}

	public void WriteFloatHalf(float value)
	{
		WriteUInt16(Mathf.FloatToHalf(value));
	}

	public void WriteVector3Half(Vector3 value)
	{
		WriteUInt16(Mathf.FloatToHalf(value.x));
		WriteUInt16(Mathf.FloatToHalf(value.y));
		WriteUInt16(Mathf.FloatToHalf(value.z));
	}

	public void WriteWorldGrid(WorldGrid value)
	{
		WriteInt16((short)(value.Value.x / 10));
		WriteInt16((short)(value.Value.y / 10));
		WriteInt16((short)(value.Value.z / 10));
	}

	public void WriteGrid3(Grid3 value)
	{
		WriteInt32(value.x);
		WriteInt32(value.y);
		WriteInt32(value.z);
	}

	public void WriteString(string value)
	{
		if (value == null)
		{
			Ensure(4);
			BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(Position, 4), -1);
			Position += 4;
			return;
		}
		Encoding uTF = Encoding.UTF8;
		int byteCount = uTF.GetByteCount(value);
		int num = 4 + byteCount;
		Ensure(num);
		Span<byte> span = _buffer.AsSpan(Position, num);
		Span<byte> span2 = span;
		BinaryPrimitives.WriteInt32LittleEndian(span2.Slice(0, 4), byteCount);
		ReadOnlySpan<char> chars = value.AsSpan();
		span2 = span;
		uTF.GetBytes(chars, span2.Slice(4, span2.Length - 4));
		Position += num;
	}

	public void WriteNetworkUpdateType(ushort value)
	{
		WriteUInt16(value);
	}

	public void Seek(int offset, SeekOrigin origin)
	{
		int num = origin switch
		{
			SeekOrigin.Begin => offset, 
			SeekOrigin.Current => Position + offset, 
			SeekOrigin.End => Length + offset, 
			_ => throw new ArgumentOutOfRangeException("origin"), 
		};
		if (num < 0 || num > _buffer.Length)
		{
			throw new OverflowException("Attempted to seek outside of buffer.");
		}
		Position = num;
	}

	public void SeekZero()
	{
		Position = 0;
	}

	public void SeekEnd()
	{
		Position = Length;
	}

	public void Seek(long positionIndex, SeekOrigin origin)
	{
		Seek((int)positionIndex, origin);
	}

	public void WriteAscii(AsciiString sourceCode)
	{
		byte[] bytes = sourceCode.GetBytes();
		Ensure(bytes.Length + 4);
		WriteInt32(bytes.Length);
		bytes.AsSpan().CopyTo(_buffer.AsSpan(Position, bytes.Length));
		Position += bytes.Length;
	}

	private void ReturnBuffer()
	{
		if (!_returned)
		{
			_returned = true;
			_pool.Return(_buffer);
		}
	}

	public override void Close()
	{
		base.Close();
		ReturnBuffer();
	}

	public override void Dispose()
	{
		base.Dispose();
		ReturnBuffer();
	}

	public void Reset()
	{
		_position = 0;
		Length = 0;
	}

	public Span<byte> AsSpan()
	{
		return _buffer.AsSpan(0, Length);
	}
}
