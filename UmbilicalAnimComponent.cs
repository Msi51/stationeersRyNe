using UnityEngine;

public class UmbilicalAnimComponent : AssignableBinaryAnimComponent
{
	public void UpdateExtendedPosition(float distance)
	{
		KeyFrameData animDataValue = state0.GetAnimDataValue(0);
		KeyFrameData animDataValue2 = state1.GetAnimDataValue(0);
		distance -= 1f;
		float z = ((distance > 0f) ? distance : animDataValue.Scale.z);
		animDataValue2.Scale = new Vector3(1f, 1f, z);
		state1.SetAnimDataValue(0, animDataValue2);
	}
}
