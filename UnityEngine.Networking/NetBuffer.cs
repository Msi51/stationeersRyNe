using System;

namespace UnityEngine.Networking;

internal class NetBuffer
{
	private const int KInitialSize = 64;

	private const float KGrowthFactor = 1.5f;

	private const int KBufferSizeWarning = 134217728;

	private byte[] _mBuffer;

	private uint _mPos;

	public uint Position => _mPos;

	public NetBuffer()
	{
		_mBuffer = new byte[64];
	}

	public NetBuffer(byte[] buffer)
	{
		_mBuffer = buffer;
	}

	public byte ReadByte()
	{
		if (_mPos >= _mBuffer.Length)
		{
			throw new IndexOutOfRangeException("ByteArrayNetworkReader:ReadByte out of range:" + ToString());
		}
		return _mBuffer[_mPos++];
	}

	public void ReadBytes(byte[] buffer, uint count)
	{
		if (_mPos + count > _mBuffer.Length)
		{
			throw new IndexOutOfRangeException("ByteArrayNetworkReader:ReadBytes out of range: (" + count + ") " + ToString());
		}
		for (ushort num = 0; num < count; num++)
		{
			buffer[num] = _mBuffer[_mPos + num];
		}
		_mPos += count;
	}

	public void ReadChars(char[] buffer, uint count)
	{
		if (_mPos + count > _mBuffer.Length)
		{
			throw new IndexOutOfRangeException("ByteArrayNetworkReader:ReadChars out of range: (" + count + ") " + ToString());
		}
		for (ushort num = 0; num < count; num++)
		{
			buffer[num] = (char)_mBuffer[_mPos + num];
		}
		_mPos += count;
	}

	internal ArraySegment<byte> AsArraySegment()
	{
		return new ArraySegment<byte>(_mBuffer, 0, (int)_mPos);
	}

	public void WriteByte(byte value)
	{
		WriteCheckForSpace(1);
		_mBuffer[_mPos] = value;
		_mPos++;
	}

	public void WriteByte2(byte value0, byte value1)
	{
		WriteCheckForSpace(2);
		_mBuffer[_mPos] = value0;
		_mBuffer[_mPos + 1] = value1;
		_mPos += 2u;
	}

	public void WriteByte4(byte value0, byte value1, byte value2, byte value3)
	{
		WriteCheckForSpace(4);
		_mBuffer[_mPos] = value0;
		_mBuffer[_mPos + 1] = value1;
		_mBuffer[_mPos + 2] = value2;
		_mBuffer[_mPos + 3] = value3;
		_mPos += 4u;
	}

	public void WriteByte8(byte value0, byte value1, byte value2, byte value3, byte value4, byte value5, byte value6, byte value7)
	{
		WriteCheckForSpace(8);
		_mBuffer[_mPos] = value0;
		_mBuffer[_mPos + 1] = value1;
		_mBuffer[_mPos + 2] = value2;
		_mBuffer[_mPos + 3] = value3;
		_mBuffer[_mPos + 4] = value4;
		_mBuffer[_mPos + 5] = value5;
		_mBuffer[_mPos + 6] = value6;
		_mBuffer[_mPos + 7] = value7;
		_mPos += 8u;
	}

	public void WriteBytesAtOffset(byte[] buffer, ushort targetOffset, ushort count)
	{
		uint num = (uint)(count + targetOffset);
		WriteCheckForSpace((ushort)num);
		if (targetOffset == 0 && count == buffer.Length)
		{
			buffer.CopyTo(_mBuffer, _mPos);
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				_mBuffer[targetOffset + i] = buffer[i];
			}
		}
		if (num > _mPos)
		{
			_mPos = num;
		}
	}

	public void WriteBytes(byte[] buffer, ushort count)
	{
		WriteCheckForSpace(count);
		if (count == buffer.Length)
		{
			buffer.CopyTo(_mBuffer, _mPos);
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				_mBuffer[_mPos + i] = buffer[i];
			}
		}
		_mPos += count;
	}

	private void WriteCheckForSpace(ushort count)
	{
		if (_mPos + count < _mBuffer.Length)
		{
			return;
		}
		int num = (int)((double)_mBuffer.Length * 1.5);
		while (_mPos + count >= num)
		{
			num = (int)((double)num * 1.5);
			if (num > 134217728)
			{
				Debug.LogWarning("NetworkBuffer size is " + num + " bytes!");
			}
		}
		byte[] array = new byte[num];
		_mBuffer.CopyTo(array, 0);
		_mBuffer = array;
	}

	public void FinishMessage()
	{
		ushort num = (ushort)(_mPos - 4);
		_mBuffer[0] = (byte)(num & 0xFF);
		_mBuffer[1] = (byte)((num >> 8) & 0xFF);
	}

	public void SeekZero()
	{
		_mPos = 0u;
	}

	public void Replace(byte[] buffer)
	{
		_mBuffer = buffer;
		_mPos = 0u;
	}

	public override string ToString()
	{
		return $"NetBuf sz:{_mBuffer.Length} pos:{_mPos}";
	}
}
