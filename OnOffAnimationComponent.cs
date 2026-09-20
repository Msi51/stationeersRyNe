using Assets.Scripts.Objects;

public abstract class OnOffAnimationComponent : BinaryTransformAnimComponent
{
	public override InteractableType AssignedAction => InteractableType.OnOff;

	protected override int InteractableState
	{
		get
		{
			if (!(parentThing != null))
			{
				return 0;
			}
			if (!parentThing.OnOff)
			{
				return 0;
			}
			return 1;
		}
	}
}
