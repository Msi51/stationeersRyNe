using System;
using Assets.Scripts.Networking;

namespace UnityEngine.Networking;

public class DummyMessage : MessageBase<DummyMessage>
{
	public override void Deserialize(RocketBinaryReader reader)
	{
		throw new NotImplementedException();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		throw new NotImplementedException();
	}
}
