using Assets.Scripts.Objects;
using UnityEngine;

public class AssignableMultiStateAnimComponent : MultiStateAnimComponent
{
	[SerializeField]
	private InteractableType interactableAction = InteractableType.Mode;

	private Interactable _interactable;

	public override InteractableType AssignedAction => interactableAction;

	protected override int InteractableState
	{
		get
		{
			if (parentThing == null || _interactable == null)
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
