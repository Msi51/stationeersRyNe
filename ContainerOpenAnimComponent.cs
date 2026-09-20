using Assets.Scripts.Objects;
using UnityEngine;

public class ContainerOpenAnimComponent : BinaryTransformAnimComponent
{
	[SerializeField]
	private Container parentContainer;

	public override InteractableType AssignedAction => InteractableType.Open;

	protected override int InteractableState
	{
		get
		{
			if (parentThing == null || !parentThing.HasOpenState)
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

	protected override void OnAnimationStart()
	{
		base.OnAnimationStart();
		int interactableState = InteractableState;
		if (interactableState != 0 && interactableState == 1)
		{
			parentContainer.SetContentsVisibility(isVisible: true);
		}
	}

	protected override void OnAnimationCompleted()
	{
		base.OnAnimationCompleted();
		if (InteractableState != 0)
		{
			_ = 1;
		}
		else
		{
			parentContainer.SetContentsVisibility(isVisible: false);
		}
	}
}
