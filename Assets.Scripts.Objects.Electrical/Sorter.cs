using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Sorter : DeviceImportExport2
{
	private static readonly string[] _modeStrings = Enum.GetNames(typeof(SorterMode));

	public HashSet<SorterMotherboard> ControllingSorterMotherboards = new HashSet<SorterMotherboard>();

	public List<FilterReference> FilterReferences = new List<FilterReference>();

	public int CurrentOutput;

	public static readonly int SortSlot1Hash = Animator.StringToHash("SortSlot1");

	public static readonly int SortSlot2Hash = Animator.StringToHash("SortSlot2");

	public static readonly int ImportHash = Animator.StringToHash("import");

	public override string[] ModeStrings => _modeStrings;

	public override bool CanIceMelt => false;

	public SorterMode SorterMode => (SorterMode)Mode;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override void Awake()
	{
		base.Awake();
		if (!GameManager.IsBatchMode)
		{
			ExportSlot.OnEnter += PlaySort1Sound;
			base.ExportSlot2.OnEnter += PlaySort2Sound;
		}
	}

	private void PlaySort1Sound()
	{
		PlaySound(SortSlot1Hash);
		GetAudioEvent(ImportHash)?.Stop();
	}

	private void PlaySort2Sound()
	{
		PlaySound(SortSlot2Hash);
		GetAudioEvent(ImportHash)?.Stop();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action != InteractableType.Mode)
		{
			return;
		}
		switch ((SorterMode)interactable.State)
		{
		case SorterMode.Split:
			RemoveAllFilters();
			if (CurrentOutput < 0)
			{
				CurrentOutput = 0;
			}
			break;
		case SorterMode.Logic:
			RemoveAllFilters();
			CurrentOutput = -1;
			break;
		case SorterMode.Filter:
			break;
		}
	}

	public FilterReference CreateFilter()
	{
		FilterReference filterReference = new FilterReference(this);
		FilterReferences.Add(filterReference);
		CheckSorterState();
		return filterReference;
	}

	public void RemoveFilter(int index)
	{
		FilterReferences.RemoveAt(index);
		CheckSorterState();
	}

	public void RemoveFilter(FilterReference filter)
	{
		FilterReferences.Remove(filter);
		CheckSorterState();
	}

	public void RemoveAllFilters()
	{
		FilterReferences.Clear();
		CheckSorterState();
	}

	private void CheckSorterState()
	{
		if (GameManager.RunSimulation)
		{
			if (FilterReferences.Count == 0 && SorterMode == SorterMode.Filter)
			{
				OnServer.Interact(base.InteractMode, 0);
			}
			else if (FilterReferences.Count > 0 && SorterMode != SorterMode.Filter)
			{
				OnServer.Interact(base.InteractMode, 1);
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SorterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is SorterSaveData sorterSaveData))
		{
			return;
		}
		CurrentOutput = sorterSaveData.CurrentOutput;
		foreach (FilterReference filterReference in sorterSaveData.FilterReferences)
		{
			FilterReferences.Add((filterReference.PrefabName == string.Empty) ? new FilterReference(filterReference.SlotType, this) : new FilterReference(filterReference.PrefabName, this));
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SorterSaveData sorterSaveData)
		{
			sorterSaveData.CurrentOutput = CurrentOutput;
			sorterSaveData.FilterReferences = FilterReferences;
		}
	}

	private bool ShouldFilter()
	{
		foreach (FilterReference filterReference in FilterReferences)
		{
			if (filterReference.IsTrue(ImportingThing))
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanCompleteImport && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	protected override void OnServerExportTick()
	{
		if (OnOff && Powered)
		{
			TryFilter();
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
		}
	}

	protected override void OnServerExport2Tick()
	{
		if (OnOff && Powered)
		{
			if (CanBeginExport2)
			{
				OnServer.Interact(base.InteractExport2, 1);
			}
			base.OnServerExport2Tick();
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return CurrentOutput;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Output)
		{
			CurrentOutput = (int)value.Clamp(-1.0, 1.0);
		}
	}

	private void TryFilter()
	{
		if (!OnOff || !Powered || !CanCompleteImport || ImportingThing == null)
		{
			return;
		}
		switch (SorterMode)
		{
		case SorterMode.Split:
			if (CurrentOutput == 0)
			{
				if (IsNextExportReady)
				{
					OnServer.MoveToSlot(ImportingThing, ExportSlot);
					CurrentOutput = 1;
				}
			}
			else if (base.IsNextExport2Ready)
			{
				OnServer.MoveToSlot(ImportingThing, base.ExportSlot2);
				CurrentOutput = 0;
			}
			break;
		case SorterMode.Filter:
			if (FilterReferences.Count <= 0)
			{
				break;
			}
			if (ShouldFilter())
			{
				if (base.IsNextExport2Ready)
				{
					OnServer.MoveToSlot(ImportingThing, base.ExportSlot2);
				}
			}
			else if (IsNextExportReady)
			{
				OnServer.MoveToSlot(ImportingThing, ExportSlot);
			}
			break;
		case SorterMode.Logic:
			if (CurrentOutput == 0)
			{
				if (IsNextExportReady)
				{
					OnServer.MoveToSlot(ImportingThing, ExportSlot);
					CurrentOutput = -1;
				}
			}
			else if (CurrentOutput == 1 && base.IsNextExport2Ready)
			{
				OnServer.MoveToSlot(ImportingThing, base.ExportSlot2);
				CurrentOutput = -1;
			}
			break;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!GameManager.IsBatchMode)
		{
			ExportSlot.OnEnter -= PlaySort1Sound;
			base.ExportSlot2.OnEnter -= PlaySort2Sound;
		}
	}
}
