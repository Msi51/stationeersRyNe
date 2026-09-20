using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace WorldLogSystem;

public class TraderCrashEvent : WorldEvent
{
	public long TargetPadId;

	public long CollisionObjectId;

	public Vector3 ShuttlePosition;

	public override string TextColor => "red";

	public override WorldEventType WorldEventType => WorldEventType.TraderCrashEvent;

	public TraderCrashEvent()
	{
	}

	public TraderCrashEvent(string text, long targetPadId, long collisionObjectId, Vector3 position)
	{
		Text = text;
		DateTime = base.DateTimeNow;
		TargetPadId = targetPadId;
		CollisionObjectId = collisionObjectId;
		ShuttlePosition = position;
	}

	public override WorldEventData ToData()
	{
		return new TraderCrashEventData
		{
			Text = Text,
			DateTime = DateTime,
			TargetPadId = TargetPadId,
			CollisionObjectId = CollisionObjectId,
			ShuttlePosition = new Float3(ShuttlePosition)
		};
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteString(DateTime);
		writer.WriteString(Text);
		writer.WriteInt64(TargetPadId);
		writer.WriteInt64(CollisionObjectId);
		writer.WriteVector3(ShuttlePosition);
	}

	public static TraderCrashEvent Deserialize(RocketBinaryReader reader)
	{
		return new TraderCrashEvent
		{
			DateTime = reader.ReadString(),
			Text = reader.ReadString(),
			TargetPadId = reader.ReadInt64(),
			CollisionObjectId = reader.ReadInt64(),
			ShuttlePosition = reader.ReadVector3()
		};
	}
}
