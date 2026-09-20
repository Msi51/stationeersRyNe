using System.Collections.Generic;

namespace Objects.Rockets;

public class FuselageDoors : Fuselage
{
	public List<FuselageDoorAnimComponent> DoorAnimComponents = new List<FuselageDoorAnimComponent>();

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		foreach (FuselageDoorAnimComponent doorAnimComponent in DoorAnimComponents)
		{
			doorAnimComponent?.RefreshState(skipAnimation);
		}
	}
}
