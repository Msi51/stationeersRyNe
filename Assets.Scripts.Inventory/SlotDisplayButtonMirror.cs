using Assets.Scripts.UI;

namespace Assets.Scripts.Inventory;

public class SlotDisplayButtonMirror : SlotDisplayButton
{
	public SlotDisplayButton MirrorParentSlotDisplayButton { get; set; }

	public override InventoryWindow SlotWindow
	{
		get
		{
			return MirrorParentSlotDisplayButton?.SlotWindow;
		}
		set
		{
		}
	}

	public override void PrimaryAction(bool isButtonPress)
	{
		MirrorParentSlotDisplayButton.PrimaryAction(isButtonPress);
	}

	public void Link(SlotDisplayButton parent)
	{
		if ((bool)parent)
		{
			MirrorParentSlotDisplayButton = parent;
			parent.MirrorChildSlotDisplayButton = this;
		}
	}

	public void Unlink()
	{
		if ((bool)MirrorParentSlotDisplayButton)
		{
			MirrorParentSlotDisplayButton.MirrorChildSlotDisplayButton = null;
			MirrorParentSlotDisplayButton = null;
		}
	}
}
