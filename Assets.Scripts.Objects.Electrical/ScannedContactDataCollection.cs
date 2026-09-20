using System.Collections.Generic;
using Assets.Scripts.Networking;

namespace Assets.Scripts.Objects.Electrical;

public class ScannedContactDataCollection
{
	public List<ScannedContactData> ScannedContactData = new List<ScannedContactData>(5);

	public bool TryGetData(TraderContact contact, out ScannedContactData data)
	{
		for (int num = ScannedContactData.Count - 1; num >= 0; num--)
		{
			if (ScannedContactData[num].Contact == contact)
			{
				data = ScannedContactData[num];
				return true;
			}
		}
		data = null;
		return false;
	}

	public void Add(TraderContact contact, float scannedDegreeOffset)
	{
		ScannedContactData item = new ScannedContactData
		{
			Contact = contact,
			LastScannedDegreeOffset = scannedDegreeOffset,
			StartTimeTillResolve = 999999f,
			CurrentTimeTillResolve = 999999f
		};
		ScannedContactData.Add(item);
	}

	public void Remove(TraderContact contact)
	{
		for (int num = ScannedContactData.Count - 1; num >= 0; num--)
		{
			if (ScannedContactData[num].Contact == contact)
			{
				ScannedContactData.RemoveAt(num);
				break;
			}
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		foreach (ScannedContactData scannedContactDatum in ScannedContactData)
		{
			scannedContactDatum.NetworkUpdateFlags = byte.MaxValue;
		}
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (ScannedContactData scannedContactDatum2 in ScannedContactData)
		{
			scannedContactDatum2.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public void Read(RocketBinaryReader reader)
	{
		Network.ReadIndex<byte>(reader, out var value);
		if (ScannedContactData.Count != value)
		{
			ScannedContactData.Clear();
		}
		for (int num = ScannedContactData.Count - 1; num >= 0; num--)
		{
			if (ScannedContactData[num] == null)
			{
				ScannedContactData.Clear();
				break;
			}
		}
		for (int i = 0; i < value; i++)
		{
			if (i < ScannedContactData.Count)
			{
				ScannedContactData[i].Read(reader);
				continue;
			}
			ScannedContactData scannedContactData = new ScannedContactData();
			scannedContactData.Read(reader);
			ScannedContactData.Add(scannedContactData);
		}
	}
}
