using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects;

public struct ShuttleUpdate(Vector3 currentThrottle, float yawVelocity, float pitchVelocity)
{
	public Vector3 CurrentThrottle = currentThrottle;

	public float YawVelocity = yawVelocity;

	public float PitchVelocity = pitchVelocity;

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteVector3Half(CurrentThrottle);
		writer.WriteFloatHalf(YawVelocity);
		writer.WriteFloatHalf(PitchVelocity);
	}

	public void Read(RocketBinaryReader reader)
	{
		CurrentThrottle = reader.ReadVector3Half();
		YawVelocity = reader.ReadFloatHalf();
		PitchVelocity = reader.ReadFloatHalf();
	}
}
