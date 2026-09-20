using Assets.Scripts.Objects;

public class ShutterMaterialAnimComponent : BinaryMaterialAnimComponent
{
	private Interactable _interactable;

	public override InteractableType AssignedAction => InteractableType.Open;

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
		if (_interactable == null && (bool)parentThing)
		{
			_interactable = parentThing.GetInteractable(AssignedAction);
		}
	}
}
