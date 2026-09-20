using UnityEngine;
using UnityEngine.Serialization;

public class UmbilicalEndAnimComponent : AssignableBinaryAnimComponent
{
	[FormerlySerializedAs("scaleToSmallGridFactor")]
	[SerializeField]
	private float gridSize = 0.5f;

	public void UpdateExtendedPosition(float distance, bool invert = false)
	{
		KeyFrameData animDataValue = state0.GetAnimDataValue(0);
		KeyFrameData animDataValue2 = state1.GetAnimDataValue(0);
		distance -= 1f;
		float num = ((distance > 0f) ? (distance * gridSize - animDataValue.Position.z) : animDataValue.Position.z);
		animDataValue2.Position = new Vector3(animDataValue2.Position.x, animDataValue2.Position.y, invert ? (0f - num) : num);
		state1.SetAnimDataValue(0, animDataValue2);
	}
}
