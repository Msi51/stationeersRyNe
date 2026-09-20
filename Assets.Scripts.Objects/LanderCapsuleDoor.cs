using UnityEngine;

namespace Assets.Scripts.Objects;

public class LanderCapsuleDoor : DraggableThing
{
	[SerializeField]
	private GenericAssignableAnimComponent _handleAnimComponent;

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_handleAnimComponent.RefreshState(skipAnimation);
	}
}
