using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.UI;

namespace Assets.Scripts.Objects;

public class SlotDisplayBase
{
	[NonSerialized]
	public SlotDisplay Display;

	public void Animate(SlotDisplayState value)
	{
		if (Display != null)
		{
			Display.Animate(value);
		}
	}
}
