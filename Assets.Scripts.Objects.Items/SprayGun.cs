using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SprayGun : Tool, ISprayer, IUsedAmount, IUsed
{
	public float Efficiency = 4f;

	private static readonly Vector3 LeftHandRot = new Vector3(-75f, 127f, -126f);

	private static readonly Vector3 LeftHandPos = new Vector3(-0.056f, 0.0231f, -0.072f);

	private static readonly Vector3 RightHandRot = new Vector3(67f, -119f, -128f);

	private static readonly Vector3 RightHandPos = new Vector3(-0.0524f, 0.0263f, 0.0566f);

	public override bool IsOperable
	{
		get
		{
			if (!IsEmpty)
			{
				return OnOff;
			}
			return false;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if ((object)SprayCan != null)
			{
				return SprayCan.Quantity <= float.Epsilon;
			}
			return true;
		}
	}

	public override int ConstructingSoundHash => Animator.StringToHash("SprayPaintLong");

	public override int FinishedConstructingSoundHash => Animator.StringToHash("SprayPaintFinished");

	public SprayCan SprayCan => Slots[0].Occupant as SprayCan;

	public Material GetPaintMaterial()
	{
		if (!IsEmpty)
		{
			return SprayCan.PaintMaterial;
		}
		return null;
	}

	public float TimeToUse()
	{
		return 0f;
	}

	public float GetUseAmount()
	{
		if (!IsEmpty)
		{
			return SprayCan.UseAmount * (1f / Efficiency);
		}
		return 0f;
	}

	public override bool CheckTogglePower()
	{
		return true;
	}

	public override void SetHandPosition(bool leftHand)
	{
		if (leftHand)
		{
			LocalRotationInHand = LeftHandRot;
			LocalOffSetInHand = LeftHandPos;
		}
		else
		{
			LocalRotationInHand = RightHandRot;
			LocalOffSetInHand = RightHandPos;
		}
		base.ThingTransformLocalPosition = LocalOffSetInHand;
		base.ThingTransformLocalRotationEuler = LocalRotationInHand;
	}

	public override bool OnUseItem(float quantity, Thing useOnThing)
	{
		if (!IsOperable)
		{
			return false;
		}
		return SprayCan.OnUseItem(quantity * (1f / Efficiency), useOnThing);
	}
}
