namespace Assets.Scripts.Objects.Pipes;

public class ChuteOutlet : DeviceImportExport
{
	public override Slot ExportSlot => Slots[0];

	protected override void OnServerExportTick()
	{
		if (IsNextExportReady && ImportChute != null && ImportChute.TransportSlot.Occupant != null)
		{
			OnServer.MoveToSlot(ImportChute.TransportSlot.Occupant, ExportSlot);
		}
		if (!IsLocked && CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}
}
