using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public class JoinHeaderMessage : MessageBase<JoinHeaderMessage>
{
	public long ExpectedBytes;

	public void Process(long hostId)
	{
		JoinFragmentMessage.TotalBytes = ExpectedBytes;
		JoinFragmentMessage.JoinBuffer = new byte[JoinFragmentMessage.TotalBytes];
		JoinFragmentMessage.ReceivedBytes = 0;
		Debug.Log($"preparing for {JoinFragmentMessage.TotalBytes} bytes");
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ExpectedBytes = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ExpectedBytes);
	}
}
