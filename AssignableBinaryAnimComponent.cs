using Assets.Scripts.Objects;
using UnityEngine;

public abstract class AssignableBinaryAnimComponent : BinaryTransformAnimComponent
{
	[SerializeField]
	private InteractableType interactableAction;

	private Interactable _interactable;

	public override InteractableType AssignedAction => interactableAction;

	protected override int InteractableState
	{
		get
		{
			if (!parentThing || _interactable == null)
			{
				return 0;
			}
			return _interactable.State;
		}
	}

	protected override void Init()
	{
		base.Init();
		if (_interactable == null && parentThing != null)
		{
			_interactable = parentThing.GetInteractable(interactableAction);
		}
	}
}
