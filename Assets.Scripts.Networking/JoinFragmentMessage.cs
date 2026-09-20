using System;
using System.IO;
using Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public class JoinFragmentMessage : MessageBase<JoinFragmentMessage>
{
	public byte[] Bytes;

	public static long TotalBytes = -1L;

	public static int ReceivedFragments;

	public static int ReceivedBytes;

	public static byte[] JoinBuffer;

	public void Process(long hostId)
	{
		Debug.Log($"receiving fragment {ReceivedFragments} with {Bytes.Length} bytes");
		ReceivedFragments++;
		Buffer.BlockCopy(Bytes, 0, JoinBuffer, ReceivedBytes, Bytes.Length);
		ReceivedBytes += Bytes.Length;
		if (ReceivedBytes < TotalBytes)
		{
			return;
		}
		byte[] array = NetworkManager.Decompress(JoinBuffer);
		using MemoryStream stream = new MemoryStream(array, 0, array.Length);
		using RocketBinaryReader rocketBinaryReader = new RocketBinaryReader(stream);
		ushort num = rocketBinaryReader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			long referenceId = rocketBinaryReader.ReadInt64();
			int prefabHash = rocketBinaryReader.ReadInt32();
			Vector3 position = rocketBinaryReader.ReadVector3();
			Quaternion rotation = rocketBinaryReader.ReadQuaternion();
			Thing.Create<Thing>(prefabHash, position, rotation, referenceId).DeserializeOnJoin(rocketBinaryReader);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		int num = reader.ReadInt32();
		Bytes = new byte[num];
		reader.ReadBytes(Bytes, num);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(Bytes.Length);
		writer.WriteBytes(Bytes, Bytes.Length);
	}
}
