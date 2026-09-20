namespace Assets.Scripts.Objects.Electrical;

public class Unloader : SlotHandlerBase
{
	public override bool CanIceMelt => false;

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (!OnOff || !Powered)
		{
			return;
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (CanCompleteImport && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
		if (!(ImportingThing != null) || !base.IsImportClosed || !IsNextExportReady || (base.SlotHandlerMode == SlotHandlerMode.Logic && CurrentOutput < 0))
		{
			return;
		}
		if (base.SlotHandlerMode != SlotHandlerMode.Logic || CurrentOutput >= 1)
		{
			foreach (Slot slot in ImportingThing.Slots)
			{
				if (!(slot.Occupant == null) && slot.IsInteractable)
				{
					OnServer.MoveToSlot(slot.Occupant, ExportSlot);
					if (base.SlotHandlerMode == SlotHandlerMode.Logic)
					{
						CurrentOutput = -1;
					}
					return;
				}
			}
		}
		OnServer.MoveToSlot(ImportingThing, ExportSlot);
		if (base.SlotHandlerMode == SlotHandlerMode.Logic)
		{
			CurrentOutput = -1;
		}
	}

	protected override void OnServerExportTick()
	{
		if (OnOff && Powered && CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}
}
