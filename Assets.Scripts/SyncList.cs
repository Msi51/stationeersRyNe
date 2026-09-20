using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;

namespace Assets.Scripts;

public class SyncList<T> : IClearable where T : ISyncListable
{
	private enum ListEncoding
	{
		Empty,
		Byte,
		Ushort,
		Uint
	}

	private readonly List<T> _newToSend = new List<T>(255);

	private readonly Action<RocketBinaryReader> _deserialize;

	public SyncList(Action<RocketBinaryReader> deserialize)
	{
		_deserialize = deserialize;
		Clearables.Register(this);
	}

	public void AddToFront(T newItem)
	{
		if (!_newToSend.Contains(newItem))
		{
			_newToSend.Insert(0, newItem);
		}
	}

	public void Add(T newItem)
	{
		if (!_newToSend.Contains(newItem))
		{
			_newToSend.Add(newItem);
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		int count = _newToSend.Count;
		ListEncoding listEncoding = ((count > 65535) ? ListEncoding.Uint : ((count > 255) ? ListEncoding.Ushort : ((count != 0) ? ListEncoding.Byte : ListEncoding.Empty)));
		ListEncoding listEncoding2 = listEncoding;
		writer.WriteByte((byte)listEncoding2);
		switch (listEncoding2)
		{
		case ListEncoding.Empty:
			return;
		case ListEncoding.Byte:
			writer.WriteByte((byte)count);
			break;
		case ListEncoding.Ushort:
			writer.WriteUInt16((ushort)count);
			break;
		case ListEncoding.Uint:
			writer.WriteUInt32((uint)count);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		for (int i = 0; i < count; i++)
		{
			_newToSend[i].Serialize(writer);
		}
		_newToSend.Clear();
	}

	public void Deserialize(RocketBinaryReader reader)
	{
		uint num;
		switch ((ListEncoding)reader.ReadByte())
		{
		case ListEncoding.Empty:
			return;
		case ListEncoding.Byte:
			num = reader.ReadByte();
			break;
		case ListEncoding.Ushort:
			num = reader.ReadUInt16();
			break;
		case ListEncoding.Uint:
			num = reader.ReadUInt32();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		uint num2 = num;
		for (int i = 0; i < num2; i++)
		{
			_deserialize(reader);
		}
	}

	public void Clear()
	{
		_newToSend.Clear();
	}
}
