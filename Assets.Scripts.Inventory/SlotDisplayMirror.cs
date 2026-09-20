using System;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;

namespace Assets.Scripts.Inventory;

[Serializable]
public class SlotDisplayMirror : SlotDisplay
{
	public SlotDisplay MirrorParentSlotDisplay { get; set; }

	public override Slot Slot
	{
		get
		{
			return MirrorParentSlotDisplay?.Slot;
		}
		set
		{
		}
	}

	public SlotDisplayMirror(SlotDisplayButton parent)
		: base(parent)
	{
	}

	public SlotDisplayMirror(SlotDisplayButton parent, Slot slot)
		: base(parent, slot)
	{
	}

	public void Link(SlotDisplay parent)
	{
		if (parent != null)
		{
			MirrorParentSlotDisplay = parent;
			parent.MirrorChildSlotDisplay = this;
		}
	}

	public void Unlink()
	{
		if (MirrorParentSlotDisplay != null)
		{
			MirrorParentSlotDisplay.MirrorChildSlotDisplay = null;
			MirrorParentSlotDisplay = null;
		}
	}
}
