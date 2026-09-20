using Assets.Scripts.Networking;

namespace Assets.Scripts.Objects;

public struct AnimationUpdate
{
	public float VFloat;

	public float HFloat;

	public float VelocityFloat;

	public float JetPackFloat;

	public bool HasItemBool;

	public bool IdleBool;

	public bool FlyUpBool;

	public bool FlyDownBool;

	public bool GroundedBool;

	public bool JumpBool;

	public bool CastingBool;

	public int ActiveHandInt;

	public int ControlModeInt;

	public bool VerticalClimb;

	public int HandGripInt;

	public float CastingAnimation;

	public bool Throwing;

	public void Update(AnimationUpdateMessage networkMessage)
	{
		VFloat = networkMessage.VFloat;
		HFloat = networkMessage.HFloat;
		VelocityFloat = networkMessage.VelocityFloat;
		JetPackFloat = (float)(int)networkMessage.JetPackFloatCompressed / 255f;
		HasItemBool = networkMessage.HasItemBool;
		IdleBool = networkMessage.IdleBool;
		FlyUpBool = networkMessage.FlyUpBool;
		FlyDownBool = networkMessage.FlyDownBool;
		GroundedBool = networkMessage.GroundedBool;
		JumpBool = networkMessage.JumpBool;
		CastingBool = networkMessage.CastingBool;
		ActiveHandInt = networkMessage.ActiveHand;
		ControlModeInt = networkMessage.ControlMode;
		VerticalClimb = networkMessage.VerticalClimb;
		HandGripInt = networkMessage.HandGrip;
		CastingAnimation = (int)networkMessage.CastingAnimation;
		Throwing = networkMessage.Throwing;
	}
}
