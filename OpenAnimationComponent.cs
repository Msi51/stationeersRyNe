using Assets.Scripts.Objects;

public abstract class OpenAnimationComponent : BinaryTransformAnimComponent
{
	public override InteractableType AssignedAction => InteractableType.Open;

	protected override int InteractableState
	{
		get
		{
			if (!parentThing)
			{
				return 0;
			}
			if (!parentThing.IsOpen)
			{
				return 0;
			}
			return 1;
		}
	}
}
