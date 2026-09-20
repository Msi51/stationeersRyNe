using System;
using System.Buffers;
using System.Buffers.Binary;
using Assets.Scripts.Networking;
using Cysharp.Threading.Tasks;
using LZ4;

namespace Assets.Scripts;

public class FragmentStream
{
	private const byte FRAME_HEADER = 1;

	private const byte FRAME_FRAGMENT = 2;

	private const int FRAME_OVERHEAD = 2;

	private const int FRAME_HEADER_SIZE = 10;

	private readonly NetworkChannel _channel;

	private readonly string _name;

	private byte[] _buffer;

	private uint _totalBytes;

	private uint _receivedBytes;

	private bool _waiting = true;

	private byte _activeCycleId;

	private bool _resyncActive;

	private uint _receivedTick;

	private readonly RocketBinaryWriter _writer = new RocketBinaryWriter(16777216);

	private bool _sending;

	private byte _sendCycleId;

	public long TotalBytesSent { get; private set; }

	public long TotalBytesReceived { get; private set; }

	public long CyclesSent { get; private set; }

	public long CyclesReceived { get; private set; }

	public long CyclesAbandoned { get; private set; }

	public long FragmentsReceived { get; private set; }

	public int LastCompressedBytes { get; private set; }

	public int LastReceivedBytes { get; private set; }

	public uint LastReceivedTick => _receivedTick;

	public FragmentStream(NetworkChannel channel, string name)
	{
		_channel = channel;
		_name = name;
	}

	public void Reset()
	{
		_buffer = null;
		_totalBytes = 0u;
		_receivedBytes = 0u;
		_waiting = true;
		_resyncActive = false;
	}

	public async UniTask Send(uint tick, Action<RocketBinaryWriter, uint> writePayload, bool logging = false)
	{
		if (_sending)
		{
			return;
		}
		_sending = true;
		try
		{
			writePayload(_writer, tick);
			int length = _writer.Length;
			if (length == 0)
			{
				return;
			}
			byte[] tmp = ArrayPool<byte>.Shared.Rent(length);
			try
			{
				_writer.AsSpan().CopyTo(tmp.AsSpan(0, length));
				byte[] compressedData = LZ4Codec.Wrap(tmp, 0, length);
				uint totalBytes = (uint)(LastCompressedBytes = compressedData.Length);
				byte cycleId = ++_sendCycleId;
				SendHeader(cycleId, tick, totalBytes);
				int messagesThisFrame = 0;
				int currentByte = 0;
				while (currentByte < totalBytes)
				{
					int num2 = (int)Math.Min(totalBytes - (uint)currentByte, (uint)(NetworkServer.FragmentSize - 2));
					byte[] fragmentBytes = ArrayPool<byte>.Shared.Rent(num2 + 2);
					try
					{
						fragmentBytes[0] = 2;
						fragmentBytes[1] = cycleId;
						Buffer.BlockCopy(compressedData, currentByte, fragmentBytes, 2, num2);
						currentByte += num2;
						await NetworkServer.SendToClientsDirect(fragmentBytes, num2 + 2, _channel, excludeConnecting: true, -1L);
						messagesThisFrame++;
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(fragmentBytes);
					}
				}
				if (logging)
				{
					ConsoleWindow.Print($"{_name}: sent {messagesThisFrame} fragments with a total of {totalBytes} bytes to clients.");
				}
				TotalBytesSent += totalBytes;
				CyclesSent++;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(tmp);
			}
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
		finally
		{
			_writer.Reset();
			_sending = false;
		}
	}

	private void SendHeader(byte cycleId, uint tick, uint totalBytes)
	{
		Span<byte> data = stackalloc byte[10];
		data[0] = 1;
		data[1] = cycleId;
		BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(2), tick);
		BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(6), totalBytes);
		NetworkServer.SendToClientsDirect(data, _channel, excludeConnecting: true, -1L);
	}

	public void Receive(byte[] bytes, int size, Action<byte[], uint> apply)
	{
		FragmentsReceived++;
		if (size < 2)
		{
			EnterResync($"frame too small ({size}B)");
			return;
		}
		byte b = bytes[0];
		byte b2 = bytes[1];
		switch (b)
		{
		case 1:
			if (size < 10)
			{
				EnterResync($"header too small ({size}B)");
				break;
			}
			if (!_waiting)
			{
				CyclesAbandoned++;
			}
			_receivedTick = BitConverter.ToUInt32(bytes, 2);
			_totalBytes = BitConverter.ToUInt32(bytes, 6);
			if (_totalBytes == 0 || _totalBytes > 16777216)
			{
				EnterResync($"header declared implausible size {_totalBytes}B");
				break;
			}
			if (_buffer == null || _buffer.Length < _totalBytes)
			{
				_buffer = new byte[_totalBytes];
			}
			_receivedBytes = 0u;
			_activeCycleId = b2;
			_waiting = false;
			_resyncActive = false;
			break;
		default:
			EnterResync($"unknown frame tag {b}");
			break;
		case 2:
		{
			if (_waiting || b2 != _activeCycleId)
			{
				EnterResync(_waiting ? "fragment arrived before any header" : $"fragment for cycle {b2}, filling {_activeCycleId}");
				break;
			}
			int num = size - 2;
			if ((uint)((int)_receivedBytes + num) > _totalBytes)
			{
				EnterResync("received too many bytes");
				break;
			}
			Buffer.BlockCopy(bytes, 2, _buffer, (int)_receivedBytes, num);
			_receivedBytes += (uint)num;
			if (_receivedBytes == _totalBytes)
			{
				_waiting = true;
				CyclesReceived++;
				LastReceivedBytes = (int)_totalBytes;
				TotalBytesReceived += _totalBytes;
				apply(_buffer, _receivedTick);
			}
			break;
		}
		}
	}

	private void EnterResync(string reason)
	{
		if (!_waiting)
		{
			CyclesAbandoned++;
		}
		_waiting = true;
		if (!_resyncActive)
		{
			_resyncActive = true;
			ConsoleWindow.Print(_name + " reassembly lost sync (" + reason + "); dropping until next header");
		}
	}
}
