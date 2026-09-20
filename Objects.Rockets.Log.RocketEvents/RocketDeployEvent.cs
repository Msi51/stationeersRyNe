using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets.Log.RocketEvents;

public class RocketDeployEvent : RocketEvent
{
	private IRocketPayload _deployed;

	private short _positionX;

	private short _positionZ;

	public sealed override RocketEventType RocketEventType => RocketEventType.Deploy;

	public override string TextColor => "green";

	public override string GetText()
	{
		return GameStrings.RocketLogDeploy.AsString(EventOrigin.DisplayName, _deployed.DisplayName, StringManager.Get(_positionX), StringManager.Get(_positionZ));
	}

	public RocketDeployEvent(IReferencable eventOrigin, IRocketPayload payload, float x, float z)
		: base(eventOrigin)
	{
		_deployed = payload;
		_positionX = (short)Mathf.RoundToInt(x);
		_positionZ = (short)Mathf.RoundToInt(z);
		long num = payload?.ReferenceId ?? 0;
		Hash = (int)(((uint)Hash ^ (uint)RocketEventType) * 41);
		Hash = (Hash ^ (int)num) * 41;
		Hash = (Hash ^ (int)x) * 41;
		Hash = (Hash ^ (int)z) * 41;
	}

	protected override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		Network.WritePackedId(writer, _deployed);
		writer.WriteInt16(_positionX);
		writer.WriteInt16(_positionZ);
	}

	public RocketDeployEvent(RocketBinaryReader reader)
		: base(reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		_deployed = Referencable.Find<IRocketPayload>(referenceId);
		_positionX = reader.ReadInt16();
		_positionZ = reader.ReadInt16();
	}
}
