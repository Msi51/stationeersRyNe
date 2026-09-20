using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Events;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Components;

public class ImportExport : Device, ISmartRotatable
{
	public List<ImportInfo> Imports;

	public List<ExportInfo> Exports;

	private static int MaximumDropStacks = 200;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public bool IsImportOpen(ImportInfo import)
	{
		if (import.Interactable.State == 0)
		{
			return import.IsCurrentAnimComplete;
		}
		return false;
	}

	public virtual bool IsNextImportReady(ImportInfo import)
	{
		if (IsImportOpen(import))
		{
			return import.ImportingThing == null;
		}
		return false;
	}

	public virtual bool CanBeginImport(ImportInfo import)
	{
		if (IsImportOpen(import))
		{
			return import.ImportingThing != null;
		}
		return false;
	}

	public virtual bool CanCompleteImport(ImportInfo import)
	{
		return IsImportClosed(import);
	}

	public bool IsImportClosed(ImportInfo import)
	{
		if (import.Interactable.State == 1)
		{
			return import.IsCurrentAnimComplete;
		}
		return false;
	}

	public bool IsExportClosed(ExportInfo export)
	{
		if (export.Interactable.State == 0)
		{
			return export.IsCurrentAnimComplete;
		}
		return false;
	}

	public bool IsExportOpen(ExportInfo export)
	{
		if (export.Interactable.State == 1)
		{
			return export.IsCurrentAnimComplete;
		}
		return false;
	}

	public bool IsExportChuteBlocked(ExportInfo export)
	{
		if (export.ExportChute != null)
		{
			return export.ExportChute.TransportSlot.Occupant != null;
		}
		return false;
	}

	public virtual bool CanBeginExport(ExportInfo export)
	{
		if (IsExportClosed(export))
		{
			return export.ExportingThing != null;
		}
		return false;
	}

	public virtual bool CanCompleteExport(ExportInfo export)
	{
		if (IsExportOpen(export))
		{
			if (!(export.ExportChute == null))
			{
				return export.ExportChute.TransportSlot.Occupant == null;
			}
			return true;
		}
		return false;
	}

	public virtual bool IsNextExportReady(ExportInfo export)
	{
		if (IsExportClosed(export))
		{
			return export.ExportingThing == null;
		}
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		foreach (ImportInfo import in Imports)
		{
			import.Slot = GetSlot(import.SlotType);
			import.Connection = GetConnection(import.ConnectionRole);
			import.Interactable = GetInteractable(import.InteractableType);
		}
		foreach (ExportInfo export in Exports)
		{
			export.Slot = GetSlot(export.SlotType);
			export.Connection = GetConnection(export.ConnectionRole);
			export.Interactable = GetInteractable(export.InteractableType);
		}
	}

