using Assets.Scripts.UI;

public static class ControllerMap
{
	public static readonly ControllerAssignment HorizontalMovement = new ControllerAssignment(1f);

	public static readonly ControllerAssignment ForwardMovement = new ControllerAssignment(-1f);

	public static readonly ControllerAssignment VerticalMovement = new ControllerAssignment(-1f);

	public static readonly ControllerAssignment VerticalLook = new ControllerAssignment(-1f);

	public static readonly ControllerAssignment HorizontalLook = new ControllerAssignment(1f);
}
