using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ChuteInlet : DeviceImport, IRobotInput
{
	public bool AllowInput
	{
		get
		{
			if (IsNextImportReady)
			{
				return base.AllowInteraction;
			}
			return false;
		}
	}

	public Slot InputSlot => ImportSlot;

	protected override bool SkipCollectingWhenHasImportChute => false;

	protected override void OnServerImportTick()
	{
		if (base.IsImportClosed)
		{
			TryFinishImport();
			if (base.IsImportClosed && ImportingThing == null)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable == base.InteractLock)
		{
			ImportZone.enabled = interactable.State == 0;
		}
	}

	protected override void OnImportOpeningComplete()
	{
		base.OnImportOpeningComplete();
		if (GameManager.RunSimulation && ImportingThing != null)
		{
			Cooldown().Forget();
			TryFinishImport();
		}
	}

	protected override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation)
		{
			TryFinishImport();
		}
	}

	private async UniTaskVoid Cooldown()
	{
		ImportZone.enabled = false;
		await UniTask.Delay(2000);
		ImportZone.enabled = true;
	}

	private void TryFinishImport()
	{
		if (ImportingThing == null)
		{
			return;
		}
		if (ImportChute != null)
		{
			if (ImportChute.TransportSlot.Occupant == null)
			{
				ImportChute.SetNeighbor(this);
				OnServer.MoveToSlot(ImportingThing, ImportChute.TransportSlot);
			}
		}
		else
		{
			OnServer.MoveToWorld(ImportingThing, ImportSlot.Location.position, ImportSlot.Location.rotation, ImportSlot.Location.forward, Random.insideUnitSphere);
		}
	}
}
