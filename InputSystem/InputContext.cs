namespace InputSystem;

public readonly struct InputContext(KeyWrap wrap, InputPhase phase)
{
	public readonly KeyWrap Wrap = wrap;

	public readonly InputPhase Phase = phase;
}
