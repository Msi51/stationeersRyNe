using Assets.Scripts.Objects;
using UnityEngine;

public class HumanSkull : Item
{
	private Vector3 LeftHandPos = new Vector3(-0.04f, 0.013f, -0.049f);

	private Vector3 LeftHandRot = new Vector3(-73.6f, -293.5f, 302.5f);

	private Vector3 RightHandPos = new Vector3(-0.045f, 0.091f, 0.035f);

	private Vector3 RightHandRot = new Vector3(-134f, -66f, 193.8f);

	public override void SetHandPosition(bool leftHand)
	{
		if (leftHand)
		{
			LocalOffSetInHand = LeftHandPos;
			LocalRotationInHand = LeftHandRot;
		}
		else
		{
			LocalOffSetInHand = RightHandPos;
			LocalRotationInHand = RightHandRot;
		}
		base.ThingTransformLocalPosition = LocalOffSetInHand;
		base.ThingTransformLocalRotationEuler = LocalRotationInHand;
	}
}