	private new Slot GetSlot(InteractableType interactableType)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Action == interactableType)
			{
				return slot;
			}
		}
		return null;
	}

	private new Interactable GetInteractable(InteractableType interactableType)
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action == interactableType)
			{
				return interactable;
			}
		}
		return null;
	}

	private Connection GetConnection(ConnectionRole role)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.ConnectionRole == role)
			{
				return openEnd;
			}
		}
		return null;
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		foreach (ImportInfo import in Imports)
		{
			TryImportFromQueue(import);
			TryChuteImport(import);
			TryImport(import);
			CheckImportAnimState(import);
		}
		foreach (ExportInfo export in Exports)
		{
			TryExport(export);
			CheckExportAnimState(export);
		}
	}

	protected virtual void OnImportOpeningComplete()
	{
	}

	protected virtual void OnImportClosingComplete()
	{
	}

	protected virtual void OnExportOpeningComplete()
	{
	}

	protected virtual void OnExportClosingComplete()
	{
	}

	private void TryImportFromQueue(ImportInfo import)
	{
		if (import.ImportQueue.Count > 0 && IsNextImportReady(import))
		{
			OnServer.MoveToSlot(import.ImportQueue[0], import.Slot);
		}
	}

	private void TryChuteImport(ImportInfo import)
	{
		if (!(import.ImportChute == null))
		{
			DynamicThing occupant = import.ImportChute.TransportSlot.Occupant;
			if (!(occupant == null) && !(import.Slot.Occupant != null))
			{
				OnServer.MoveToSlot(occupant, import.Slot);
			}
		}
	}

	private void CheckImportAnimState(ImportInfo import)
	{
		BinaryAnimState currentState = import.AnimationComponent.CurrentState;
		if (currentState == BinaryAnimState.None)
		{
			import.ForceUpdateAnimState = true;
			import.IsCurrentAnimComplete = false;
		}
		if (currentState != import.PreviousAnimState || import.ForceUpdateAnimState)
		{
			import.ForceUpdateAnimState = false;
			switch (currentState)
			{
			case BinaryAnimState.OffToOn:
			case BinaryAnimState.OnToOff:
				import.IsCurrentAnimComplete = false;
				break;
			case BinaryAnimState.Off:
			case BinaryAnimState.On:
				import.IsCurrentAnimComplete = true;
				switch (import.Interactable.State)
				{
				case 0:
					OnImportOpeningComplete();
					break;
				case 1:
					OnImportClosingComplete();
					break;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case BinaryAnimState.None:
				break;
			}
		}
		import.PreviousAnimState = currentState;
	}

	private void TryImport(ImportInfo import)
	{
		if (CanBeginImport(import))
		{
			OnServer.Interact(import.Interactable, 1);
			import.ImportCount++;
		}
		if (CanCompleteImport(import) && import.ImportingThing == null)
		{
			OnServer.Interact(import.Interactable, 0);
		}
	}

	private void CheckExportAnimState(ExportInfo export)
	{
		BinaryAnimState currentState = export.AnimationComponent.CurrentState;
		if (currentState == BinaryAnimState.None)
		{
			export.ForceUpdateAnimState = true;
			export.IsCurrentAnimComplete = false;
		}
		if (currentState != export.PreviousAnimState || export.ForceUpdateAnimState)
		{
			export.ForceUpdateAnimState = false;
			switch (currentState)
			{
			case BinaryAnimState.OffToOn:
			case BinaryAnimState.OnToOff:
				export.IsCurrentAnimComplete = false;
				break;
			case BinaryAnimState.Off:
			case BinaryAnimState.On:
				export.IsCurrentAnimComplete = true;
				switch (export.Interactable.State)
				{
				case 0:
					OnExportClosingComplete();
					break;
				case 1:
					OnExportOpeningComplete();
					break;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case BinaryAnimState.None:
				break;
			}
		}
		export.PreviousAnimState = currentState;
	}

	private void TryExport(ExportInfo export)
	{
		if (!CanCompleteExport(export))
		{
			return;
		}
		if (export.ExportingThing != null)
		{
			if (export.ExportChute != null)
			{
				export.ExportChute.SetNeighbor(this);
				OnServer.MoveToSlot(export.ExportingThing, export.ExportChute.TransportSlot);
			}
			else
			{
				OnServer.MoveToWorld(export.ExportingThing, export.Slot, 1f);
			}
			export.ExportCount++;
		}
		OnServer.Interact(export.Interactable, 0);
	}

	public override void OnInputTriggerExit(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
		base.OnInputTriggerExit(dynamicThing, trigger);
		foreach (ImportInfo import in Imports)
		{
			if (import.Trigger == trigger)
			{
				import.ImportQueue.Remove(dynamicThing);
				break;
			}
		}
	}

	public override void OnInputTriggerEnter(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
		foreach (ImportInfo import in Imports)
		{
			if (!(import.Trigger == trigger))
			{
				continue;
			}
			if (!import.ImportChute)
			{
				Item item = dynamicThing as Item;
				if ((bool)item && !import.ImportQueue.Contains(dynamicThing) && (import.Slot.Type == Slot.Class.None || item.SlotType == import.Slot.Type))
				{
					import.ImportQueue.Add(dynamicThing);
				}
			}
			break;
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		UpdateChutes();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		foreach (ImportInfo import in Imports)
		{
			import.AnimationComponent.RefreshState(skipAnimation);
		}
		foreach (ExportInfo export in Exports)
		{
			export.AnimationComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateChutes();
	}

	public override void OnAddChuteNetwork(ChuteNetwork newNetwork)
	{
		base.OnAddChuteNetwork(newNetwork);
		UpdateChutes();
	}

	public override void OnRemoveChuteNetwork(ChuteNetwork newNetwork)
	{
		base.OnRemoveChuteNetwork(newNetwork);
		UpdateChutes();
	}

	private void UpdateChutes()
	{
		foreach (ImportInfo import in Imports)
		{
			import.ImportChute = import.Connection.GetChute();
		}
		foreach (ExportInfo export in Exports)
		{
			export.ExportChute = export.Connection.GetChute();
		}
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		base.OnInteractableStateChanged(interactable, newState, oldState);
		foreach (ImportInfo import in Imports)
		{
			if (interactable.Action == import.InteractableType)
			{
				import.IsCurrentAnimComplete = false;
				import.ForceUpdateAnimState = true;
			}
		}
		foreach (ExportInfo export in Exports)
		{
			if (interactable.Action == export.InteractableType)
			{
				export.IsCurrentAnimComplete = false;
				export.ForceUpdateAnimState = true;
			}
		}
	}

	public override void OnReleaseReagents()
	{
		base.OnReleaseReagents();
		if (GameManager.GameState == GameState.Running && GameManager.RunSimulation && ReagentMixture != null && ReagentMixture.TotalReagents > 0.0)
		{
			int num = 0;
			while (ReagentMixture.TotalReagents > 0.0 && num < MaximumDropStacks)
			{
				DropReagent(DropType == DropType.Ore);
				num++;
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		foreach (ImportInfo import in Imports)
		{
			if (newChild.ParentSlot == import.Slot)
			{
				newChild.ScaleToSlot();
				import.ImportingThing = newChild;
			}
		}
		foreach (ExportInfo export in Exports)
		{
			if (newChild.ParentSlot == export.Slot)
			{
				newChild.ScaleToSlot();
				export.ExportingThing = newChild;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		foreach (ImportInfo import in Imports)
		{
			if (import.ImportingThing == previousChild)
			{
				import.ImportingThing = null;
				break;
			}
		}
		foreach (ExportInfo export in Exports)
		{
			if (export.ExportingThing == previousChild)
			{
				export.ExportingThing = null;
				break;
			}
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}
}
